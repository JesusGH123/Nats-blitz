using UnityEngine;
using System.Text;
using NATS.Client;

public class PlayerController : MonoBehaviour
{
    public const string PLAYERPOS = "blitz.playerPosition";
    public const string PLAYERROT = "blitz.playerRotation";

    private float updateTime = 0.1f;
    private float timer;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;
    public CharacterController charCon;
    public float jumpForce = 12f, gravityMod = 2.5f;
    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    [System.Serializable]
    private class PositionData
    {
        public Vector3 position;
    }

    [System.Serializable]
    private class RotationData
    {
        public Quaternion rotation;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;   //Cursor dissapears when the game starts

        NetworkManagerController.instance.connection.SubscribeAsync(PLAYERPOS, OnPositionMessage);
        NetworkManagerController.instance.connection.Publish(PLAYERPOS, Encoding.UTF8.GetBytes("Player pos captured"));
        NetworkManagerController.instance.connection.SubscribeAsync(PLAYERROT, OnRotationMessage);
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

        //Network behaviour
        timer += Time.deltaTime;

        if (timer >= updateTime)
        {
            Debug.Log("Message sent");
            timer = 0;
            SendRotationUpdate();
            SendPositionUpdate();
        }

    }

    private void SendPositionUpdate()
    {
        string positionData = JsonUtility.ToJson(new PositionData
        {
            position = this.GetComponent<Transform>().position
        });
        byte[] data = Encoding.UTF8.GetBytes(positionData);
        NetworkManagerController.instance.connection.Publish(PLAYERPOS, data);
    }
    private void SendRotationUpdate()
    {
        string rotationData = JsonUtility.ToJson(new RotationData
        {
            rotation = this.GetComponent<Transform>().rotation
        });
        byte[] data = Encoding.UTF8.GetBytes(rotationData);
        NetworkManagerController.instance.connection.Publish(PLAYERROT, data);
    }

    private void OnPositionMessage(object sender, MsgHandlerEventArgs args)
    {
        Debug.Log("A position message arrived!");
    }

    private void OnRotationMessage(object sender, MsgHandlerEventArgs args)
    {
        Debug.Log("A rotatiom message arrived!");
    }
}