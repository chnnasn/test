using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerEffect : MonoBehaviour
{
    public GameObject Volume;
    public float showDuration = 1.5f; // 显示持续时间

    private Tween hideTween;
    private Graphic[] _volumeGraphics;
    private Color[] _originalVolumeColors;
    private bool _isGamblingVolumeActive;

    private void Awake()
    {
        CacheVolumeGraphics();
    }

    private void CacheVolumeGraphics()
    {
        if (Volume != null)
        {
            _volumeGraphics = Volume.GetComponentsInChildren<Graphic>();
            _originalVolumeColors = _volumeGraphics.Length > 0
                ? new Color[_volumeGraphics.Length]
                : null;
        }
    }

    private void OnEnable()
    {
        if (RunTimeContext.TryGetExistingInstance(out RunTimeContext context))
        {
            context.InjectPlayer(BindPlayer);
            context.PlayerChanged += OnPlayerChanged;
        }

        if (EventManager.TryGetExistingInstance(out EventManager em))
        {
            em.GamblingGreatLuckStarted += OnGamblingGreatLuckStarted;
            em.GamblingGreatLuckEnded += OnGamblingGreatLuckEnded;
        }
    }

    private void OnDisable()
    {
        if (!RunTimeContext.TryGetExistingInstance(out RunTimeContext context)) return;

        context.PlayerChanged -= OnPlayerChanged;
        context.InjectPlayer(UnbindPlayer);
        
        StopHideTween();

        if (EventManager.TryGetExistingInstance(out EventManager em))
        {
            em.GamblingGreatLuckStarted -= OnGamblingGreatLuckStarted;
            em.GamblingGreatLuckEnded -= OnGamblingGreatLuckEnded;
        }
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

        // 大吉 Volume 激活中，不覆盖
        if (_isGamblingVolumeActive) return;

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

    #region Gambling Great Luck Volume

    private void OnGamblingGreatLuckStarted(float duration)
    {
        if (Volume == null) return;

        _isGamblingVolumeActive = true;
        StopHideTween();

        // 保存原始颜色并设为金色
        if (_volumeGraphics != null)
        {
            for (int i = 0; i < _volumeGraphics.Length; i++)
            {
                _originalVolumeColors[i] = _volumeGraphics[i].color;
                _volumeGraphics[i].color = new Color(1f, 0.85f, 0.3f, _originalVolumeColors[i].a);
            }
        }

        Volume.SetActive(true);
        Debug.LogWarning("[PlayerEffect] 大吉 Volume 显示（金色）");
    }

    private void OnGamblingGreatLuckEnded()
    {
        _isGamblingVolumeActive = false;

        // 恢复原始颜色
        if (_volumeGraphics != null)
        {
            for (int i = 0; i < _volumeGraphics.Length; i++)
                _volumeGraphics[i].color = _originalVolumeColors[i];
        }

        if (Volume != null)
            Volume.SetActive(false);

        Debug.LogWarning("[PlayerEffect] 大吉 Volume 恢复");
    }

    #endregion
}