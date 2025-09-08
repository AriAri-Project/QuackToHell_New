using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;



/// <summary>
/// 책임: 게임 내 전체 카드 정보 관리
/// </summary>
public class DeckManager : NetworkBehaviour
{
    

    #region 싱글톤 코드

    private static DeckManager _instance;
    public static DeckManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindAnyObjectByType<DeckManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DeckManager");
                    _instance = go.AddComponent<DeckManager>();
                }
            }
            return _instance;
        }
        set
        {
            _instance = value;
        }

    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 데이터
    public Action OnAllCardsOnGameDataChanged;
    [Header("확인용 변수 열어두기")]
    [SerializeField]
    private NetworkList<CardItemData> allCardsOnGameData = new NetworkList<CardItemData>();
    public NetworkList<CardItemData> AllCardsOnGameData
    {
        get
        {
            return allCardsOnGameData;
        }
        set
        {
            allCardsOnGameData = value;
            //invoke하기
            OnAllCardsOnGameDataChanged?.Invoke();
        }
    }
    public async Task SetTotalCardsOnGame(Dictionary_CardIdCardDef[] cardDefKeyValuePairs)
    {
        CardItemData cardItemData = new CardItemData();
        foreach (var card in cardDefKeyValuePairs)
        {
            for (int i = 1; i <= card.Value.AmountOfCardItem; i++)
            {
                cardItemData = new CardItemData
                {
                    CardIdKey = card.Key,
                    CardDef = card.Value,
                    CardItemStatusData = new CardStatusData
                    {
                        CardID = card.Key,
                        CardItemID = card.Key + i,
                        Price = card.Value.BasePrice,
                        Cost = card.Value.BaseCost,
                        State = CardItemState.None
                    }
                };

                allCardsOnGameData.Add(cardItemData);
            }
        }
        await Task.CompletedTask;
    }
    public CardItemData? GetPurchaseableCardItemDataByCardIdKey(int cardIdKey)
    {
        if (allCardsOnGameData.Count == 0)
        {
            Debug.LogError("[DeckManager] GetCardItemDataByCardIdKey: 게임 내 카드 데이터가 없습니다.");
            return null;
        }

        // 카드가 sold, solding 상태가 아니면서, cardIdKey와 같은 카드를 찾아서 반환
        foreach (var card in allCardsOnGameData)
        {
            if(card.CardIdKey != cardIdKey)
            {
                continue;
            }
            if (card.CardItemStatusData.State != CardItemState.Sold && card.CardItemStatusData.State != CardItemState.Solding)
            {
                return card;
            }
        }

        // 반환할 카드가 없을 경우 null 반환
        return null;
    }
    public bool IsValidCardIdKey(int cardIdKey)
    {
        foreach (var card in allCardsOnGameData)
        {
            if (card.CardIdKey == cardIdKey)
            {
                return true;
            }
        }
        return false;
    }
    public bool IsValidCardItemIdKey(int cardItemIdKey)
    {
        foreach (var card in allCardsOnGameData)
        {
            if (card.CardItemStatusData.CardItemID == cardItemIdKey)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region 카드 구매 처리

    /// <summary>
    /// 살 수 있는지 검증하는 함수
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TryPurchaseCardServerRpc(CardItemData card, ulong clientId)
    {
        CardShopPresenter cardShopPresenter;
        cardShopPresenter = GameObject.FindAnyObjectByType<CardShopPresenter>();

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        // 해당 카드가 존재하는지 확인
        int cardItemIdKey = card.CardItemStatusData.CardItemID;
        if (!IsValidCardItemIdKey(cardItemIdKey))
        {
            Debug.Log("[DeckManager] TryPurchaseCardServerRpc: 유효하지 않은 카드 ID 키입니다.");
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
        }

        // 플레이어 골드 확인
        int playerGold = PlayerHelperManager.Instance.GetPlayerGoldByClientId(clientId);
        if (playerGold < card.CardItemStatusData.Price)
        {
            //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
            cardShopPresenter.PurchaseCardResultClientRpc(false, clientRpcParams);
            //구매 실패 여부를 클라이언트에게 전달. (ClientRPC, CardItemData값 보내기)
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
            return;
        }


        //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
        cardShopPresenter.PurchaseCardResultClientRpc(true, clientRpcParams);
        //구매 성공 여부를 클라이언트에게 전달. (ClientRPC, CardItemData값 보내기)
        PurchaseCardResultClientRpc(true, card, clientId, clientRpcParams);

    }

    [ClientRpc]
    private void PurchaseCardResultClientRpc(bool success, CardItemData card, ulong clientId, ClientRpcParams sendParams = default)
    {
        if (!success)
        {
            Debug.Log("[DeckManager] 카드 구매 실패");
            return;
        }

        // GameManager에게 해당 클라이언트의 골드 차감 요청 (책임 분리)
        GameManager.Instance.DeductPlayerGoldServerRpc(clientId, card.CardItemStatusData.Price);

        #region 카드인벤에 카드 추가
        // 카드 상태 주입 (Sold) - 기존 cardItemId 그대로 사용
        CardItemData tempCardData = card;
        tempCardData.CardItemStatusData.State = CardItemState.Sold;
        //카드 획득 시간 주입
        tempCardData.AcquiredTicks = DateTime.Now.Ticks;
        // 해당 플레이어 인벤토리에 카드 추가
        PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(clientId)?.GetComponent<CardInventoryModel>().AddOwnedCardServerRpc(tempCardData);

        #endregion
    }



    [ServerRpc]
    public void RequestUpdateAllCardsOnGameDataServerRpc(CardItemData cardItemData)
    {
        int cardItemId = cardItemData.CardItemStatusData.CardItemID;
        //전체 카드 목록에서 해당 카드 아이템 아이디를 가진 카드의 인덱스 찾기
        int index = -1;
        for (int i = 0; i < allCardsOnGameData.Count; i++)
        {
            if (allCardsOnGameData[i].CardItemStatusData.CardItemID == cardItemId)
            {
                index = i;
                break;
            }
        }
        if (index != -1)
        {
            // NetworkList의 특정 인덱스 업데이트
            allCardsOnGameData[index] = cardItemData;
            Debug.Log($"[DeckManager] 카드 상태 업데이트 완료: CardItemID {cardItemId}, State: {cardItemData.CardItemStatusData.State}");
        }
        else
        {
            Debug.LogError($"[DeckManager] RequestUpdateAllCardsOnGameDataServerRpc: CardItemID {cardItemId}를 가진 카드를 찾을 수 없습니다.");
        }
    }

    #endregion
}
