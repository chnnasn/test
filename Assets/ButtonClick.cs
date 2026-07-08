using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using InfimaGames.LowPolyShooterPack;

public enum ButtonType
{   shoot,
    anim
}


public class ButtonClick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    ButtonType type;
    
    [Header("Button Press Effect")]
    [SerializeField] private float pressDuration = 0.1f;
    [SerializeField] private Color pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f); // 灰色
    
    private Color originalColor;
    private Image buttonImage;
    
    private Character character;
    private void Start()
    {
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage.color;
        
        character = FindFirstObjectByType<Character>();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.rawPointerPress != null && eventData.rawPointerPress != gameObject) 
            return;
    
        Sequence clickSequence = DOTween.Sequence();
        clickSequence.Append(buttonImage.DOColor(originalColor, pressDuration));
    
        clickSequence.OnComplete(() => {
            if (type == ButtonType.shoot)
            {
                character.FireWeapon();
            }
            else if(type == ButtonType.anim)
            {
                Debug.Log(type);    
            }
        });
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        
        buttonImage.DOColor(pressedColor, pressDuration);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        
        buttonImage.DOColor(originalColor, pressDuration);
    }
}
