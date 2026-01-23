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
        public Action<CardItemData, ulong> OnSellRequested;

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
            cardItemView.OnSellClicked += CardItemView_OnSellClicked;

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
                cardItemView.OnSellClicked -= CardItemView_OnSellClicked;
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
            string localizedName = cardDef.cardName.ToString();
            string localizedDescription = cardDef.description.ToString();
            
            if (DeckManager.Instance != null && DeckManager.Instance.CardDefinitionCount > 0)
            {
                if (DeckManager.Instance.TryGetCardDisplay(cardDef.cardID, "ko", out CardDisplay display))
                {
                    localizedName = display.name;
                    localizedDescription = display.description;
                }
            }

            
            
            // 모든 외관 요소 한 번에 설정
            //카드아이콘
            cardItemView.SetCardIcon(cardData.cardDef.cardIconResourcePath.ToString());
            //카드분류
            //숫자면 숫자도 표시
            if (cardData.cardDef.type == TypeEnum.Number) 
            {
                //숫자 값에 따라 다르게 색 부여
                if (cardData.cardDef.Value.Equals(CardValue.V1) || cardData.cardDef.Value.Equals(CardValue.V2))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(0.788f, 0.655f, 0.518f, 1f));
                    cardItemView.SetCardType("숫자", new Color(0.788f, 0.655f, 0.518f, 1f));
                    cardItemView.SetIconText(cardData.cardDef.Value.ToString().Substring(1));
                }
                if (cardData.cardDef.Value.Equals(CardValue.V3) || cardData.cardDef.Value.Equals(CardValue.V4))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(0.941f, 0.941f, 0.941f, 1f));
                    cardItemView.SetCardType("숫자",new Color(0.941f, 0.941f, 0.941f, 1f) );
                    cardItemView.SetIconText(cardData.cardDef.Value.ToString().Substring(1));
                }
                if (cardData.cardDef.Value.Equals(CardValue.V5) || cardData.cardDef.Value.Equals(CardValue.V6)|| cardData.cardDef.Value.Equals(CardValue.V0))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(1f, 0.765f, 0f, 1f) );
                    cardItemView.SetCardType("숫자",new Color(1f, 0.765f, 0f, 1f) );
                    cardItemView.SetIconText(cardData.cardDef.Value.ToString().Substring(1));
                }
                //미지숫자
                if (cardData.cardDef.Value.Equals(CardValue.N))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(0.988f, 0.835f, 1f, 1f));
                    cardItemView.SetCardType("미지 숫자",new Color(0.988f, 0.835f, 1f, 1f) );
                    cardItemView.SetIconText("N");
                }
            }
            //기호
            if (cardData.cardDef.type == TypeEnum.Operator)
            {
                //금
                if (cardData.cardDef.tier == TierEnum.Special)
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(1f, 0.765f, 0f, 1f));
                    cardItemView.SetCardType("기호",new Color(1f, 0.765f, 0f, 1f) );
                   
                }
                
                //은
                if (cardData.cardDef.tier == TierEnum.Rare)
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), Color.white);
                    cardItemView.SetCardType("기호",Color.white );
                }
               
                //동
                if (cardData.cardDef.tier == TierEnum.Common)
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(0.788f, 0.655f, 0.518f, 1f));
                    cardItemView.SetCardType("기호",new Color(0.788f, 0.655f, 0.518f, 1f));
                }
                
                //기호 이름 따라 부호 다르게 input
                switch (cardData.cardDef.Value)
                {
                    case CardValue.ADD:
                        cardItemView.SetIconText("+");
                        break;
                    case CardValue.DIV :
                        cardItemView.SetIconText("÷");
                        break;
                    case CardValue.SUB :
                        cardItemView.SetIconText("-");
                        break;
                    case  CardValue.MULT:
                        cardItemView.SetIconText("x");
                        break;
                }
                
               
            } 
            //반란
            if (cardData.cardDef.type == TypeEnum.Special)
            {
                cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(1f, 0.608f, 0.157f, 1f));
                cardItemView.SetCardType("반란",new Color(1f, 0.608f, 0.157f, 1f));
            }
            //직업
            if (cardData.cardDef.type == TypeEnum.Roll)
            {
                //마피아
                if (cardData.cardDef.subType.Equals(SubTypeEnum.Farmer))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(),new Color(1f, 0.361f, 0.361f, 1f));
                    cardItemView.SetCardType("직업",new Color(1f, 0.361f, 0.361f, 1f));
                }
                
                //시민
                if (cardData.cardDef.subType.Equals(SubTypeEnum.Animal))
                {
                    cardItemView.SetCardName(cardData.cardDef.cardName.ToString(), new Color(0.361f, 1f, 0.404f, 1f));
                    cardItemView.SetCardType("직업",new Color(0.361f, 1f, 0.404f, 1f));
                }
            }
            //카드설명
            cardItemView.SetCardExplain(cardData.cardDef.description.ToString());

            //카드 일러스트
            cardItemView.SetCardBG(cardData.cardDef.cardIImagePath.ToString());
            
            //가격
            cardItemView.SetCardPrice(cardData.cardItemStatusData.price.ToString());
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

        private void CardItemView_OnSellClicked(ulong inputClientId)
        {
            CardItemData myCardItemData = cardItemModel.CardItemData;
            Debug.Log($"[CardItemPresenter] Sell requested cardItemID={myCardItemData.cardItemStatusData.cardItemID}");
            OnSellRequested?.Invoke(myCardItemData, inputClientId);
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
