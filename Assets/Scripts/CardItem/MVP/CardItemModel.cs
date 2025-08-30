using UnityEngine;
using Unity.Netcode;

public class CardItemModel : NetworkBehaviour
{
 /*   private void Start()
    {
        //carditemdefdata값 바뀌면 OnCardDefDataChanged 실행
        OnCardDefDataChanged += (newValue) =>
        {
            CardDefData = newValue;
        };

        //carditemstate값 바뀌면 SetStateByCardItemStateEnum() 실행
        OnCardItemStatusDataChanged += (newValue) =>
        {
            SetStateByCardItemStateEnum(newValue.State);
            ApplyStateChange();
        };
        //초기화 실행
        SetStateByCardItemStateEnum(CardItemStatusData.State);
        ApplyStateChange();

        
    }
    private void Update()
    {
        if (curState != null)
        {
            curState.OnStateUpdate();
        }
    }*/

    #region 데이터
    //데이터
    private CardItemData cardItemData;
    public CardItemData CardItemData
    {
        get => cardItemData;
        set
        {
            if (cardItemData.Equals(value)) return;
            cardItemData = value;
        }
    }

    #endregion
/*    #region 카드 상태

    private State preState;
    private State tempState;
    private State curState;


    private void SetStateByCardItemStateEnum(CardItemState inputCardItemState = CardItemState.None)
    {
        switch (inputCardItemState)
        {
            case CardItemState.None:
                SetState(gameObject.AddComponent<CardItemNoneState>());
                break;
            case CardItemState.Sold:
                SetState(gameObject.AddComponent<CardItemSoldState>());
                break;
            default:
                break;
        }
        
    }

    private void SetState(State state)
    {
        tempState = curState;
        curState = state;
        preState = tempState;

        //안 쓰는 컴포넌트 삭제
        foreach (var _state in GetComponents<State>())
        {
            if (_state != curState && _state != preState)
            {
                Destroy(_state);
            }
        }
    }

    

    private void ApplyStateChange()
    {
        if (preState != null)
        {
            preState.OnStateExit();
        }
        
        curState.OnStateEnter();
    }
    #endregion
*/
}
