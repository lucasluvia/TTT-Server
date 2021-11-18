using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ServerButtonBehaviour : MonoBehaviour
{
    [SerializeField]
    private char buttonID;
    [SerializeField]
    private NetworkedServer server;
    [SerializeField]
    private TextMeshProUGUI textX;
    [SerializeField]
    private TextMeshProUGUI textO;

    private bool isOccupied = false;

    public void PlaceX()
    {
        textX.gameObject.SetActive(true); 
        isOccupied = true;
    }

    public void PlaceO()
    {
        textO.gameObject.SetActive(true);
        isOccupied = true;
    }

    public void WipePlacement()
    {
        textX.gameObject.SetActive(false);
        textO.gameObject.SetActive(false);
        isOccupied = false;
    }
}
