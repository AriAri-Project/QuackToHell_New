using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public struct TotalCardsOnGameData : INetworkSerializable, IEquatable<TotalCardsOnGameData>
{
    public int cardId;
    public int cardTotalCount;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardId);
        serializer.SerializeValue(ref cardTotalCount);
    }

    public bool Equals(TotalCardsOnGameData other)
    {
        return cardId == other.cardId && cardTotalCount == other.cardTotalCount;
    }
    
    public override bool Equals(object obj)
    {
        return obj is TotalCardsOnGameData other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(cardId, cardTotalCount);
    }
}



/// <summary>
/// 덱 매니저: 게임 내 전체 카드 매물을 관리하는 매니저
/// 
/// 책임:
/// - 게임 내 전체 카드 매물 관리 (수량, 가격, 재고)
/// - 카드 구매/판매 처리 및 검증
/// - 게임 내 존재하는 카드 데이터 조회 및 제공
/// 
/// 주의: 
/// - 골드 차감은 GameManager에 위임
/// - 실제 카드 데이터는 TotalCardsOnGame에서 카드 키 조회하여 가져와 사용하기
/// </summary>
public class DeckManager : NetworkBehaviour
{
    #region 싱글톤 코드
    //싱글톤 코드
    private static DeckManager _instance;
    public static DeckManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DeckManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DeckManager");
                    _instance = go.AddComponent<DeckManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 게임 내 전체 카드 매물 관리(덱 관리)
    private NetworkList<TotalCardsOnGameData> _totalCardsOnGame = new NetworkList<TotalCardsOnGameData>();
    public NetworkList<TotalCardsOnGameData> TotalCardsOnGame => _totalCardsOnGame;



    public async Task SetTotalCardsOnGame(CardKeyValuePair[] cardKeyValuePairs)
    {
        if (!IsHost)
        {
            return;
        }

        if (cardKeyValuePairs == null)
        {
            Debug.LogError("[CardItemFactory] 전달받은 카드 데이터가 null입니다.");
            return;
        }

        _totalCardsOnGame.Clear();

        foreach (var card in cardKeyValuePairs)
        {
            var cardData = new TotalCardsOnGameData
            {
                cardId = card.Key,
                cardTotalCount = card.Value.AmountOfCardItem
            };
            _totalCardsOnGame.Add(cardData);

        }

        Debug.Log($"[DeckManager] {_totalCardsOnGame.Count}개 카드 데이터 설정 완료");
        foreach (var card in _totalCardsOnGame)
        {
            Debug.Log($"[DeckManager] CardID: {card.cardId}, Amount: {card.cardTotalCount}");
        }

    }

    public int GetCardTotalCount(int cardId)
    {
        foreach (var cardData in _totalCardsOnGame)
        {
            if (cardData.cardId == cardId)
            {
                return cardData.cardTotalCount;
            }
        }
        Debug.LogError($"[DeckManager] CardID {cardId}가 TotalCardsOnGame에 존재하지 않습니다.");
        return -1;
    }

    #endregion


    #region 카드 구매 처리

    [ServerRpc(RequireOwnership = false)]
    public void TryPurchaseCardServerRpc(InventoryCard card, ulong clientId)
    {
        CardShopPresenter cardShopPresenter;
        cardShopPresenter = GameObject.FindObjectOfType<CardShopPresenter>();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // 해당 카드가 존재하는지 확인
        TotalCardsOnGameData cardData = default;
        bool found = false;

        for (int i = 0; i < _totalCardsOnGame.Count; i++)
        {
            if (_totalCardsOnGame[i].cardId == card.CardID)
            {
                cardData = _totalCardsOnGame[i];
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError($"[DeckManager] CardID {card.CardID}가 TotalCardsOnGame에 존재하지 않습니다.");
            cardShopPresenter.PurchaseCardResultClientRpc(false, clientRpcParams);
            return;
        }



        //만약 물량 없으면 리턴 
        if (cardData.cardTotalCount <= 0)
        {
            Debug.Log($"[DeckManager] CardID {card.CardID}의 물량이 없습니다. 구매 실패");
            //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
            cardShopPresenter.PurchaseCardResultClientRpc(false, clientRpcParams);
            //구매 실패 여부를 클라이언트에게 전달. (ClientRPC, InventoryCard값 보내기)
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);

            return;
        }

        // 플레이어 골드 확인
        int playerGold = PlayerHelperManager.Instance.GetPlayerGoldByClientId(clientId);
        if (playerGold < card.Status.Price)
        {
            //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
            cardShopPresenter.PurchaseCardResultClientRpc(false, clientRpcParams);
            //구매 실패 여부를 클라이언트에게 전달. (ClientRPC, InventoryCard값 보내기)
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
            return;
        }

        // 물량 감소 (NetworkList 동기화)
        var index = _totalCardsOnGame.IndexOf(cardData);
        var newCardData = new TotalCardsOnGameData
        {
            cardId = cardData.cardId,
            cardTotalCount = cardData.cardTotalCount - 1
        };
        _totalCardsOnGame[index] = newCardData;

        // GameManager에게 해당 클라이언트의 골드 차감 요청 (책임 분리)
        GameManager.Instance.DeductPlayerGoldServerRpc(clientId, card.Status.Price);

        Debug.Log($"[DeckManager] Player {clientId} purchased card {card.CardID} for {card.Status.Price} gold");

        //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
        cardShopPresenter.PurchaseCardResultClientRpc(true, clientRpcParams);
        //구매 성공 여부를 클라이언트에게 전달. (ClientRPC, InventoryCard값 보내기)
        PurchaseCardResultClientRpc(true, card, clientId, clientRpcParams);


    }

    [ClientRpc]
    private void PurchaseCardResultClientRpc(bool success, InventoryCard card, ulong clientId, ClientRpcParams sendParams = default)
    {
        if (!success)
        {
            Debug.Log("[DeckManager] 카드 구매 실패");
            return;
        }
        
        // 카드 상태 주입 (Sold) - 기존 cardItemId 그대로 사용
        var updatedCard = card;
        updatedCard.Status.State = CardItemState.Sold;

        //카드 획득 시간 주입
        updatedCard.AcquiredTicks = DateTime.Now.Ticks;

        // 해당 플레이어 인벤토리에 카드 추가
        PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(clientId)?.GetComponent<CardInventoryModel>().AddOwnedCardServerRpc(updatedCard);

        //해당 carditemid에 대한 CardForSale 프리팹에 대해서 sold상태로 전환
        string findCardForSaleString = $"CardForSale_{card.CardItemId}";
        GameObject cardForSale = GameObject.Find(findCardForSaleString);
        if (cardForSale != null)
        {
            CardItemModel cardItemModel = cardForSale.GetComponentInChildren<CardItemModel>();
            if (cardItemModel != null)
            {
                // 새로운 CardItemStatusData 인스턴스를 생성하여 Sold 상태로 설정
                var newStatusData = cardItemModel.CardItemStatusData;
                newStatusData.State = CardItemState.Sold;
                cardItemModel.CardItemStatusData = newStatusData;
            }
        }
    }


    #endregion
}
