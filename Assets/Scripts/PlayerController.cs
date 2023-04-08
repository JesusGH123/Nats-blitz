using UnityEngine;
using System.Text;
using NATS.Client;

public class PlayerController : MonoBehaviour
{

    private string playerId;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;
    public CharacterController charCon;
    public float jumpForce = 12f, gravityMod = 2.5f;
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;   //Cursor dissapears when the game starts


        Launcher.instance.connection.SubscribeAsync("blitz.playerPos", (sender, args) => {});
        InvokeRepeating("PublishPlayerData", 1.0f, 0.1f);
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
        string message = playerId + "," + transform.position.x + "," + transform.position.y + "," + transform.position.z;
        byte[] payload = System.Text.Encoding.Default.GetBytes(message);

        // Publish message to NATS server
        Launcher.instance.connection.Publish("blitz.playerPos." + GetInstanceID(), payload);
    }
}