using UnityEngine;
using System.Collections.Generic;
using System.Text;
using NATS.Client;

public class PlayerController : MonoBehaviour
{
    private float verticalRotStore;
    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;
    public CharacterController charCon;
    public float jumpForce = 12f, gravityMod = 2.5f;
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;
    public Camera playerCam;
    private Vector2 mouseInput;
    private int myPlayerId;
    public float mouseSensivity = 1f;

    string id;
    bool isMine;

    private static IDictionary<string, GameObject> playersMap;


    void Start()
    {
        
        Cursor.lockState = CursorLockMode.Locked;   //Cursor dissapears when the game starts
        myPlayerId = Random.Range(1, 100);
        if (playersMap == null)
        {
            playersMap = new Dictionary<string, GameObject>();

            Launcher.instance.connection.SubscribeAsync("blitz.playerPos", (sender, args) => {
                //Parse and search for the session Id
                string payload = System.Text.Encoding.UTF8.GetString(args.Message.Data);
                string[] playerId = payload.Split(':');
                id = playerId[0];

                if (!playersMap.ContainsKey(id))
                {
                    string[] coords;
                    coords = playerId[1].Split(',');

                    //We do not have a key and the other player is not spawned yet
                    if (myPlayerId.ToString() != id)
                    {
                        isMine = false;

                        GameObject result = Instantiate(Launcher.instance.playerPrefab, new Vector3(
                        float.Parse(coords[0]),
                        float.Parse(coords[1]),
                        float.Parse(coords[2])),
                        Quaternion.identity);

                        playersMap.Add(id, result);
                            
                    }
                    else
                        isMine = true;
                }
                else
                {
                    string[] coords = playerId[1].Split(','); ;
                    GameObject remotePlayer;

                    playersMap.TryGetValue(id, out remotePlayer);

                    //We have registered the key and it is not our player
                    if (myPlayerId.ToString() != id) {
                        isMine = false;

                        remotePlayer.transform.position = new Vector3(
                                float.Parse(coords[0]),
                                float.Parse(coords[1]),
                                float.Parse(coords[2])
                        );
                    } else
                    {
                        isMine = true;
                    }

                }
            });

            InvokeRepeating("PublishPlayerData", 1.0f, 1/60f);
        }
    }

    void Update()
    {
        //Local behaviour
        //Player movement
        if (isMine) {
            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            activeMoveSpeed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : moveSpeed;

            float yVel = movement.y;
            movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
            movement.y = yVel;

            if (charCon.isGrounded)
            {
                movement.y = 0;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
            charCon.Move(movement * Time.deltaTime);

            //Camera behaviour
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensivity;

            verticalRotStore -= Mathf.Clamp(mouseInput.y, -60f, 60f);
            playerCam.transform.rotation = Quaternion.Euler(verticalRotStore, playerCam.transform.rotation.eulerAngles.y + mouseInput.x, playerCam.transform.rotation.eulerAngles.z); //Look up/down
        }

        //Cursor behaviour
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
                Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void PublishPlayerData()
    {
        string message = myPlayerId + ":" + transform.position.x + "," + transform.position.y + "," + transform.position.z;
        byte[] payload = System.Text.Encoding.Default.GetBytes(message);

        // Publish message to NATS server (payload is the message)
        Launcher.instance.connection.Publish("blitz.playerPos", payload);
    }

    void Log(string message)
    {
        Launcher.instance.connection.Publish("blitz.Log." + myPlayerId.ToString(), System.Text.Encoding.Default.GetBytes(message));
    }
}