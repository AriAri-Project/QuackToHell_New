using UnityEngine;
using System;

namespace CardItem.MVP
{
    public class CardItemModel : MonoBehaviour
    {
       

        #region 데이터
        private CardItemData _cardItemData;
        public CardItemData CardItemData => _cardItemData;
        public event Action<CardItemData, CardItemData> OnCardDataChanged;


        public void UpdateCardStateFromServer(CardItemData newData)
        {
            // 이전 값을 저장
            CardItemData previousValue = _cardItemData;
            
            // 서버에서 직접 수정하거나, 클라이언트에서 서버 동기화 받기
            _cardItemData = newData;

            // 이벤트 발생 
            OnCardDataChanged?.Invoke(previousValue, newData);
            
            // 상태 업데이트
            OnCardItemDataChanged(previousValue, newData);
        }

        //(CardItemPresenter용)

        public void SetCardData(CardItemData newData)
        {
            UpdateCardStateFromServer(newData);
        }

        #endregion

        #region 카드 상태

        // 상태 컴포넌트들 (미리 생성)
        private CardItemNoneState noneStateComponent;
        private CardItemSoldState soldStateComponent;
        private CardItemSoldingState soldingStateComponent;

        private StateBase preState;
        private StateBase tempState;
        private StateBase curState;

        private void Start()
        {
            // 미리 부착된 컴포넌트들 참조
            noneStateComponent = GetComponent<CardItemNoneState>();
            soldStateComponent = GetComponent<CardItemSoldState>();
            soldingStateComponent = GetComponent<CardItemSoldingState>();

            // 초기 상태 설정
            if (_cardItemData.cardItemStatusData.State != CardItemState.None)
            {
                SetStateByCardItemStateEnum(_cardItemData.cardItemStatusData.State);
                ApplyStateChange();
            }
        }

        private void OnCardItemDataChanged(CardItemData previousValue, CardItemData newValue)
        {
            // 상태가 변경되었을 때만 상태 업데이트
            if (previousValue.cardItemStatusData.State != newValue.cardItemStatusData.State)
            {
                SetStateByCardItemStateEnum(newValue.cardItemStatusData.State);
            }
            
        }

        private void SetStateByCardItemStateEnum(CardItemState inputCardItemState = CardItemState.None)
        {
            switch (inputCardItemState)
            {
                case CardItemState.None:
                    SetState(noneStateComponent);
                    break;
                case CardItemState.Sold:
                    SetState(soldStateComponent);
                    break;
                case CardItemState.Solding:
                    SetState(soldingStateComponent);
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

            // 이전 상태 비활성화
            if (preState != null)
            {
                preState.enabled = false;
            }

            // 현재 상태 활성화
            if (curState != null)
            {
                curState.enabled = true;
            }

            // 상태 변경 적용 (OnStateEnter/OnStateExit 호출)
            ApplyStateChange();
        }

        private void ApplyStateChange()
        {
            string preStateName = preState?.GetType().Name ?? "null";
            string curStateName = curState?.GetType().Name ?? "null";
            
            if (preState != null)
            {
                preState.OnStateExit();
            }
            
            if (curState != null)
            {
                curState.OnStateEnter();
            }
        }

        private void Update()
        {
            if (curState != null)
            {
                curState.OnStateUpdate();
            }
        }

        #endregion
        #region 외부 인터페이스 (메시지 기반)
        
        
        /// <summary>
        /// 카드 가격 조회
        /// </summary>
        public int GetCardPrice()
        {
            return _cardItemData.cardItemStatusData.price;
        }
        
        /// <summary>
        /// 카드 구매 가능 여부 조회
        /// </summary>
        public bool IsPurchasable()
        {
            return _cardItemData.cardItemStatusData.state == CardItemState.None;
        }
        
        #endregion
    }
    
}
