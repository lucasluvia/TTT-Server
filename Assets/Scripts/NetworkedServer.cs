using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    [SerializeField] ServerButtonBehaviour buttonTL;
    [SerializeField] ServerButtonBehaviour buttonTM;
    [SerializeField] ServerButtonBehaviour buttonTR;
    [SerializeField] ServerButtonBehaviour buttonCL;
    [SerializeField] ServerButtonBehaviour buttonCM;
    [SerializeField] ServerButtonBehaviour buttonCR;
    [SerializeField] ServerButtonBehaviour buttonBL;
    [SerializeField] ServerButtonBehaviour buttonBM;
    [SerializeField] ServerButtonBehaviour buttonBR;

    [SerializeField] WatchState watchState;

    public int winResult = 0;
    bool shouldCheckWinState = true;
    public bool doReplay = false;

    bool player1WantsRestart = false;
    bool player2WantsRestart = false;
    bool TurnOdd = true;

    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    LinkedList<PlayerAccount> playerAccounts;
    PlayerAccount Player1 = new PlayerAccount(0);
    PlayerAccount Player2 = new PlayerAccount(0);
    PlayerAccount Spectator = new PlayerAccount(0);
    PlayerAccount ActivePlayer = new PlayerAccount(0);

    private IEnumerator showReplay = null;

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
        if(shouldCheckWinState)
            WinState();
        if (doReplay)
        {
            doReplay = false;
            Replay();
        }


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
                else
                {
                    Spectator.setID(recConnectionID);
                    SendMessageToClient(ServerToClientSignifiers.Spectating + ",You are a spectator", Spectator.playerID);
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
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + clickedSquare, Spectator.playerID);
                watchState.RecieveInput(clickedSquare, id);
                ActivePlayer.playerID = Player2.playerID;
                SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", Player2.playerID);
            } 
            else if (id == Player2.playerID)
            {
                AddInputToServerUI(clickedSquare, id);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, id);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, Player1.playerID);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + clickedSquare, Spectator.playerID);
                watchState.RecieveInput(clickedSquare, id);
                ActivePlayer.playerID = Player1.playerID;
                SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", Player1.playerID);
            }
            
        }
        if (signifier == ClientToServerSignifiers.Restart)
        {
            if (id == Player1.playerID)
                player1WantsRestart = true;
            else if (id == Player2.playerID)
                player2WantsRestart = true;

            if(player1WantsRestart && player2WantsRestart)
            {
                SendMessageToClient(ServerToClientSignifiers.WipeBoard + ",Wipe your board!", Player1.playerID);
                SendMessageToClient(ServerToClientSignifiers.WipeBoard + ",Wipe your board!", Player2.playerID);
                SendMessageToClient(ServerToClientSignifiers.WipeBoard + ",Wipe your board!", Spectator.playerID);
                SendMessageToClient(ServerToClientSignifiers.ItsYourTurn + ",It's your turn", ActivePlayer.playerID);
                WipeBoard();
                
            }
        }
        if (signifier == ClientToServerSignifiers.SendText)
        {
            SendMessageToClient(ServerToClientSignifiers.ReceiveText + "," + csv[1].ToString(), Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.ReceiveText + "," + csv[1].ToString(), Player2.playerID);
        }
    }
    
    private void AddInputToServerUI(string clickedSquare, int id)
    {
        if(id == Player1.playerID)
        {
            inPositionPlaceX(clickedSquare);
        }
        else if(id == Player2.playerID)
        {
            inPositionPlaceO(clickedSquare);
        }
    }

    private void WinState()
    {
        if(winResult == 1)
        {
            SendMessageToClient(ServerToClientSignifiers.YouWon + ",You Won! Press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.YouLost + ",You Lost. press R to play again", Player2.playerID);
            shouldCheckWinState = false;
        }
        else if(winResult == 2)
        {
            SendMessageToClient(ServerToClientSignifiers.YouLost + ",You Lost. press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.YouWon + ",You Won! Press R to play again", Player2.playerID);
            shouldCheckWinState = false;
        }
        else if(winResult == 3)
        {
            SendMessageToClient(ServerToClientSignifiers.Tie + ",Tie Game. press R to play again", Player1.playerID);
            SendMessageToClient(ServerToClientSignifiers.Tie + ",Tie Game. press R to play again", Player2.playerID);
            shouldCheckWinState = false;
        }
    }

    private void WipeBoard()
    {
        // Wipe Buttons
        WipeButtons();

        // Wipe State
        watchState.WipeState();

        // Wipe Variables
        winResult = 0;
        shouldCheckWinState = true;
        player1WantsRestart = false;
        player2WantsRestart = false;
    }
    private void WipeButtons()
    {
        buttonTL.WipePlacement();
        buttonTM.WipePlacement();
        buttonTR.WipePlacement();
        buttonCL.WipePlacement();
        buttonCM.WipePlacement();
        buttonCR.WipePlacement();
        buttonBL.WipePlacement();
        buttonBM.WipePlacement();
        buttonBR.WipePlacement();
    }

    public void Replay()
    {
        WipeButtons();
        SendMessageToClient(ServerToClientSignifiers.WatchReplay + "", Player1.playerID);
        SendMessageToClient(ServerToClientSignifiers.WatchReplay + "", Player2.playerID);
        SendMessageToClient(ServerToClientSignifiers.WatchReplay + "", Spectator.playerID);
        showReplay = ShowReplay(1.0f);
        StartCoroutine(showReplay);
    }


    IEnumerator ShowReplay(float TimeToWait)
    {
        string placedLocation;
        while (watchState.OrderOfPlay.Count > 0)
        {
            yield return new WaitForSeconds(TimeToWait);
            placedLocation = watchState.OrderOfPlay.Dequeue();
            if (TurnOdd)
            {
                inPositionPlaceX(placedLocation);

                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + placedLocation, Player1.playerID);
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + placedLocation, Player2.playerID);
                SendMessageToClient(ServerToClientSignifiers.XValuePlaced + ",in square ," + placedLocation, Spectator.playerID);
            }
            else if (!TurnOdd)
            {
                inPositionPlaceO(placedLocation);

                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + placedLocation, Player1.playerID);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + placedLocation, Player2.playerID);
                SendMessageToClient(ServerToClientSignifiers.OValuePlaced + ",in square ," + placedLocation, Spectator.playerID);

            }
            TurnOdd = !TurnOdd;

        }
        SendMessageToClient(ServerToClientSignifiers.WantToRestart + "", Player1.playerID);
        SendMessageToClient(ServerToClientSignifiers.WantToRestart + "", Player2.playerID);
    }


    private void inPositionPlaceX(string location)
    {
        if (location == "TL")
            buttonTL.PlaceX();
        else if (location == "TM")
            buttonTM.PlaceX();
        else if (location == "TR")
            buttonTR.PlaceX();
        else if (location == "CL")
            buttonCL.PlaceX();
        else if (location == "CM")
            buttonCM.PlaceX();
        else if (location == "CR")
            buttonCR.PlaceX();
        else if (location == "BL")
            buttonBL.PlaceX();
        else if (location == "BM")
            buttonBM.PlaceX();
        else if (location == "BR")
            buttonBR.PlaceX();
    }

    private void inPositionPlaceO(string clickedSquare)
    {
        if (clickedSquare == "TL")
            buttonTL.PlaceO();
        else if (clickedSquare == "TM")
            buttonTM.PlaceO();
        else if (clickedSquare == "TR")
            buttonTR.PlaceO();
        else if (clickedSquare == "CL")
            buttonCL.PlaceO();
        else if (clickedSquare == "CM")
            buttonCM.PlaceO();
        else if (clickedSquare == "CR")
            buttonCR.PlaceO();
        else if (clickedSquare == "BL")
            buttonBL.PlaceO();
        else if (clickedSquare == "BM")
            buttonBM.PlaceO();
        else if (clickedSquare == "BR")
            buttonBR.PlaceO();
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
    public const int Restart = 3;
    public const int SendText = 4;
}

public static class ServerToClientSignifiers
{
    public const int XValuePlaced = 1;
    public const int OValuePlaced = 2;
    public const int ItsYourTurn = 3;
    public const int YouWon = 4;
    public const int YouLost = 5;
    public const int Tie = 6;
    public const int WipeBoard = 7;
    public const int WatchReplay = 8;
    public const int WantToRestart = 9;
    public const int ReceiveText = 10;
    public const int Spectating = 11;
}
