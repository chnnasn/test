using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PlayerStates : MonoBehaviour
{

    private Character _character;
    
    // Start is called before the first frame update
    void Start()
    {
        _character = GetComponent<Character>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Character getCharacter => _character;
}
