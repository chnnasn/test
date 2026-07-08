using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PortalWave", menuName = "ScriptableObjects/PortalWave", order = 1)]
public class PortalWave : ScriptableObject
{
    public SpawnPortal[] spawnPortals;
}