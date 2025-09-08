using UnityEngine;
using Unity.Netcode;
using System;

public class CardItemModel : MonoBehaviour
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

    private GameObject CardForSaleParent;

    private void Start()
    {
        //찾기
        CardForSaleParent = GameObject.Find("CardForSaleParent");

        gameObject.transform.SetParent(CardForSaleParent.transform);

        //비활성화
        gameObject.SetActive(false); 
    }

    #region 데이터
    //데이터
    private NetworkVariable<CardItemData> cardItemData = new NetworkVariable<CardItemData>();
    public NetworkVariable<CardItemData> CardItemData
    {
        get => cardItemData;
        set
        {
            cardItemData.Value = value.Value;
            DeckManager.Instance.RequestUpdateAllCardsOnGameDataServerRpc(cardItemData.Value);
        }
    }

    #endregion
/*    #region 카드 상태

    private StateBase preState;
    private StateBase tempState;
    private StateBase curState;


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

    private void SetState(StateBase state)
    {
        tempState = curState;
        curState = state;
        preState = tempState;

        //안 쓰는 컴포넌트 삭제
        foreach (var _state in GetComponents<StateBase>())
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
