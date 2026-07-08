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
    anim
}

public class ButtonClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    ButtonType type;
    
    [SerializeField]
    private ButtonTriggerType triggerType = ButtonTriggerType.SingleClick; // 触发方式
    
    [Header("Button Press Effect")]
    [SerializeField] private float pressDuration = 0.1f;
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Header("Hold Settings")]
    [SerializeField] private float fireRate = 0.1f; // 按住时开火间隔（秒）
    
    private Color originalColor;
    private Image buttonImage;
    private Character character;
    
    private bool isPressed = false;
    private float nextFireTime = 0f;
    
    private void Start()
    {
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage.color;
        character = FindFirstObjectByType<Character>();
    }
    
    private void Update()
    {

        if (triggerType == ButtonTriggerType.LongpPress && isPressed)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                TriggerAction();
            }
        }
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
        // 按住持续触发模式：不需要在PointerClick中处理，已在Update中处理
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        buttonImage.DOColor(pressedColor, pressDuration);
        
        // 按住模式：立即触发第一次
        if (triggerType == ButtonTriggerType.LongpPress)
        {
            nextFireTime = Time.time; // 立即触发第一次
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        buttonImage.DOColor(originalColor, pressDuration);
    }
    
    private void TriggerAction()
    {
        if (type == ButtonType.shoot)
        {
            character.FireWeapon();
        }
        else if (type == ButtonType.anim)
        {
            Debug.Log(type);
            
        }
    }
}