using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchState : MonoBehaviour
{
    [SerializeField] private NetworkedServer server;

    int count = 0;
    
    bool Ax = false; bool Bx = false; bool Cx = false; bool Dx = false; bool Ex = false; bool Fx = false; bool Gx = false; bool Hx = false; bool Ix = false;
    bool Ao = false; bool Bo = false; bool Co = false; bool Do = false; bool Eo = false; bool Fo = false; bool Go = false; bool Ho = false; bool Io = false;

    public Queue<string> OOP = new Queue<string>(); // Order Of Play

    void Update()
    {
        if(hasXWon() && !server.winX)
        {
            server.winX = true;
            server.doReplay = true;
        }
        else if(hasOWon() && !server.winO)
        {
            server.winO = true;
            server.doReplay = true;
        }
        else
        {
            if (count == 9 && !server.isStalemate)
            {
                server.isStalemate = true;
                server.Replay();
            }
        }
    }

    public void RecieveInput(string location, int id)
    {
        if(id == 1)
        {
            if (location == "A")
                Ax = true;
            else if (location == "B")
                Bx = true;
            else if (location == "C")
                Cx = true;
            else if (location == "D")
                Dx = true;
            else if (location == "E")
                Ex = true;
            else if (location == "F")
                Fx = true;
            else if (location == "G")
                Gx = true;
            else if (location == "H")
                Hx = true;
            else if (location == "I")
                Ix = true;
        }
        else if(id == 2)
        {
            if (location == "A")
                Ao = true;
            else if (location == "B")
                Bo = true;
            else if (location == "C")
                Co = true;
            else if (location == "D")
                Do = true;
            else if (location == "E")
                Eo = true;
            else if (location == "F")
                Fo = true;
            else if (location == "G")
                Go = true;
            else if (location == "H")
                Ho = true;
            else if (location == "I")
                Io = true;
        }
        count++;
        OOP.Enqueue(location);
    }

    private bool hasXWon()
    {
        if (Ax)
        {
            if (Bx && Cx)
                return true;
            else if (Dx && Gx)
                return true;
        }
        else if (Ex)
        {
            if (Ax && Ix)
                return true;
            else if (Cx && Gx)
                return true;
            else if (Bx && Hx)
                return true;
            else if (Dx && Fx)
                return true;
        }
        else if (Ix)
        {
            if (Cx && Fx)
                return true;
            else if (Gx && Hx)
                return true;
        }

        return false;
        
    }

    private bool hasOWon()
    {
        if (Ao)
        {
            if (Bo && Co)
                return true;
            else if (Do && Go)
                return true;
        }
        else if (Eo)
        {
            if (Ao && Io)
                return true;
            else if (Co && Go)
                return true;
            else if (Bo && Ho)
                return true;
            else if (Do && Fo)
                return true;
        }
        else if (Io)
        {
            if (Co && Fo)
                return true;
            else if (Go && Ho)
                return true;
        }
        
        return false;
        
    }

    public void WipeState()
    {
        count = 0;
        Ax = false; Bx = false; Cx = false; Dx = false; Ex = false; Fx = false; Gx = false; Hx = false; Ix = false;
        Ao = false; Bo = false; Co = false; Do = false; Eo = false; Fo = false; Go = false; Ho = false; Io = false;
    }

}
