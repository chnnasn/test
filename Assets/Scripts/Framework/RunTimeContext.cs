using InfimaGames.LowPolyShooterPack;
using UnityEngine;

public class RunTimeContext : LazySingleton<RunTimeContext>
{
    public Player Player { get; private set; }
    public Character Character { get; private set; }
    public WaveManager WaveManager { get; private set; }

    public GameObject PlayerObject => Character != null ? Character.gameObject : null;

    public void RegisterPlayer(Player player)
    {
        Player = player;
        Character = player != null ? player.GetComponent<Character>() : null;
        RebindPendingProperties();
    }

    public void UnregisterPlayer(Player player)
    {
        if (Player != player) return;

        Player = null;
        Character = null;
    }

    public void RegisterWaveManager(WaveManager waveManager)
    {
        WaveManager = waveManager;
        RebindPendingProperties();
    }

    public void UnregisterWaveManager(WaveManager waveManager)
    {
        if (WaveManager == waveManager)
            WaveManager = null;
    }

    private void RebindPendingProperties()
    {
        if (EventManager.TryGetExistingInstance(out EventManager eventManager))
            eventManager.BindPendingRuntimeContextProperties();
    }
}
