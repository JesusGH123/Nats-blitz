using UnityEngine;
using System.Collections.Generic;
using System.Text;
using NATS.Client;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;
    public CharacterController charCon;
    public float jumpForce = 12f, gravityMod = 2.5f;
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    private int myPlayerId;

    private static IDictionary<string, GameObject> playersMap;


    void Start()
    {
        myPlayerId = Random.Range(1, 100);
        //Cursor.lockState = CursorLockMode.Locked;   //Cursor dissapears when the game starts

        if (playersMap == null)
        {
            playersMap = new Dictionary<string, GameObject>();

            Launcher.instance.connection.SubscribeAsync("blitz.playerPos", (sender, args) => {
                //Parse and search for the session Id
                string payload = System.Text.Encoding.UTF8.GetString(args.Message.Data);
                string[] playerId = payload.Split(':');
                string id = playerId[0];

                if (!playersMap.ContainsKey(id))
                {
                    string[] coords;
                    coords = playerId[1].Split(',');

                    if (myPlayerId.ToString() != id)
                    {
                        Log("Creating object");

                        try
                        {
                            GameObject result = Instantiate(Launcher.instance.playerPrefab, new Vector3(
                                float.Parse(coords[0]),
                                float.Parse(coords[1]),
                                float.Parse(coords[2])),
                                Quaternion.identity);
                            try
                            {
                                playersMap.Add(id, result);
                                Log("Added + " + id);
                            }
                            catch (System.Exception ex)
                            {
                                Log(ex.Message.ToString());
                                Log("Adding error");
                            }
                        }
                        catch
                        {
                            Log("----------- There was an exception ---------------- ");
                        }
                    }
                    else
                    {
                        Log("NOT creating object");
                    }
                }
                else
                {
                    Log("Player contain key");
                    string[] coords = playerId[1].Split(','); ;
                    GameObject remotePlayer;

                    try
                    {
                        playersMap.TryGetValue(id, out remotePlayer);

                        if (id != myPlayerId.ToString()) {
                            Log("x: " + float.Parse(coords[0]) +
                                "y: " + float.Parse(coords[1]) +
                                "z: " + float.Parse(coords[2]));
                            remotePlayer.transform.position = new Vector3(
                                    float.Parse(coords[0]),
                                    float.Parse(coords[1]),
                                    float.Parse(coords[2])
                            );
                        }
                    }
                    catch(System.Exception ex)
                    {
                        Log("----------- There was an exception 2 ---------------- " + ex.Message);
                    }
                }
            });

            InvokeRepeating("PublishPlayerData", 1.0f, 1/60f);
        }
        else
            Log("Making the map again");
    }

    void Update()
    {
        //Local behaviour
        //Player movement
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