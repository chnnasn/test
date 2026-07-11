using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using InfimaGames.LowPolyShooterPack;

public enum ButtonTriggerType
{
    SingleClick,   // 单次点击触发
    LongpPress // 按住持续触发
}

public enum ButtonType
{   shoot,
    anim,
    reload,
    sprint
}

public class ButtonClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    private ButtonType _type;
    public ButtonType Type => _type;
    
    [SerializeField]
    private ButtonTriggerType triggerType = ButtonTriggerType.SingleClick; // 触发方式
    
    [Header("Button Press Effect")]
    [SerializeField] private float pressDuration = 0.1f;
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    private Color originalColor;
    private Image buttonImage;
    
    private bool isPressed = false;

    /// <summary>
    /// anim 类型的瞄准切换状态。
    /// </summary>
    private bool _animAiming = false;
    
    private void Start()
    {
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage.color;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.rawPointerPress != null && eventData.rawPointerPress != gameObject) 
            return;
        
        // 单次点击模式：在颜色恢复后触发
        if (triggerType == ButtonTriggerType.SingleClick)
        {
            Sequence clickSequence = DOTween.Sequence();
            clickSequence.Append(buttonImage.DOColor(originalColor, pressDuration));
            
            clickSequence.OnComplete(() => {
                TriggerAction();
            });
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        buttonImage.DOColor(pressedColor, pressDuration);
        
        //长按
        if (triggerType == ButtonTriggerType.LongpPress && _type == ButtonType.shoot)
        {
            EventManager.Instance.SetExternalFire(true);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        buttonImage.DOColor(originalColor, pressDuration);

        // 长按开火松开：清除 holdingButtonFire
        if (triggerType == ButtonTriggerType.LongpPress && _type == ButtonType.shoot)
        {
            EventManager.Instance.SetExternalFire(false);
        }
    }
    
    private void TriggerAction()
    {
        if (_type == ButtonType.shoot)
        {
           EventManager.Instance.FireWeapon();
        }
        else if (_type == ButtonType.anim)
        {
            _animAiming = !_animAiming;
            EventManager.Instance.SetAimingExternal(_animAiming);
        }
        else if (_type == ButtonType.reload)
        {
            EventManager.Instance.TryReload();
        }
        else if (_type == ButtonType.sprint)
        {
            EventManager.Instance.TriggerExternalSprint();
        }
    }
}