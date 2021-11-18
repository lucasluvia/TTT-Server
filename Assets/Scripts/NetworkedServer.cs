using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    [SerializeField] ServerButtonBehaviour buttonA;
    [SerializeField] ServerButtonBehaviour buttonB;
    [SerializeField] ServerButtonBehaviour buttonC;
    [SerializeField] ServerButtonBehaviour buttonD;
    [SerializeField] ServerButtonBehaviour buttonE;
    [SerializeField] ServerButtonBehaviour buttonF;
    [SerializeField] ServerButtonBehaviour buttonG;
    [SerializeField] ServerButtonBehaviour buttonH;
    [SerializeField] ServerButtonBehaviour buttonI;

    [SerializeField] WatchState watchState;

    public bool winX = false;
    public bool winO = false;
    public bool isStalemate = false;
    bool shouldCheckState = true;

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
        if(shouldCheckState)
            CheckState();
        
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
                    SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", Player1.playerID);
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
                AddInputToServerUI(clickedSquare, id);
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + clickedSquare, id); 
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + clickedSquare, Player2.playerID);
                watchState.RecieveInput(clickedSquare, id);
                ActivePlayer.playerID = Player2.playerID;
                SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", Player2.playerID);
            } 
            else if (id == Player2.playerID)
            {
                AddInputToServerUI(clickedSquare, id);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, id);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, Player1.playerID);
                watchState.RecieveInput(clickedSquare, id);
                ActivePlayer.playerID = Player1.playerID;
                SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", Player1.playerID);
            }
            
        }
        if (signifier == ClientToServerSignifiers.Replay)
        {
            //Reset both boards, player turn, player isgameover, all buttons, and watchState
            Debug.Log("Replay Triggered");
        }
    }
    
    private void AddInputToServerUI(string clickedSquare, int id)
    {
        if(id == Player1.playerID)
        {
            if (clickedSquare == "A")
                buttonA.PlaceX();
            else if (clickedSquare == "B")
                buttonB.PlaceX();
            else if (clickedSquare == "C")
                buttonC.PlaceX();
            else if (clickedSquare == "D")
                buttonD.PlaceX();
            else if (clickedSquare == "E")
                buttonE.PlaceX();
            else if (clickedSquare == "F")
                buttonF.PlaceX();
            else if (clickedSquare == "G")
                buttonG.PlaceX();
            else if (clickedSquare == "H")
                buttonH.PlaceX();
            else if (clickedSquare == "I")
                buttonI.PlaceX();
        }
        else if(id == Player2.playerID)
        {

            if (clickedSquare == "A")
                buttonA.PlaceO();
            else if (clickedSquare == "B")
                buttonB.PlaceO();
            else if (clickedSquare == "C")
                buttonC.PlaceO();
            else if (clickedSquare == "D")
                buttonD.PlaceO();
            else if (clickedSquare == "E")
                buttonE.PlaceO();
            else if (clickedSquare == "F")
                buttonF.PlaceO();
            else if (clickedSquare == "G")
                buttonG.PlaceO();
            else if (clickedSquare == "H")
                buttonH.PlaceO();
            else if (clickedSquare == "I")
                buttonI.PlaceO();
        }
    }

    private void CheckState()
    {
        if(winX)
        {
            SendMessageToClient(ServerToClientSignifiers.YouWon + ",You Won! Press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.YouLost + ",You Lost. press R to play again", Player2.playerID);
            shouldCheckState = false;
        }
        else if(winO)
        {
            SendMessageToClient(ServerToClientSignifiers.YouLost + ",You Lost. press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.YouWon + ",You Won! Press R to play again", Player2.playerID);
            shouldCheckState = false;
        }
        else if(isStalemate)
        {
            SendMessageToClient(ServerToClientSignifiers.Tie + ",Tie Game. press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.Tie + ",Tie Game. press R to play again", Player2.playerID);
            shouldCheckState = false;
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
    public const int Replay = 2;
}

public static class ServerToClientSignifiers
{
    public const int XValuePlaced = 1;
    public const int OValuePlaced = 2;
    public const int ValueNotPlaced = 3;
    public const int ItsYourTurn = 4;
    public const int YouWon = 5;
    public const int YouLost = 6;
    public const int Tie = 7;

}
