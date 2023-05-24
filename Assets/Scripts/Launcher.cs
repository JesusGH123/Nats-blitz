using UnityEngine;
using System.Text;
using NATS.Client;
using TMPro;

//Class to make NATS connection
public class Launcher : MonoBehaviour
{
    public static Launcher instance;
    public IConnection connection;

    public GameObject playerPrefab;
    public Transform playerSpawn;

    public GameObject nameInputScreen;
    public TMP_InputField playerName, serverUrl;

    public GameObject myPlayer;

    private string defaultUrl = "nats://demo.nats.io:4222";

    private void Awake()
    {
        instance = this;
        Cursor.lockState = CursorLockMode.Confined;   //Cursor dissapears when the game starts
    }

    public void Connect()
    {
        Options opt = ConnectionFactory.GetDefaultOptions();

        if(serverUrl.text == null || serverUrl.text = "")
        {
            opt.Url = defaultUrl;
        } else
        {
            opt.Url = serverUrl.text;
        }
        opt.Name = "Nats Blitz";
        opt.NoEcho = true;

        ConnectionFactory cf = new ConnectionFactory();
        connection = cf.CreateConnection(opt);

        //If the connection was successful spawn a player
        if (connection.ConnectedId != null)
        {
            myPlayer = Instantiate(playerPrefab, playerSpawn.position, Quaternion.identity);
        }
    }
}