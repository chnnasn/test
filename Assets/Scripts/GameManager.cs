using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class GameManager : LazySingleton<GameManager>
{
    private Character _character;

    public Action<float> AttackAction;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _character = playerObj.GetComponent<Character>();
        }
    }

    /// <summary>
    /// 获取玩家 GameObject
    /// </summary>
    public GameObject GetPlayer()
    {
        return _character != null ? _character.gameObject : null;
    }

    /// <summary>
    /// 获取玩家 Character 组件
    /// </summary>
    public Character GetCharacter()
    {
        return _character;
    }
}
