using Unity.Netcode;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public interface ICardShopModel
{
    void RequestPurchase(CardItemData card, ulong clientId);

    bool IsLocked { get; set; }
    bool TryReRoll();
    void DisplayCardsForSale();
}

public sealed class CardShopModel 
{
    public bool IsLocked { get; set; }

    private enum Rarity { Common, Rare }

    private GameObject cardForSaleParent;
    private Transform rowObjectTransform;
    const int maximumDisplayCount = 5;
    private int totalCardCountOnMap = 0;

    public void Initiate(GameObject CardForSaleParent, Transform rowObjectTrs)
    {
        cardForSaleParent = CardForSaleParent;
        rowObjectTransform = rowObjectTrs;

        // 판매할 카드 만들어두기
        CardItemFactory.Instance.CreateTotalCardForSale();        
        
    }

    public void RequestPurchase(CardItemData card, ulong clientId)
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

    private bool IsDuplicateRestrictedAndOwned(CardItemData card, ulong clientId, out string msg)
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

    
    public void DisplayCardForSale()
    {
        //cardForSaleParent의 자식 오브젝트 중에서, 5개의 랜덤한 오브젝트를 찾아서 
        
        //[TODO] sold가 아닌 애들을 후보군으로
        //[TODO] 모두 sold 상태면, 물량 없다고 에러찍고 진열 포기

        for (int i = 0; i < totalCardCountOnMap; i++) {
            //TODO: 후보군중에서 randomIndex뽑기
            //임시로 전체에서 뽑음
            int randomIndex = Random.Range(0, totalCardCountOnMap);
            //해당 인덱스의 오브젝트를 Row밑으로 옮기고 활성화
            Transform pickedCard = cardForSaleParent.transform.GetChild(randomIndex);
            pickedCard.SetParent(rowObjectTransform, false);
            pickedCard.gameObject.SetActive(true);
        }
        
    }
    #endregion
}
