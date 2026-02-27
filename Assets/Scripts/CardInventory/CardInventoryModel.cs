using Court;
using Court.Hand;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


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
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ★ [핵심] "이 캐릭터가 내 캐릭터(IsOwner)라면?"
        if (IsOwner)
        {
            base.OnNetworkSpawn();
            // 씬 로드 이벤트 등록 (씬이 바뀔 때마다 감시)
            if (IsOwner)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                // 혹시 이미 재판장에 들어와 있는 상태에서 스폰되었을 경우를 대비
                CheckAndConnectUI(SceneManager.GetActiveScene().name);
            }
        }
    }
    
    public override void OnNetworkDespawn()

    {
        base.OnNetworkDespawn();
        // 이벤트 해제 (메모리 누수 방지)
        if (IsOwner)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    /// <summary>
    /// 씬 로딩이 끝날 때마다 호출됨
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndConnectUI(scene.name);
    }
    
    /// <summary>
    /// 재판장인지 확인하고 UI 연결
    /// </summary>
    private void CheckAndConnectUI(string sceneName)
    {
        // 1. 내 캐릭터가 아니면 무시
        if (!IsOwner) return;
        
        // 2. 현재 씬이 "재판장(Court)"이 아니면 무시 (아무때나 찾지 않음!)
        // GameScenes.Court 상수를 사용하거나 문자열 "Court" 사용
        if (sceneName != GameScenes.Court) 
        {
            return;
        }
        
        // 3. 재판장이 맞다면 UI 찾기 시도
        var handPresenter = FindAnyObjectByType<TrialHandPresenter>();
        
        if (handPresenter != null)
        {
            Debug.Log($"[CardInventory] 재판장 도착! 손패 UI 연결 시도...");
            handPresenter.SetInventory(this);
        }
        else
        {
            // 재판장인데 UI가 없다면 에러 (배치 실수)
            Debug.LogError("[CardInventory] 재판장(Court)인데 TrialHandPresenter가 없습니다!");
        }
    }

    
    #endregion
    
    #region InventoryCard 데이터 추가, 삭제 메서드
    [ServerRpc(RequireOwnership = false)]
    public void AddOwnedCardServerRpc(CardItemData card)
    {  
        if (ownedCards.Count >= GameConstants.Card.maxCardCount)
        {
            return;
        }
        ownedCards.Add(card);
    }

    [ServerRpc(RequireOwnership = false)]
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

    public bool HasAllRevoltCards()
    {
        return HasCard(60100) && HasCard(60200) && HasCard(60300);
    }

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

    /// <summary>
    /// 카드 아이템 ID 기준 소유 여부 확인 (판매용)
    /// </summary>
    public bool HasOwnedCard(int cardItemId)
    {
        if (ownedCards == null) return false;

        foreach (CardItemData card in ownedCards)
        {
            if (card.cardItemStatusData.cardItemID == cardItemId)
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
    
    
    #region 증거물 제출 로직 

    [ServerRpc]
    public void SubmitEvidenceServerRpc(int handIndex1, int handIndex2, ulong targetClientId)
    {
        // 1. 유효성 검사
        if (handIndex1 < 0 || handIndex1 >= ownedCards.Count ||
            handIndex2 < 0 || handIndex2 >= ownedCards.Count || handIndex1 == handIndex2) return;

        CardItemData card1 = ownedCards[handIndex1];
        CardItemData card2 = ownedCards[handIndex2];

        // 2. 호환성 검사 (Rules 사용)
        if (!CourtGameRules.IsCompatible(card1, card2)) return;

        // 3. 타겟 정보 가져오기
        int currentVote = 0;
        int targetIndex = VoteModel.Instance.GetPlayerIndex(targetClientId);
        if (targetIndex != -1) currentVote = VoteModel.Instance.GetVoteCount(targetIndex);
        else return;

        // ★ 4. Rules에게 계산 위임! (여기가 핵심 변경점)
        // CardInventoryModel은 이제 수학을 몰라도 됩니다.
        int finalDelta = CourtGameRules.CalculateFinalScore(card1, card2, currentVote);
        bool allowZero = CourtGameRules.IsMultiplyByZeroCombo(card1, card2);
        
        Debug.Log($"[Server] 카드 사용: Target({targetClientId}), 변동량({finalDelta})");

        // 5. 결과 적용
        if (VoteModel.Instance != null && finalDelta != 0)
        {
            VoteModel.Instance.AddVote(targetClientId, finalDelta, allowZero);
        }

        // 6. 카드 소모
        if (handIndex1 > handIndex2) { ownedCards.RemoveAt(handIndex1); ownedCards.RemoveAt(handIndex2); }
        else { ownedCards.RemoveAt(handIndex2); ownedCards.RemoveAt(handIndex1); }
    }

    #endregion
    
    //초기화 함수
    public void Initialize()
    {
        ownedCards.Clear();
    }
}
