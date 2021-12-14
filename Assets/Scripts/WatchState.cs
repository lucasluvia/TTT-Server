using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchState : MonoBehaviour
{
    [SerializeField] private NetworkedServer server;

    int count = 0;
    bool hasWon = false;
    int winCheck;

    int StateTL = 0; int StateTM = 0; int StateTR = 0; int StateCL = 0; int StateCM = 0; int StateCR = 0; int StateBL = 0; int StateBM = 0; int StateBR = 0; 
   
    public Queue<string> OrderOfPlay = new Queue<string>(); 

    void Update()
    {
        winCheck = CheckForWin();
        if (winCheck == 1 && !hasWon)
        {
            hasWon = true;
            server.winResult = 1;
            server.doReplay = true;
        }
        else if (winCheck == 2 && !hasWon)
        {
            hasWon = true;
            server.winResult = 2;
            server.doReplay = true;
        }
        else
        {
            if (count == 9 && !hasWon)
            {
                hasWon = true;
                server.winResult = 3;
                server.Replay();
            }
        }
    }

    public void RecieveInput(string location, int id)
    {
        if (location == "TL")
            StateTL = id;
        else if (location == "TM")
            StateTM = id;
        else if (location == "TR")
            StateTR = id;
        else if (location == "CL")
            StateCL = id;
        else if (location == "CM")
            StateCM = id;
        else if (location == "CR")
            StateCR = id;
        else if (location == "BL")
            StateBL = id;
        else if (location == "BM")
            StateBM = id;
        else if (location == "BR")
            StateBR = id;
        
        count++;
        OrderOfPlay.Enqueue(location);
    }

    private int CheckForWin()
    {
        if (StateTL != 0)
        {
            if (StateTM == StateTL && StateTR == StateTL)
                return StateTL;
            if (StateCL == StateTL && StateBL == StateTL)
                return StateTL;
            if (StateCM == StateTL && StateBR == StateTL)
                return StateTL;
        }
        else if (StateCM != 0)
        {
            if (StateBL == StateCM && StateTR == StateCM)
                return StateCM;
            if (StateTM == StateCM && StateBM == StateCM)
                return StateCM;
            if (StateCL == StateCM && StateCR == StateCM)
                return StateCM;
        }
        else if (StateBR != 0)
        {
            if (StateTR == StateBR && StateCR == StateBR)
                return StateBR;
            if (StateBL == StateBR && StateBM == StateBR)
                return StateBR;
        }
        return 0;
    }

    public void WipeState()
    {
        count = 0;
        hasWon = false;
        StateTL = 0; StateTM = 0; StateTR = 0; StateCL = 0; StateCM = 0; StateCR = 0; StateBL = 0; StateBM = 0; StateBR = 0; 
    }

}
