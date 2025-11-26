using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // UI 클릭을 위해 Image 권장
public class ClickSpotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Order")]
    [Tooltip("순서 모드일 때 이 인덱스 순서로 클릭해야 함")]
    [SerializeField] private int orderIndex = 0;

    [Header("Visual (Optional)")]
    [Tooltip("클릭 완료 시 켜질 추가 표시(체크마크 등). 없어도 됨")]
    [SerializeField] private GameObject onClickedVisual;
    [Tooltip("잘못된 순서 클릭 시 깜빡이는 연출(선택)")]
    [SerializeField] private Animation wrongOrderAnim;

    [Header("Sprite Swap")]
    [Tooltip("스프라이트를 교체할 타겟 Image. 비우면 자기 Image를 사용")]
    [SerializeField] private Image targetImage;
    [Tooltip("꺼진 상태 스프라이트(예: 촛대_꺼짐)")]
    [SerializeField] private Sprite offSprite;
    [Tooltip("켜진 상태 스프라이트(예: 촛대_켜짐)")]
    [SerializeField] private Sprite onSprite;
    [Tooltip("스프라이트 교체 시 SetNativeSize 호출")]
    [SerializeField] private bool setNativeSizeOnSwap = false;

    [Header("Tint (Optional)")]
    [SerializeField] private Color offColor = Color.white;
    [SerializeField] private Color onColor = Color.white;

    private ClickOncePointsGame _manager;
    private bool _clicked;

    public int OrderIndex => orderIndex;
    public bool WasClicked => _clicked;

    public void Init(ClickOncePointsGame manager)
    {
        _manager = manager;
        if (!targetImage) targetImage = GetComponent<Image>();
        ResetState();
    }

    public void ResetState()
    {
        _clicked = false;

        if (onClickedVisual) onClickedVisual.SetActive(false);

        if (!targetImage) targetImage = GetComponent<Image>();
        if (targetImage)
        {
            if (offSprite) targetImage.sprite = offSprite;
            targetImage.color = offColor;
            if (setNativeSizeOnSwap && offSprite) targetImage.SetNativeSize();
        }
    }

    public void MarkClicked()
    {
        _clicked = true;

        if (onClickedVisual) onClickedVisual.SetActive(true);

        if (targetImage)
        {
            if (onSprite) targetImage.sprite = onSprite;
            targetImage.color = onColor;
            if (setNativeSizeOnSwap && onSprite) targetImage.SetNativeSize();
        }
    }

    public void PlayWrongOrderFeedback()
    {
        if (wrongOrderAnim) wrongOrderAnim.Play();
        else
        {
            if (targetImage) targetImage.color = new Color(1f, 0.85f, 0.85f, 1f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_clicked) return;
        _manager?.NotifySpotClicked(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Order++")] private void CtxOrderPlus() => orderIndex++;
    [ContextMenu("Order--")] private void CtxOrderMinus() => orderIndex--;
#endif
}

