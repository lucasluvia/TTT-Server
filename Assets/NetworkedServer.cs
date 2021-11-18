using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    LinkedList<PlayerAccount> playerAccounts;
    PlayerAccount Player1 = new PlayerAccount(0);
    PlayerAccount Player2 = new PlayerAccount(0);
    PlayerAccount ActivePlayer = new PlayerAccount(0);


    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        playerAccounts = new LinkedList<PlayerAccount>();

    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                if(Player1.playerID == 0)
                {
                    Player1.setID(recConnectionID);
                    Debug.Log("Player1 connected at id: " + recConnectionID + " (playerID = " + Player1.playerID + ")");
                }
                else if(Player2.playerID == 0)
                {
                    Player2.setID(recConnectionID);
                    Debug.Log("Player2 connected at id: " + recConnectionID + " (playerID = " + Player2.playerID + ")");
                    ActivePlayer.setID(Player1.playerID);
                }
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessReceivedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }


    private void ProcessReceivedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');

        int signifier = int.Parse(csv[0]);
        
        if (signifier == ClientToServerSignifiers.ClickedSquare && id == ActivePlayer.playerID)
        {
            string clickedSquare = csv[1];
            if(id == Player1.playerID)
            {
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + clickedSquare, id);
            } 
            else if (id == Player2.playerID)
            {
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, id);
            }
            
        }
    }

}
public class PlayerAccount
{
    public int playerID = 0;
    public PlayerAccount()
    {

    }
    public PlayerAccount(int id)
    {
        playerID = id;
    }
    public void setID(int newID)
    {
        playerID = newID;
    }
}
public static class ClientToServerSignifiers
{
    public const int ClickedSquare = 1;
}
public static class ServerToClientSignifiers
{
    public const int XValuePlaced = 1;
    public const int OValuePlaced = 2;
    public const int ValueNotPlaced = 3;
}
