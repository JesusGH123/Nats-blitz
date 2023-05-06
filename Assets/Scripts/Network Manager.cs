using UnityEngine;
using System.Text;
// Reference the NATS client.
using NATS.Client;

//Class to make NATS connection
public class NetworkManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public IConnection connection;

    private GameObject newPlayer;
    private string playerId;

    void OnPlayerSpawn(object sender, MsgHandlerEventArgs args)
    {
        // Parse the message data to get the player's ID, position, and rotation
        string[] data = System.Text.Encoding.UTF8.GetString(args.Message.Data).Split(',');
        int playerId = int.Parse(data[0]);
        Vector3 position = new Vector3(float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]));
        Quaternion rotation = Quaternion.Euler(float.Parse(data[4]), float.Parse(data[5]), float.Parse(data[6]));

        // Spawn a player prefab at the specified position and rotation
        newPlayer = Instantiate(playerPrefab, position, rotation);
        // Set the player's ID
        playerId = newPlayer.GetInstanceID();
    }

    void Start()
    {

        Options opt = ConnectionFactory.GetDefaultOptions();
        opt.Url = "nats://demo.nats.io:4222";
        opt.Name = "Nats Blitz";

        ConnectionFactory cf = new ConnectionFactory();
        IConnection connection = cf.CreateConnection(opt);

        connection.SubscribeAsync("blitz.playerConnected", OnPlayerSpawn);
        connection.Publish("blitz.playerConnected", Encoding.UTF8.GetBytes("Game connected!"));
    }

    // Update is called once per frame
    void Update()
    {
        // Check for input to move the player
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        transform.position += movement * Time.deltaTime;

        // If the player has moved, send a message to update their position
        string messageData = playerId + "," + transform.position.x + "," + transform.position.y + "," + transform.position.z;
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(messageData);
        connection.Publish("blitz.update_position", messageBytes);
    }
}
