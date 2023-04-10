using UnityEngine;
using System.Text;
using NATS.Client;

//Class to make NATS connection
public class Launcher : MonoBehaviour
{
    public static Launcher instance;
    public IConnection connection;

    public GameObject playerPrefab;
    public Transform playerSpawn;

    private string playerId;

    public string getPlayerId()
    {
        return playerId;
    }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Options opt = ConnectionFactory.GetDefaultOptions();
        opt.Url = "nats://demo.nats.io:4222";
        opt.Name = "Nats Blitz";
        //opt.NoEcho = true;

        ConnectionFactory cf = new ConnectionFactory();
        connection = cf.CreateConnection(opt);
        connection.SubscribeAsync("blitz.playerConnected", (sender, args) => {
            Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        });

        //If the connection was successful spawn a player
        if (connection.ConnectedId != null)
        {
            connection.Publish("blitz.playerConnected", Encoding.UTF8.GetBytes("Game connected!"));
        }
    }
}

