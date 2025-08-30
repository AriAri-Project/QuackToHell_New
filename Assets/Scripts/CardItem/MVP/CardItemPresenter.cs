using UnityEngine;
using System;

public class CardItemPresenter : MonoBehaviour
{

    private void Start()
    {
        //구매 클릭 이벤트 바인딩
        cardItemView.OnPurchaseClicked += CardItemView_OnPurchaseClicked;
        //값 변경에 대해 바인딩
        cardItemModel.OnCardDefDataChanged += CardDefData_OnValueChanged;
        cardItemModel.OnCardItemStatusDataChanged += CardItemStatusData_OnValueChanged;

        //외향 초기화
        CardDefData_OnValueChanged(cardItemModel.CardDefData);
        CardItemStatusData_OnValueChanged(cardItemModel.CardItemStatusData);

        //카드 판매 가격 초기화
        cardItemView.SetCardForSaleAppearence(cardItemModel.CardItemStatusData.Price);
        //카드 아이템 id 초기화
        cardItemView.SetCardItemIdAppearence(cardItemModel.CardItemStatusData.CardItemID);
    }

    #region 모델, 뷰 참조
    private CardItemModel cardItemModel;
    private CardItemView cardItemView;
    private void Awake()
    {
        cardItemModel = GetComponent<CardItemModel>();
        cardItemView = GetComponent<CardItemView>();
    }
    #endregion

    #region 외향


    private void CardDefData_OnValueChanged(CardDef cardDefData)
    {
        cardItemView.SetCardItemNameAppearence(cardDefData.CardNameKey.ToString(), cardDefData.Tier);
        cardItemView.SetCardItemImageAppearence(cardDefData.Tier, cardDefData.Type);
        cardItemView.SetCardTypeAppearence(cardDefData.Map_Restriction, cardDefData.Type);
        cardItemView.SetCardDefinitionAppearence(cardDefData.DescriptionKey.ToString());
        cardItemView.SetCardCharacteristicAppearence(cardItemModel.CardItemStatusData.Cost, cardDefData.Type, cardDefData.Map_Restriction);
    }
    private void CardItemStatusData_OnValueChanged(CardStatusData cardItemStatusData)
    {
        cardItemView.SetCardCharacteristicAppearence(cardItemStatusData.Cost, cardItemModel.CardDefData.Type, cardItemModel.CardDefData.Map_Restriction);
        cardItemView.SetCardForSaleAppearence(cardItemStatusData.Price);
        cardItemView.SetCardItemIdAppearence(cardItemStatusData.CardItemID);
    }
    #endregion

    #region 구매 클릭 입력 이벤트 전달
    private CardShopPresenter cardShopPresenter;
    private void CardItemView_OnPurchaseClicked(ulong inputClientId)
    {
        Debug.Log("[CardItemPresenter] cardShopPresenter.TryPurchaseCard 호출");
        InventoryCard card = new InventoryCard
        {
            CardID = cardItemModel.CardItemStatusData.CardID,
            CardItemId = cardItemModel.CardItemStatusData.CardItemID,
            Status = cardItemModel.CardItemStatusData,
            AcquiredTicks = DateTime.UtcNow.Ticks
        };
        // TODO:CardShop에게 카드 구매 요청 : 카드 아이디, 플레이어 아이디, 카드 가격 보내주기
        cardShopPresenter = GameObject.FindObjectOfType<CardShopPresenter>();
        cardShopPresenter.TryPurchaseCard(card, inputClientId);
    }
    #endregion
}
