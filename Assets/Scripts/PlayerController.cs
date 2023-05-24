using UnityEngine;
using System.Collections.Generic;
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

    public string myPlayerId;

    public float mouseSensivity = 1f;

    private static IDictionary<string, GameObject> playersMap;
    public List<Material> playerColors;

    private RaycastHit hit;

    GameObject remotePlayer;
    IAsyncSubscription playerSub, removePlayerSub;

    private void Awake()
    {
        Destroy(this.GetComponent<Rigidbody>());
    }

    void Start()
    {
        Launcher.instance.nameInputScreen.SetActive(false);

        //Set player random color
        this.GetComponent<MeshRenderer>().material = playerColors[UnityEngine.Random.Range(0, playerColors.Count)];
        this.transform.Find("Backpack").GetComponent<MeshRenderer>().material = this.GetComponent<MeshRenderer>().material;

        Cursor.lockState = CursorLockMode.Locked;   //Cursor dissapears when the game starts
        if (playersMap == null)
        {
            playersMap = new Dictionary<string, GameObject>();
            myPlayerId = Launcher.instance.playerName.text;
            string id;

            playerSub = Launcher.instance.connection.SubscribeAsync("blitz.playerPos", (sender, args) =>
            {
                string[] playerId;   
                //Parse and search for the session Id
                string payload = System.Text.Encoding.UTF8.GetString(args.Message.Data);
                playerId = payload.Split(':');
                id = playerId[0];

                RemotePlayersUpdate(id, playerId[1].Split(','));
            });
            InvokeRepeating("PublishPlayerData", 1.0f, 1 / 30f);

            removePlayerSub = Launcher.instance.connection.SubscribeAsync("blitz.playerRemove", (sender, args) =>
            {
                DestroyRemotePlayer(System.Text.Encoding.UTF8.GetString(args.Message.Data));
            });
        }
    }

    void Update()
    {
        if (this.gameObject == Launcher.instance.myPlayer)
        {
            moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            activeMoveSpeed = (Input.GetKey(KeyCode.LeftShift)) ? runSpeed : moveSpeed;

            float yVel = movement.y;
            movement = ((-transform.forward * moveDir.z) + (-transform.right * moveDir.x)).normalized * activeMoveSpeed;
            movement.y = yVel;

            if (charCon.isGrounded)
            {
                movement.y = 0;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, out hit, .25f, groundLayers);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            if (isGrounded && hit.collider.gameObject.CompareTag("Player"))
            {
                //Log("Aplastando a: " + hit.collider.gameObject.GetComponent<PlayerController>().myPlayerId);
                //DestroyRemotePlayer(hit.collider.gameObject.GetComponent<PlayerController>().myPlayerId);
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
            charCon.Move(movement * Time.deltaTime);
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
        Launcher.instance.connection.Publish("blitz.Log." + myPlayerId, System.Text.Encoding.Default.GetBytes(message));
    }

    void RemotePlayersUpdate(string id, string[] coords)
    {
        playersMap.TryGetValue(id, out remotePlayer);
        if (remotePlayer == null)
        {
            remotePlayer = Instantiate(
                Launcher.instance.playerPrefab,
                new Vector3(
                    float.Parse(coords[0]),
                    float.Parse(coords[1]),
                    float.Parse(coords[2])),
                Quaternion.identity
            );
            playersMap.Add(id, remotePlayer);
        }
        else
        {
            remotePlayer.transform.position =
                new Vector3(
                    float.Parse(coords[0]),
                    float.Parse(coords[1]),
                    float.Parse(coords[2])
                    );
        }
    }

    private void DestroyRemotePlayer(string playerId)
    {
        playersMap.TryGetValue(playerId, out remotePlayer);
        if(remotePlayer != null)
        {
            Destroy(remotePlayer);
        }
        playersMap.Remove(playerId);
    }

    public void DestroyMe()
    {
        //Unsubscribe suscriptions
        playerSub.Unsubscribe();
        removePlayerSub.Unsubscribe();

        //Destroy me on remote players
        Launcher.instance.connection.Publish(
            "blitz.playerRemove",
            System.Text.Encoding.Default.GetBytes(myPlayerId)
        );
        //Destroy local player
        Launcher.instance.connection.Close();
        Launcher.instance.connection.Flush();
        Destroy(this);
    }

    private void OnApplicationQuit()
    {
        DestroyMe();
    }

}