using UnityEngine;
using System.Text;
// Reference the NATS client.
using NATS.Client;

//Class to make NATS connection
public class Launcher : MonoBehaviour
{
    public static Launcher instance;
    public IConnection connection;

    public Transform spawnPoint; 
    public GameObject playerPrefab, player;

    public void SpawnPlayer(object sender, MsgHandlerEventArgs args)
    {
        string[] data = System.Text.Encoding.UTF8.GetString(args.Message.Data).Split(',');
        Vector3 position = new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
        Quaternion rotation = Quaternion.Euler(float.Parse(data[3]), float.Parse(data[4]), float.Parse(data[5]));

        player = Instantiate(playerPrefab, position, rotation);
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

        ConnectionFactory cf = new ConnectionFactory();
        IConnection connection = cf.CreateConnection(opt);

        connection.SubscribeAsync("blitz.playerConnected", SpawnPlayer);
        connection.Publish("blitz.playerConnected", Encoding.UTF8.GetBytes("Game connected!"));
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
