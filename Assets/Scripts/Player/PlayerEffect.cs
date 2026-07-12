using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerEffect : MonoBehaviour
{
    public GameObject Volume;
    public float showDuration = 1.5f; // 显示持续时间
    
    private Tween hideTween;
    
    private void OnEnable()
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
        {
            context.InjectPlayer(BindPlayer);
        }
    }

    private void OnDisable()
    {
        if (!RunTimeContext.TryGetExistingInstance(out RunTimeContext context)) return;

        context.PlayerChanged -= OnPlayerChanged;
        context.InjectPlayer(UnbindPlayer);
        
        StopHideTween();
    }
    
    private void OnPlayerChanged(Player oldPlayer, Player newPlayer)
    {
        UnbindPlayer(oldPlayer);
        BindPlayer(newPlayer);
    }
    
    private void BindPlayer(Player player)
    {
        if (player == null) return;

        player.CurrentHP.OnValueChanged -= OnPlayerHpChanged;
        player.CurrentHP.OnValueChanged += OnPlayerHpChanged;

        SetHp(player.CurrentHP.Value, player.MaxHP);
        
        Volume.SetActive(false);
    }

    private void UnbindPlayer(Player player)
    {
        if (player == null) return;

        player.CurrentHP.OnValueChanged -= OnPlayerHpChanged;
    }
    
    private void OnPlayerHpChanged(float currentHp)
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context) && context.Player != null)
            SetHp(currentHp, context.Player.MaxHP);
    }
    
    public void SetHp(float currentHp, float maxHp)
    {
        // 扣血时触发
        if (currentHp < maxHp)
        {
            ShowVolume();
        }
    }
    
    private void ShowVolume()
    {
        if (Volume == null) return;
        
        // 取消之前的隐藏定时
        StopHideTween();
        
        // 显示
        Volume.SetActive(true);
        
        // 延迟后隐藏
        hideTween = DOVirtual.DelayedCall(showDuration, () => Volume.SetActive(false));
    }
    
    private void StopHideTween()
    {
        if (hideTween != null)
        {
            hideTween.Kill();
            hideTween = null;
        }
    }
}