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
    
    
    #region 증거물 제출 로직 (신규 구현)

    /// <summary>
    /// 클라이언트가 선택한 카드 2장을 제출하고 타겟에게 데미지를 줌
    /// </summary>
    [ServerRpc]
    public void SubmitEvidenceServerRpc(int handIndex1, int handIndex2, ulong targetClientId)
    {
        // 1. 인덱스 유효성 검사
        if (handIndex1 < 0 || handIndex1 >= OwnedCards.Count ||
            handIndex2 < 0 || handIndex2 >= OwnedCards.Count ||
            handIndex1 == handIndex2)
        {
            Debug.LogError($"[Inventory] 잘못된 카드 인덱스 요청: {handIndex1}, {handIndex2}");
            return;
        }

        // 2. 카드 데이터 가져오기
        // (주의: RemoveAt을 하면 인덱스가 밀리므로 데이터를 먼저 확보)
        CardItemData card1 = OwnedCards[handIndex1];
        CardItemData card2 = OwnedCards[handIndex2];

        // 3. 호환성 검사 (서버 보안 검증)
        if (!CourtGameRules.IsCompatible(card1, card2))
        {
            Debug.LogWarning("[Inventory] 호환되지 않는 카드 조합입니다.");
            return;
        }

        // 4. 점수(데미지) 계산
        int damage = CourtGameRules.CalculateEvidenceScore(card1, card2);

        // 5. VoteModel을 통해 타겟에게 점수 반영
        if (VoteModel.Instance != null)
        {
            VoteModel.Instance.AddVote(targetClientId, damage);
        }
        else
        {
            Debug.LogError("[Inventory] VoteModel 인스턴스를 찾을 수 없습니다!");
            return; // 모델 없으면 카드 소모 안 함
        }

        // 6. 사용한 카드 인벤토리에서 제거
        // (인덱스가 큰 것부터 지워야 앞쪽 인덱스가 변하지 않음)
        if (handIndex1 > handIndex2)
        {
            OwnedCards.RemoveAt(handIndex1);
            OwnedCards.RemoveAt(handIndex2);
        }
        else
        {
            OwnedCards.RemoveAt(handIndex2);
            OwnedCards.RemoveAt(handIndex1);
        }
        
        Debug.Log($"[Server] 카드 제출 완료! 타겟:{targetClientId}, 데미지:{damage}");
    }

    #endregion
}
