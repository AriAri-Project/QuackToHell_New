using UnityEngine;
using System.Collections.Generic;
using CardItem.MVP;

public class CardShopModel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cardForSaleParent;
    [SerializeField] private Transform rowObjectTransform;
    [SerializeField] private Transform cardForSaleParentTransform;
    
    [Header("Shop Data")]
    public bool IsLocked { get; set; }

    private enum Rarity { Common, Rare }
    const int maximumDisplayCount = 5;
    private int totalCardCountOnMap = 0;

    private void Awake()
    {
        CardItemFactoryManager.Instance.CreateTotalCardForSale(cardForSaleParent);
        totalCardCountOnMap = cardForSaleParent.transform.childCount;
    }
    

    #region 카드 목록 새로고침

    public bool TryReRoll(ulong clientId){
        if(IsLocked) return false;
        DeckManager.Instance.RequestDisplayCardsServerRpc(clientId);
        return true;
    }


    /// <summary>
    /// 서버에서 진열 결과를 받아서 UI 업데이트
    /// </summary>
    public void UpdateDisplayFromServer(CardItemData[] displayedCards)
    {
        // 기존 진열된 카드들 정리
        ClearCurrentDisplay();

        // 서버에서 선택된 카드들을 진열
        foreach (CardItemData cardData in displayedCards)
        {
            // 해당 카드 오브젝트 찾기
            Transform cardObject = FindCardObjectByCardItemId(cardData.cardItemStatusData.cardItemID);
            if (cardObject != null)
            {
                // Row로 이동하여 진열
                cardObject.SetParent(rowObjectTransform, false);
                cardObject.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 현재 진열된 카드들을 정리
    /// </summary>
    private void ClearCurrentDisplay()
    {
        int childCount = rowObjectTransform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = rowObjectTransform.GetChild(0);
            child.SetParent(cardForSaleParentTransform, false);
            child.gameObject.SetActive(false);
        }   
    }

    /// <summary>
    /// CardItemID로 카드 오브젝트 찾기
    /// </summary>
    private Transform FindCardObjectByCardItemId(int cardItemId)
    {
        if (cardForSaleParentTransform == null) return null;
        
        for (int i = 0; i < cardForSaleParentTransform.childCount; i++)
        {
            Transform card = cardForSaleParentTransform.GetChild(i);
            if (card == null) continue;
            
            CardItemModel cardItemModel = card.GetComponent<CardItemModel>();
            if (cardItemModel != null && 
                cardItemModel.CardItemData.cardItemStatusData.cardItemID == cardItemId)
            {
                return card;
            }
        }
        return null;
    }

    public bool HasDisplayedCards()
    {
        return rowObjectTransform != null && rowObjectTransform.childCount > 0;
    }
    #endregion
}
