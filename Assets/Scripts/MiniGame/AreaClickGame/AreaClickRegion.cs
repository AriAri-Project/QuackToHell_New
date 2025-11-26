using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AreaClickRegion : MonoBehaviour, IPointerClickHandler
{
    
    [Header("Hit By Image Alpha")]
    [Tooltip("이 값보다 알파가 큰 픽셀만 클릭 판정 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaThreshold = 0.1f;

    [Tooltip("알파 히트 테스트를 사용할지 (UI-Image의 내장 기능)")]
    [SerializeField] private bool useAlphaHitTest = true;

    public event Action<Vector2> OnRegionClicked; // screen position

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (_image == null)
        {
            Debug.LogError("[AreaClickRegion] Image 컴포넌트가 필요합니다.", this);
            enabled = false;
            return;
        }

        if (useAlphaHitTest)
        {
            // Unity UI Image의 알파 히트 테스트 기능 사용
            // (Sprite가 아틀라스일 때도 동작하지만, Import 설정에 따라 다를 수 있음)
            _image.alphaHitTestMinimumThreshold = alphaThreshold;
            _image.raycastTarget = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // useAlphaHitTest=true면 여기 도달 시 이미 알파 통과 상태
        // false인 경우도 그냥 허용 (사각형 전체 영역이 클릭영역)
        Debug.Log($"[Region] click {eventData.position}");
        OnRegionClicked?.Invoke(eventData.position);
    }
}
