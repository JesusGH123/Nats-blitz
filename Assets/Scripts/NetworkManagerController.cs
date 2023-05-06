using UnityEngine;
using System.Text;
// Reference the NATS client.
using NATS.Client;

//Class to make NATS connection
public class NetworkManagerController : MonoBehaviour
{
    public static NetworkManagerController instance;
    public IConnection connection;

    public GameObject playerPrefab;
    public Transform playerSpawn;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Options opt = ConnectionFactory.GetDefaultOptions();
        opt.Url = "nats://demo.nats.io:4222";
        opt.Name = "Nats Blitz";

        ConnectionFactory cf = new ConnectionFactory();
        connection = cf.CreateConnection(opt);
        connection.SubscribeAsync("blitz.playerConnected", (sender, args) => {
            Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        });
        

        if(connection.ConnectedId != null)
        {
            connection.Publish("blitz.playerConnected", Encoding.UTF8.GetBytes("Game connected!"));
            //Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
