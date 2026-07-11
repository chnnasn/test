using System;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class RunTimeContext : LazySingleton<RunTimeContext>
{
    public event Action RuntimeDataChanged;
    public event Action<Player, Player> PlayerChanged;
    public event Action<Character, Character> CharacterChanged;
    public event Action<WaveManager, WaveManager> WaveManagerChanged;

    public Player Player { get; private set; }
    public Character Character { get; private set; }
    public WaveManager WaveManager { get; private set; }

    public GameObject PlayerObject => Character != null ? Character.gameObject : null;

    public void InjectPlayer(Action<Player> bind)
    {
        if (Player != null)
            bind?.Invoke(Player);
    }

    public void InjectCharacter(Action<Character> bind)
    {
        if (Character != null)
            bind?.Invoke(Character);
    }

    public void InjectWaveManager(Action<WaveManager> bind)
    {
        if (WaveManager != null)
            bind?.Invoke(WaveManager);
    }

    public void RegisterPlayer(Player player)
    {
        Player oldPlayer = Player;
        Character oldCharacter = Character;
        Character newCharacter = player != null ? player.GetComponent<Character>() : null;

        Player = player;
        Character = newCharacter;

        if (oldPlayer != Player)
            PlayerChanged?.Invoke(oldPlayer, Player);
        if (oldCharacter != Character)
            CharacterChanged?.Invoke(oldCharacter, Character);
        RuntimeDataChanged?.Invoke();
    }

    public void UnregisterPlayer(Player player)
    {
        if (Player != player) return;

        Player oldPlayer = Player;
        Character oldCharacter = Character;

        Player = null;
        Character = null;

        PlayerChanged?.Invoke(oldPlayer, null);
        CharacterChanged?.Invoke(oldCharacter, null);
        RuntimeDataChanged?.Invoke();
    }

    public void RegisterWaveManager(WaveManager waveManager)
    {
        WaveManager oldWaveManager = WaveManager;
        WaveManager = waveManager;

        if (oldWaveManager != WaveManager)
            WaveManagerChanged?.Invoke(oldWaveManager, WaveManager);
        RuntimeDataChanged?.Invoke();
    }

    public void UnregisterWaveManager(WaveManager waveManager)
    {
        if (WaveManager != waveManager) return;

        WaveManager oldWaveManager = WaveManager;
        WaveManager = null;

        WaveManagerChanged?.Invoke(oldWaveManager, null);
        RuntimeDataChanged?.Invoke();
    }
}
