using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using System.Collections.Generic;
using System.Linq;

public interface ICardShopModel
{
    void RequestPurchase(InventoryCard card, ulong clientId);

    bool IsLocked { get; set; }
    bool TryReRoll();
    void DisplayCardsForSale();
}

public sealed class CardShopModel 
{
    public bool IsLocked { get; set; }

    private enum Rarity { Common, Rare }

    public void RequestPurchase(InventoryCard card, ulong clientId)
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("[CardShopModel] DeckManager.Instance is null");
            return;
        }
        Debug.Log("[CardShopModel] RequestPurchase 실행됨");
        DeckManager.Instance.TryPurchaseCardServerRpc(card, clientId);

       /* if (IsInventoryFull(clientId, out var invMsg))
        {
            Debug.LogWarning($"[Shop] 구매 실패(인벤토리 초과): {invMsg}");
            CardShopPresenter.ServerSendResultTo(clientId, false);
            return;
        }

        if (IsDuplicateRestrictedAndOwned(card, clientId, out var dupMsg))
        {
            Debug.LogWarning($"[Shop] 구매 실패(중복 제한): {dupMsg}");
            CardShopPresenter.ServerSendResultTo(clientId, false);
            return;
        }*/
    }

   /* private bool IsInventoryFull(ulong clientId, out string msg)
    {
        // TODO: 실제 인벤토리 조회로 교체
        msg = string.Empty;
        return false; 
    }

    private bool IsDuplicateRestrictedAndOwned(InventoryCard card, ulong clientId, out string msg)
    {
        // TODO: 카드 메타데이터에 중복 제한 플래그가 있다면 확인
        msg = string.Empty;
        return false; 
    }*/

    #region 카드 생성 확률 로직
    /*private const float BaseCommon = 0.8f;
    private const float BaseRare = 0.2f;
    private const float Delta = 0.3f; // 30%

    // TODO: 실제 카드DB와 연결되면 이 풀은 DB에서 가져오기.
    private static readonly int[] _fallbackCommonPool = { 10000, 20000, 30000 };
    private static readonly int[] _fallbackRarePool = { 10100, 20200 };

    private readonly System.Random _rng = new System.Random();

    // TODO: 실제 전체 인원/사망 인원은 Game/Match 매니저에서 받아오기.
    // 지금은 샘플로 0명 사망 가정(=초기 확률 유지).
    private float GetDeathRatio()
    {
        return 0f;
    }

    private (float common, float rare) ComputeRarityWeights()
    {
        var r = Mathf.Clamp01(GetDeathRatio()); // 0~1
        var rare = Mathf.Clamp(BaseRare + Delta * r, 0.5f, 0.5f);     // 0.2→최대 0.5
        var common = Mathf.Clamp(BaseCommon - Delta * r, 0.5f, 0.5f); // 0.8→최소 0.5
        return (common, rare);
    }

    private Rarity RollRarity((float common, float rare) w)
    {
        var t = _rng.NextDouble();
        return (t < w.rare) ? Rarity.Rare : Rarity.Common;
    }

    private int GetRandomCardIdByRarity(Rarity rarity)
    {
        // TODO: CardDB.GetRandomId(rarity)로 교체
        if (rarity == Rarity.Rare)
            return _fallbackRarePool[_rng.Next(_fallbackRarePool.Length)];
        return _fallbackCommonPool[_rng.Next(_fallbackCommonPool.Length)];
    }

    private List<int> RollShopCardIds(int count)
    {
        var w = ComputeRarityWeights();
        var ids = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            var r = RollRarity(w);
            ids.Add(GetRandomCardIdByRarity(r));
        }
        return ids;
    }*/
    #endregion

    #region 카드 목록 새로고침
    public bool TryReRoll()
    {
        if (IsLocked) return false;

        // 새로 뿌리기
        DisplayCardForSale();

        return true;
    }

    private Transform _row;
    private Transform GetRow()
    {
        Debug.Log($"[CardShopModel] GetRow 실행됨");
        if (_row == null)
        {
            GameObject rowParent = GameObject.FindWithTag("CardShop");
            if (rowParent != null)
            {
                // 모든 자손에서 CardShopRow 찾기 (비활성화된 것도 포함)
                Transform[] allChildren = rowParent.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    if (child.name == "CardShopRow")
                    {
                        _row = child;
                        break;
                    }
                }
            }
        }
        return _row;
    }
    public void DisplayCardForSale()
    {
        Debug.Log($"[CardShopModel] 카드 진열함수 들어옴");
        if (IsLocked)
        {
            Debug.Log("[CardShopModel] Locked: skip DisplayCardsForSale");
            return;
        }

        var row = GetRow();
        if (row == null) return;

        // 기존 카드 다 지우기
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            GameObject cardForSale = row.GetChild(i).gameObject;
            CardItemModel cardModel = cardForSale.transform.GetComponentInChildren<CardItemModel>();
      
            if (cardModel == null)
            {
                //팔린애에 대해서는 처리x
                continue;
            }

            //물량 재공급
            int cardId = cardModel.CardItemStatusData.CardID;
            DeckManager.Instance.IncreaseCardTotalCount(cardId);

            //파괴
            Object.Destroy(row.GetChild(i).gameObject);
        }

        Vector3 center = row.position;
        if (row is RectTransform rt) center = rt.TransformPoint(rt.rect.center);

        const int count = 5;
        float cardWidth = 200f;   // UI에 맞게 조정
        float spacing = 50f;
        float startX = -(count - 1) * (cardWidth + spacing) * 0.5f;

        // 5개 생성
        int[] pool = { 10000, 20000, 30000, 10100, 20200, 30200 };
        var rng = new System.Random();

        for (int i = 0; i < count; i++)
        {
            int pick = pool[rng.Next(pool.Length)];
            var pos = new Vector3(center.x + startX + i * (cardWidth + spacing), center.y, center.z);
            Debug.Log($"[CardShopModel] 카드 생성 id={pick}");
            // 물량 체크 및 대체 카드 선택
            if (DeckManager.Instance.GetCardTotalCount(pick) <= 0)
            {
                var availableCards = pool.Where(id => DeckManager.Instance.GetCardTotalCount(id) > 0).ToArray();
                if (availableCards.Length == 0)
                {
                    Debug.Log("[CardShopModel] 모든 카드 물량이 없습니다.");
                    continue;
                }
                pick = availableCards[rng.Next(availableCards.Length)];
            }
            CardItemFactory.Instance.CreateCardForSale(pick, Vector3.zero);
        }

        MoveCardsForSaleToRowObject();
    }
    /// <summary>
    /// 카드들을 Row 오브젝트 밑으로 이동시킵니다.
    /// </summary>
    public void MoveCardsForSaleToRowObject()
    {
        var row = GetRow();
        if (row == null) return;

        // 스폰된 카드들을 전부 Row 밑으로 이동
        var spawned = GameObject.FindGameObjectsWithTag("CardForSale");
        foreach (var go in spawned)
        {
            Debug.Log($"[CardShopModel] 카드 진열함수: 카드의 부모오브젝트 세팅");
            go.transform.SetParent(row, false);
        }
    }
    #endregion
}
