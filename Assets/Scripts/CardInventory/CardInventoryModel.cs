using UnityEngine;
using Unity.Netcode;


public enum InventorySotringOption
{
    RecentlyAcquired,
}

public class CardInventoryModel : NetworkBehaviour
{
    #region 데이터
    // 로컬 클라이언트의 인벤토리가 소유하는 카드 정보
    private NetworkList<CardItemData> ownedCards = new NetworkList<CardItemData>();
    public NetworkList<CardItemData> OwnedCards => ownedCards;
    private ulong myClientId;
    private InventorySotringOption _sortingOption = InventorySotringOption.RecentlyAcquired;
    public InventorySotringOption SortingOption => _sortingOption;
    // TODO: 필요하면, cardCount 추가

    #endregion

    #region 초기화
    private void Start()
    {
        myClientId = NetworkManager.Singleton.LocalClientId;
    }
    #endregion

    #region InventoryCard 데이터 추가, 삭제 메서드
    [ServerRpc]
    public void AddOwnedCardServerRpc(CardItemData card)
    {  
        if (ownedCards.Count >= GameConstants.Card.maxCardCount)
        {
            return;
        }
        ownedCards.Add(card);
    }

    [ServerRpc]
    public void RemoveOwnedCardServerRpc(CardItemData card)
    {
        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (ownedCards[i].cardItemStatusData.cardItemID == card.cardItemStatusData.cardItemID)
            {
                ownedCards.RemoveAt(i);
                break;
            }
        }
    }
    #endregion

    #region 정렬
    //TODO: 정렬 버튼 생길 시 옵션에 따른 정렬 메서드 추가
    
    /*public void SortCardsByAcquiredTicks()
    {

        // NetworkList는 직접 정렬할 수 없으므로, 임시 리스트로 정렬 후 다시 추가
        List<CardItemData> sortedList = new List<CardItemData>();
        foreach (CardItemData card in ownedCards)
        {
            sortedList.Add(card);
        }
        
        sortedList.Sort((a, b) => b.AcquiredTicks.CompareTo(a.AcquiredTicks));
        
        // NetworkList 업데이트
        ownedCards.Clear();
        foreach (CardItemData card in sortedList)
        {
            ownedCards.Add(card);
        }
    }*/
    
    #endregion
    
    #region 외부 인터페이스 (메시지 기반)
    
    
    /// <summary>
    /// 소유한 카드 수 조회
    /// </summary>
    public int GetOwnedCardCount()
    {
        return ownedCards.Count;
    }
    
    /// <summary>
    /// 특정 카드 소유 여부 조회
    /// </summary>
    public bool HasCard(int cardId)
    {
        if (ownedCards == null) return false;
        
        foreach (CardItemData card in ownedCards)
        {
            if (card.cardItemStatusData.cardItemID == cardId)
                return true;
        }
        return false;
    }

    public bool IsInventoryMaximum()
    {
        if (ownedCards.Count == GameConstants.Card.maxCardCount)
        {
            return true;
        }

        return false;
    }


    
    #endregion
}
