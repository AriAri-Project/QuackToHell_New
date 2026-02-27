using UnityEngine;
using UnityEngine.EventSystems;
using Court;      // TrialManager, PlayerTrialState가 있는 곳
using Court.Hand; // TrialCardView가 있는 곳

// 드래그 인터페이스 구현
public class EndSpeechCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private TrialCardView _cardView;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent; // 필요시 부모 복귀용

    private void Start()
    {
        _cardView = GetComponent<TrialCardView>();
        
        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_canvasGroup) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. TrialCardView에게 "잠깐 위치 잡지 마!" 라고 명령
        if (_cardView != null) 
        {
            _cardView.IsAutoLayoutEnabled = false;
        }

        // 2. 드롭 감지를 위해 레이캐스트 끄기
        _canvasGroup.blocksRaycasts = false; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 3. 마우스 위치로 직접 이동 (기존 카드는 이 코드가 없어서 안 움직임)
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        // 4. 드롭 로직 수행 (발언 마치기)
        bool isUsed = TryUseCard(eventData);

        if (isUsed)
        {
            // 사용 성공 -> 카드 파괴
            Destroy(gameObject);
        }
        else
        {
            // 사용 실패(취소) -> 다시 TrialCardView에게 제어권 반납
            if (_cardView != null) 
            {
                // 이걸 true로 하면 다음 프레임 Update부터 원래 자리(부채꼴)로 스르륵 돌아감
                _cardView.IsAutoLayoutEnabled = true; 
            }
        }
    }

    private bool TryUseCard(PointerEventData eventData)
    {
        // TODO: 만약 특정 영역(테이블 등) 위에 놨을 때만 발동하게 하려면 여기서 Raycast 체크
        // 지금은 "놓으면 무조건 발동"으로 처리
        
        bool hasManager = TrialManager.Instance != null;
        bool hasLocalPlayer = hasManager && TrialManager.Instance.LocalPlayer != null;
        Debug.Log($"[EndSpeechCard] TryUseCard - hasManager:{hasManager}, hasLocalPlayer:{hasLocalPlayer}");

        if (hasManager && hasLocalPlayer)
        {
            ulong ownerClientId = TrialManager.Instance.LocalPlayer.OwnerClientId;
            Debug.Log($"[EndSpeechCard] 발언 종료! OwnerClientId:{ownerClientId}");
            try
            {
                TrialManager.Instance.LocalPlayer.EndSpeech();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EndSpeechCard] EndSpeech 예외 발생: {ex}");
            }
            return true;
        }
        
        return false;
    }
}