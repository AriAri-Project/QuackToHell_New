using System;
using UnityEngine;

namespace CardItem.MVP
{
    public class CardItemPresenter : MonoBehaviour
    {
        [Header("Components")]
        private CardItemModel cardItemModel;
        private CardItemView cardItemView;
        /*
        [Header("References")]
        private CardShopPresenter _cardShopPresenter;
        public CardShopPresenter CardShopPresenter
        {
            set { _cardShopPresenter = value; }
        }
        */
        
        [Header("Events")]
        public Action<CardItemData, ulong> OnPurchaseRequested;

        private void Awake()
        {
            cardItemModel = GetComponent<CardItemModel>();
            cardItemView = GetComponent<CardItemView>();
                
            DebugUtils.AssertComponent(cardItemModel, "CardItemModel", this);
            DebugUtils.AssertComponent(cardItemView, "CardItemView", this);
        }

        private void Start()
        {
            
            //구매 클릭 이벤트 바인딩
            cardItemView.OnPurchaseClicked += CardItemView_OnPurchaseClicked;
            
            // 카드 데이터 변경 이벤트 바인딩
            cardItemModel.OnCardDataChanged += OnCardItemDataChanged;

            //외향 초기화
            UpdateCardAppearance(cardItemModel.CardItemData);
        }

        private void OnDestroy()
        {    
            if (cardItemView != null)
            {
                cardItemView.OnPurchaseClicked -= CardItemView_OnPurchaseClicked;
            }
    
            if (cardItemModel != null)
            {
                cardItemModel.OnCardDataChanged -= OnCardItemDataChanged;
            }
        }

        #region 외향
        /// <summary>
        /// 카드 외관을 업데이트
        /// </summary>
        private void UpdateCardAppearance(CardItemData cardData)
        {
            CardDef cardDef = cardData.cardDef;
            CardStatusData statusData = cardData.cardItemStatusData;
            
            // 로컬라이제이션 처리
            string localizedName = cardDef.cardNameKey.ToString();
            string localizedDescription = cardDef.descriptionKey.ToString();
            
            if (DeckManager.Instance != null && DeckManager.Instance.CardDefinitionCount > 0)
            {
                if (DeckManager.Instance.TryGetCardDisplay(cardDef.cardID, "ko", out CardDisplay display))
                {
                    localizedName = display.name;
                    localizedDescription = display.description;
                }
            }
            
            // 모든 외관 요소 한 번에 설정
            cardItemView.SetCardItemNameAppearence(localizedName, cardDef.tier);
            cardItemView.SetCardItemImageAppearence(cardDef.tier, cardDef.type);
            cardItemView.SetCardTypeAppearence(cardDef.mapRestriction, cardDef.type);
            cardItemView.SetCardDefinitionAppearence(localizedDescription);
            cardItemView.SetCardCharacteristicAppearence(statusData.cost, cardDef.type, cardDef.mapRestriction);
            cardItemView.SetCardForSaleAppearence(statusData.price);
            cardItemView.SetCardItemIdAppearence(statusData.cardItemID);
        }



        
        #endregion

        #region 구매 클릭 입력 이벤트 전달
        private void CardItemView_OnPurchaseClicked(ulong inputClientId)
        {
            CardItemData myCardItemData = cardItemModel.CardItemData;
            /*
            // CardShop에게 카드 구매 요청
            if (DebugUtils.AssertNotNull(_cardShopPresenter, "CardShopPresenter", this))
            {
                _cardShopPresenter.TryPurchaseCard(myCardItemData, inputClientId);
            }
            */
            OnPurchaseRequested?.Invoke(myCardItemData, inputClientId);
        }

        /// <summary>
        /// 카드 데이터 변경 시 호출되는 이벤트 핸들러
        /// </summary>
        private void OnCardItemDataChanged(CardItemData previousValue, CardItemData newValue)
        {
            // 상태가 변경되었을 때 UI 업데이트
            if (previousValue.cardItemStatusData.state != newValue.cardItemStatusData.state)
            {
                UpdateCardAppearance(newValue);     
                
            }
        }
        #endregion

        
    }
}
