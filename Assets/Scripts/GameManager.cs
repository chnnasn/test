using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : LazySingleton<GameManager>
{

    private GameObject Player;

    private void Start()
    {
        
        Player = GameObject.FindGameObjectWithTag("Player");
    }

    public GameObject GetPlayer()
    {
        return Player;
    }
}
