using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;


public enum InventorySotringOption
{
    RecentlyAcquired,
}

public class CardInventoryModel : NetworkBehaviour
{
    #region 데이터
    // 로컬 클라이언트의 인벤토리가 소유하는 카드 정보
    private NetworkVariable<List<InventoryCard>> ownedCards = new NetworkVariable<List<InventoryCard>>(new List<InventoryCard>());
    public NetworkVariable<List<InventoryCard>> OwnedCards => ownedCards;
    const int maxCardCount = 20;
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
    public void AddOwnedCardServerRpc(InventoryCard card)
    {   
        if (ownedCards.Value.Count >= maxCardCount)
        {
            Debug.Log("카드 추가 실패: 인벤토리 가득 참");
            return;
        }
        List<InventoryCard> newList = new List<InventoryCard>(ownedCards.Value);
        newList.Add(card);
        ownedCards.Value = newList;
        Debug.Log($"[CardInventoryModel] 카드 추가 성공: {card.CardID}");
    }

    [ServerRpc]
    public void RemoveOwnedCardServerRpc(int cardItemId)
    {
        for (int i = 0; i < ownedCards.Value.Count; i++)
        {
            if (ownedCards.Value[i].Status.CardItemID == cardItemId)
            {
                List<InventoryCard> newList = new List<InventoryCard>(ownedCards.Value);
                newList.RemoveAt(i);
                ownedCards.Value = newList;
                break;
            }
        }
        Debug.Log($"[CardInventoryModel] 카드 삭제 성공: {cardItemId}");
    }
    #endregion

    #region 정렬
    public void SortCardsByAcquiredTicks()
    {
        // 간단한 정렬: 최근 획득 순
        var sortedList = new List<InventoryCard>(ownedCards.Value);
        sortedList.Sort((a, b) => b.AcquiredTicks.CompareTo(a.AcquiredTicks));
        
        // NetworkList 업데이트
        ownedCards.Value.Clear();
        foreach (var card in sortedList)
        {
            ownedCards.Value.Add(card);
        }
    }
    #endregion
}
