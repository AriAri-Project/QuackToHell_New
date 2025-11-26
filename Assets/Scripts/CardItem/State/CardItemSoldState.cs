using UnityEngine;

public class CardItemSoldState : StateBase
{
   
    public override void OnStateEnter()
    {
        
        // 카드가 상점에서 진열된 상태인지 확인
        if (gameObject.transform.parent != null && 
            gameObject.transform.CompareTag(GameTags.CardForSale))
        {
            CanvasGroup canvasGroup = GameObjectUtils.GetOrAddComponent<CanvasGroup>(gameObject);
            // 1. 안 보이게 (완전 투명하게)
            canvasGroup.alpha = 0f;
        
            // 2. 입력 막기 (버튼 등이 비활성화되고 클릭 이벤트 무시)
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("[CardItemSoldState] 파괴 조건을 만족하지 않음 - Parent가 null이거나 CardForSale 태그가 아님");
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {
        
    }
}
