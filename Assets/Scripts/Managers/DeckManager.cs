using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using System.Threading;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using CardItem.MVP;
using Unity.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;




#region Data Structs (기획서 타입 반영)
public enum TierEnum { None = 0, Common = 1, Rare = 2, Special = 3 }
public enum TypeEnum { None = 0, Attack = 1, Defense = 2, Special = 3, Number = 4, Operator = 5, Roll = 6}
public enum SubTypeEnum
{ None = 0, Const = 1, N = 2, Revolt = 3, Farmer = 4, Animal = 5 }

public enum CardValue
{
    Unknown = -1,
    V0, V1, V2, V3, V4, V5, V6, // 숫자 0~6
    N,                          // 'N'
    ADD, SUB, MULT, DIV         // 연산 기호
}

// 딕셔너리의 Key와 Value 한 쌍을 담을 컨테이너 struct
public struct DictionaryCardIdCardDef : INetworkSerializable, IEquatable<DictionaryCardIdCardDef>
{
    
    
    public int key;
    public CardDef value;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref key);
        serializer.SerializeValue(ref value);
    }

    public bool Equals(DictionaryCardIdCardDef other)
    {
        return key == other.key && value.Equals(other.value);
    }

    public override bool Equals(object obj)
    {
        return obj is DictionaryCardIdCardDef pair && Equals(pair);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(key, value);
    }
}

/// <summary>
/// 카드 아이템 데이터 (CardDef + CardItemStatusData)
/// </summary>
public struct CardItemData : INetworkSerializable, IEquatable<CardItemData>, IEquatable<CardDef>, IEquatable<CardStatusData>
{
    public int cardIdKey;
    public CardDef cardDef;
    public CardStatusData cardItemStatusData;
    public long acquiredTicks; // 카드 획득 시점
    public ulong displayingClientId; // 진열 중인 클라이언트 ID (9999이면 진열되지 않음 상태) 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardIdKey);
        serializer.SerializeValue(ref cardDef);
        serializer.SerializeValue(ref cardItemStatusData);
        serializer.SerializeValue(ref acquiredTicks);
        serializer.SerializeValue(ref displayingClientId);
    }

    public bool Equals(CardItemData other)
    {
        return cardIdKey == other.cardIdKey && 
               cardDef.Equals(other.cardDef) && 
               cardItemStatusData.Equals(other.cardItemStatusData) && 
               acquiredTicks == other.acquiredTicks &&
               displayingClientId == other.displayingClientId;
    }

    public bool Equals(CardDef other)
    {
        return cardDef.Equals(other);
    }

    public bool Equals(CardStatusData other)
    {
        return cardItemStatusData.Equals(other);
    }

    public override bool Equals(object obj)
    {
        if (obj is CardItemData cardItemData)
            return Equals(cardItemData);
        if (obj is CardDef cardDef)
            return Equals(cardDef);
        if (obj is CardStatusData statusData)
            return Equals(statusData);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(cardIdKey, cardDef, cardItemStatusData, acquiredTicks, displayingClientId);
    }
}

/// <summary>
/// Inspector에서 카드 상태를 쉽게 보기 위한 요약 구조체
/// </summary>
[System.Serializable]
public struct CardStatusSummary
{
    [Header("🆔 카드 정보")]
    public int cardIdKey;
    public int cardItemId;
    public string cardName;
    
    [Header("📊 상태 정보")]
    public CardItemState state;
    public ulong displayingClientId;
    public string acquiredTime;
    
    [Header("💰 가격 정보")]
    public int price;
    public int cost;
    
    [Header("🎯 기타")]
    public TierEnum tier;
    public TypeEnum type;
    public string statusDescription;
}

public struct CardDef : INetworkSerializable, IEquatable<CardDef>
{
    public int cardID;
    public FixedString64Bytes cardName;
    public TierEnum tier;      // enum
    public TypeEnum type;      // enum
    public SubTypeEnum subType;        // 사용 안 하면 0
    public CardValue Value;
    public bool isUniqueCard;
    public bool isSellableCard;
    public int buyableClass;
    public int usableClass;      // 3bit
    public int mapRestriction;  // 2bit
    public int basePrice;
    // public int baseCost;
    public FixedString64Bytes description;
    public FixedString64Bytes cardIImagePath;
    public int amountOfCardItem;
    public FixedString128Bytes cardIconResourcePath;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardID);
        serializer.SerializeValue(ref cardName);
        serializer.SerializeValue(ref tier);
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref subType);
        serializer.SerializeValue(ref Value);
        serializer.SerializeValue(ref isUniqueCard);
        serializer.SerializeValue(ref isSellableCard);
        serializer.SerializeValue(ref buyableClass);
        serializer.SerializeValue(ref usableClass);
        serializer.SerializeValue(ref mapRestriction);
        serializer.SerializeValue(ref basePrice);
        // serializer.SerializeValue(ref baseCost);
        serializer.SerializeValue(ref description);
        serializer.SerializeValue(ref cardIImagePath);
        serializer.SerializeValue(ref amountOfCardItem);
        serializer.SerializeValue(ref cardIconResourcePath);
    }

    public bool Equals(CardDef other)
    {
        return cardID == other.cardID && 
               cardName.Equals(other.cardName) && 
               tier == other.tier && 
               type == other.type && 
               subType == other.subType && 
               Value == other.Value &&
               isUniqueCard == other.isUniqueCard && 
               isSellableCard == other.isSellableCard &&
               buyableClass == other.buyableClass &&
               usableClass == other.usableClass && 
               mapRestriction == other.mapRestriction && 
               basePrice == other.basePrice && 
               // baseCost == other.baseCost && 
               description.Equals(other.description) &&
               cardIImagePath.Equals(other.cardIImagePath) && 
               amountOfCardItem == other.amountOfCardItem &&
               cardIconResourcePath.Equals(other.cardIconResourcePath);
    }

    public override bool Equals(object obj)
    {
        return obj is CardDef def && Equals(def);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(cardID);
        hash.Add(cardName);
        hash.Add(tier);
        hash.Add(type);
        hash.Add(subType);
        hash.Add(Value);
        hash.Add(isUniqueCard);
        hash.Add(isSellableCard);
        hash.Add(buyableClass);
        hash.Add(usableClass);
        hash.Add(mapRestriction);
        hash.Add(basePrice);
        // hash.Add(baseCost);
        hash.Add(description);
        hash.Add(cardIImagePath);
        hash.Add(amountOfCardItem);
        hash.Add(cardIconResourcePath);
        return hash.ToHashCode();
    }
}

public enum CardItemState
{
    None,
    Solding,
    Sold,
}

public struct CardStatusData : INetworkSerializable, IEquatable<CardStatusData>
{
    public int cardItemID;
    public int cardID;
    public int price;
    public CardItemState state;
    
    // Property to access state (for compatibility with existing code)
    public CardItemState State => state;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardItemID);
        serializer.SerializeValue(ref cardID);
        serializer.SerializeValue(ref price);
        serializer.SerializeValue(ref state);
    }

    public bool Equals(CardStatusData other)
    {
        return cardItemID == other.cardItemID && cardID == other.cardID && price == other.price && state == other.state;
    }

    public override bool Equals(object obj)
    {
        return obj is CardStatusData data && Equals(data);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(cardItemID, cardID, price, state);
    }   
}

[Serializable] public struct StringRow { [FormerlySerializedAs("Key")] public string key; [FormerlySerializedAs("KR")] public string kr; [FormerlySerializedAs("EN")] public string en; }
[Serializable] public struct ResourceRow { [FormerlySerializedAs("Key")] public string key; [FormerlySerializedAs("Path")] public string path; }

[Serializable]
public struct CardDisplay
{
    [FormerlySerializedAs("CardID")] public int cardID;
    [FormerlySerializedAs("Name")] public string name;
    [FormerlySerializedAs("Description")] public string description;
    [FormerlySerializedAs("CardIImagePath")] public string cardIImagePath;
    [FormerlySerializedAs("CardIconResourcePath")] public string cardIconResourcePath;
    [FormerlySerializedAs("Tier")] public TierEnum tier;
    [FormerlySerializedAs("Type")] public TypeEnum type;
    [FormerlySerializedAs("BasePrice")] public int basePrice;
    // [FormerlySerializedAs("BaseCost")] public int baseCost;
}
#endregion

/// <summary>
/// 책임: 게임 내 전체 카드 정보 관리 (권위적 데이터 소스)
/// </summary>
public class DeckManager : NetworkBehaviour
{
    [Header("SFX")]
    [SerializeField] private AudioSource soldFailSFX;
    [SerializeField] private AudioSource solSucceedSFX;

    public enum DeckSFX{
        soldFailSFX,
        soldSucceedSFX
    }

    
    #region 싱글톤
    public static DeckManager Instance => SingletonHelper<DeckManager>.Instance;

    private void Awake()
    {
        SingletonHelper<DeckManager>.InitializeSingleton(this);
        
        // NetworkList 변경 이벤트 바인딩
        _allCardsOnGameData.OnListChanged += OnAllCardsOnGameDataChanged;
        //SceneLoad에 바인딩
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameScenes.Village)
        {
            GameObject CardShopCanvas =  GameObject.FindGameObjectWithTag(GameTags.UI_CardShopCanvas);
            GameObject CardShopPanel = CardShopCanvas.transform.GetChild(0).gameObject;
            cardShopPresenter = CardShopPanel.GetComponent<CardShopPresenter>();
        }
    }

    public override void OnDestroy()
    {
        // 이벤트 언바인딩
        if (_allCardsOnGameData != null)
        {
            _allCardsOnGameData.OnListChanged -= OnAllCardsOnGameDataChanged;
        }
    }

    private void OnAllCardsOnGameDataChanged(NetworkListEvent<CardItemData> changeEvent)
    {
        // 서버에서만 디버그 정보 업데이트
        if (IsServer)
        {
            UpdateCardCounts();
        }
    }

    private static bool TryGetHeaderValue(List<string> headers, List<string> cols, string header, out string value)
    {
        value = null;
        int idx = -1;
        for (int i = 0; i < headers.Count; i++)
        {
            if (headers[i].Trim().Equals(header, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }

        if (idx < 0 || idx >= cols.Count)
            return false;

        value = cols[idx]?.Trim() ?? "";
        return true;
    }

    private static int TryGetHeaderInt(List<string> headers, List<string> cols, string header, int @default = 0)
    {
        return TryGetHeaderValue(headers, cols, header, out var s) && int.TryParse(s, out var v) ? v : @default;
    }

    private static bool TryGetHeaderBool(List<string> headers, List<string> cols, string header, bool @default = false)
    {
        if (!TryGetHeaderValue(headers, cols, header, out var s))
            return @default;

        s = (s ?? "").Trim().ToLowerInvariant();
        return s is "1" or "true" or "y" or "yes";
    }

    #endregion

    #region 데이터
    public Action onAllCardsOnGameDataChanged;
    [Header("확인용 변수 열어두기")]
    [SerializeField]
    private NetworkList<CardItemData> _allCardsOnGameData = new NetworkList<CardItemData>();
    
    [Header("디버그용 - 카드 상태별 개수 (서버 권위적)")]
    [SerializeField] private int _totalCardCount;
    [SerializeField] private int _soldCardCount;
    [SerializeField] private int _soldingCardCount;
    [SerializeField] private int _noneCardCount;
    
    [Header("디버그용 - 클라이언트별 진열 상태")]
    [SerializeField] private Dictionary<ulong, int> _clientDisplayCounts = new Dictionary<ulong, int>();
    
    [Header("디버그용 - 개별 카드 상태 (Inspector용)")]
    [SerializeField] private List<CardItemData> _debugCardList = new List<CardItemData>();
    
    [Header("References")]
    private CardShopPresenter cardShopPresenter;
    
    [Header("📋 모든 카드 상태 요약 (펼쳐서 보기)")]
    [Tooltip("모든 카드의 상태를 한 눈에 볼 수 있습니다. 리스트를 펼쳐서 각 카드의 상세 정보를 확인하세요.")]
    [SerializeField] private List<CardStatusSummary> _cardStatusSummaries = new List<CardStatusSummary>();
    
    
    public NetworkList<CardItemData> AllCardsOnGameData
    {
        get
        {
            return _allCardsOnGameData;
        }
        set
        {
            _allCardsOnGameData = value;
            //invoke하기
            onAllCardsOnGameDataChanged?.Invoke();
        }
    }

    // CSV 파싱을 위한 내부 데이터 저장소 (CardDataPresenter에서 이동)
    private Dictionary<int, CardDef> _cardDefinitions = new();
    private Dictionary<string, StringRow> _strings = new();
    private Dictionary<string, ResourceRow> _resources = new();
    private Task _dataLoadTask;
    private CancellationTokenSource _cancellationTokenSource;

    // Public APIs for clients (CardDataPresenter에서 이동)
    public bool TryGetCardDefinition(int cardId, out CardDef def) => _cardDefinitions.TryGetValue(cardId, out def);
    
    public bool TryGetCardDisplay(int cardId, string locale, out CardDisplay disp)
    {
        disp = default;
        if (!_cardDefinitions.TryGetValue(cardId, out CardDef cardDefinition)) return false;
        disp = new CardDisplay
        {
            cardID = cardDefinition.cardID,
            name = Localize(cardDefinition.cardName.ToString(), locale),
            description = Localize(cardDefinition.description.ToString(), locale),
            cardIImagePath = ResolvePath(cardDefinition.cardIImagePath.ToString()),
            cardIconResourcePath = cardDefinition.cardIconResourcePath.ToString(),
            tier = cardDefinition.tier,
            type = cardDefinition.type,
            basePrice = cardDefinition.basePrice,
            // baseCost = cardDefinition.baseCost
        };
        return true;
    }

    public IReadOnlyDictionary<int, CardDef> CardDefinitions => _cardDefinitions;
    public int CardDefinitionCount => _cardDefinitions.Count;

    // 로컬라이제이션 및 리소스 해결 (CardDataModel에서 이동)
    private string Localize(string key, string locale)
    {
        if (!_strings.TryGetValue(key, out StringRow stringRow)) return key;
        return locale switch { "ko" => stringRow.kr ?? key, "en" => stringRow.en ?? key, _ => stringRow.kr ?? stringRow.en ?? key };
    }
    
    private string ResolvePath(string key) => _resources.TryGetValue(key, out ResourceRow resourceRow) ? resourceRow.path : key;

    /// <summary>
    /// 카드 ID로 표시 이름을 가져오는 헬퍼 메서드
    /// </summary>
    private string GetCardDisplayName(int cardId)
    {
        if (TryGetCardDisplay(cardId, "ko", out CardDisplay display))
        {
            return display.name;
        }
        return $"Card_{cardId}";
    }

    /// <summary>
    /// 카드 상태에 대한 설명을 생성하는 헬퍼 메서드
    /// </summary>
    private string GetStatusDescription(CardItemState state, ulong displayingClientId)
    {
        return state switch
        {
            CardItemState.None => "🟢 사용 가능",
            CardItemState.Solding => $"🟡 진열 중 (클라이언트 {displayingClientId})",
            CardItemState.Sold => "🔴 판매됨",
            _ => "❓ 알 수 없음"
        };
    }

    /// <summary>
    /// 디버그용 카드 상태별 개수 업데이트 (서버에서만)
    /// </summary>
    private void UpdateCardCounts()
    {
        if (!IsServer) return;

        _totalCardCount = _allCardsOnGameData.Count;
        _soldCardCount = 0;
        _soldingCardCount = 0;
        _noneCardCount = 0;

        // 디버그용 리스트도 업데이트
        _debugCardList.Clear();
        _cardStatusSummaries.Clear();
        
        foreach (CardItemData card in _allCardsOnGameData)
        {
            _debugCardList.Add(card);
            
            // 카드 상태 요약 생성
            CardStatusSummary summary = new CardStatusSummary
            {
                cardIdKey = card.cardIdKey,
                cardItemId = card.cardItemStatusData.cardItemID,
                cardName = GetCardDisplayName(card.cardDef.cardID),
                state = card.cardItemStatusData.state,
                displayingClientId = card.displayingClientId,
                acquiredTime = card.acquiredTicks > 0 ? new DateTime(card.acquiredTicks).ToString("HH:mm:ss") : "N/A",
                price = card.cardItemStatusData.price,
                tier = card.cardDef.tier,
                type = card.cardDef.type,
                statusDescription = GetStatusDescription(card.cardItemStatusData.state, card.displayingClientId)
            };
            _cardStatusSummaries.Add(summary);
            
            switch (card.cardItemStatusData.state)
            {
                case CardItemState.Sold:
                    _soldCardCount++;
                    break;
                case CardItemState.Solding:
                    _soldingCardCount++;
                    break;
                case CardItemState.None:
                    _noneCardCount++;
                    break;
            }
        }

        // 모든 클라이언트에게 디버그 정보 동기화
        SyncDebugInfoToAllClientsClientRpc(_totalCardCount, _soldCardCount, _soldingCardCount, _noneCardCount);
    }

    /// <summary>
    /// 모든 클라이언트에게 디버그 정보 동기화
    /// </summary>
    [ClientRpc]
    private void SyncDebugInfoToAllClientsClientRpc(int totalCount, int soldCount, int soldingCount, int noneCount)
    {
        _totalCardCount = totalCount;
        _soldCardCount = soldCount;
        _soldingCardCount = soldingCount;
        _noneCardCount = noneCount;
    }

    // CSV 데이터 로딩 (CardDataPresenter에서 이동)
    public Task LoadCardDataFromCsv(string cardCsvUrl, string stringCsvUrl, string resourceCsvUrl, CancellationToken ct = default)
    {
        if (_dataLoadTask != null) return _dataLoadTask;
        _dataLoadTask = LoadCardDataImplAsync(cardCsvUrl, stringCsvUrl, resourceCsvUrl, ct);
        return _dataLoadTask;
    }

    public Task WhenDataReadyAsync() => _dataLoadTask ?? Task.CompletedTask;

    private async Task LoadCardDataImplAsync(string cardUrl, string strUrl, string resUrl, CancellationToken ct)
    {
        // 세 시트를 병렬 다운로드
        Task<string> cardTask = GetTextAsync(cardUrl, ct);
        Task<string> stringTask = GetTextAsync(strUrl, ct);
        Task<string> resourceTask = GetTextAsync(resUrl, ct);

        string cardCsv = await cardTask; 
        string stringCsv = await stringTask; 
        string resourceCsv = await resourceTask;

        // 데이터 파싱 및 로드
        LoadCardDefinitions(ParseCardTable(cardCsv));
        LoadStrings(ParseStringTable(stringCsv));
        LoadResources(ParseResourceTable(resourceCsv));

        // 게임 내 카드 데이터 생성
        await SetTotalCardsOnGame(_cardDefinitions.Select(kvp => new DictionaryCardIdCardDef { key = kvp.Key, value = kvp.Value }).ToArray());

    }

    private void LoadCardDefinitions(IEnumerable<CardDef> rows) 
    { 
        _cardDefinitions.Clear(); 
        foreach (CardDef cardDefinition in rows) _cardDefinitions[cardDefinition.cardID] = cardDefinition; 
    }
    
    private void LoadStrings(IEnumerable<StringRow> rows) 
    { 
        _strings.Clear(); 
        foreach (StringRow stringRow in rows) if (!string.IsNullOrEmpty(stringRow.key)) _strings[stringRow.key] = stringRow; 
    }
    
    private void LoadResources(IEnumerable<ResourceRow> resourceRows) 
    { 
        _resources.Clear(); 
        foreach (ResourceRow resourceRow in resourceRows) if (!string.IsNullOrEmpty(resourceRow.key)) _resources[resourceRow.key] = resourceRow; 
    }

    public async Task SetTotalCardsOnGame(DictionaryCardIdCardDef[] cardDefKeyValuePairs)
    {
        foreach (DictionaryCardIdCardDef card in cardDefKeyValuePairs)
        {
            for (int i = 1; i <= card.value.amountOfCardItem; i++)
            {
                CardItemData cardItemData = new CardItemData
                {
                    cardIdKey = card.key,
                    cardDef = card.value,
                    displayingClientId = GameConstants.Card.NOT_DISPLAYING_CLIENT_ID,
                    cardItemStatusData = new CardStatusData
                    {
                        cardID = card.key,
                        cardItemID = card.key + i,
                        price = card.value.basePrice,
                        // cost = card.value.baseCost,
                        state = CardItemState.None
                    }
                };

                _allCardsOnGameData.Add(cardItemData);
            }
        }
        await Task.CompletedTask;
    }
    public CardItemData? GetPurchaseableCardItemDataByCardIdKey(int cardIdKey)
    {
        if (_allCardsOnGameData.Count == 0)
        {
            Debug.LogError("[DeckManager] GetCardItemDataByCardIdKey: 게임 내 카드 데이터가 없습니다.");
            return null;
        }

        // 서버에서는 권위적 데이터 확인, 클라이언트에서는 읽기 전용 데이터 확인
        if (IsServer)
        {
            return GetAvailableCardForPurchase(cardIdKey);
        }
        else
        {
            return GetAvailableCardForPurchaseClient(cardIdKey);
        }
    }
    public bool IsValidCardIdKey(int cardIdKey)
    {
        foreach (var card in _allCardsOnGameData)
        {
            if (card.cardIdKey == cardIdKey)
            {
                return true;
            }
        }
        return false;
    }
    public bool IsValidCardItemIdKey(int cardItemIdKey)
    {
        foreach (CardItemData card in _allCardsOnGameData)
        {
            if (card.cardItemStatusData.cardItemID == cardItemIdKey)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 특정 카드 ID의 구매 가능한 물량이 있는지 확인
    /// </summary>
    public bool IsCardAvailableForPurchase(int cardIdKey)
    {
        // 서버에서만 권위적 데이터 확인
        if (!IsServer)
        {
            Debug.LogWarning("[DeckManager] IsCardAvailableForPurchase: 클라이언트에서 호출됨. 서버 권위성을 위해 서버 RPC를 사용하세요.");
            return false;
        }

        // 해당 카드 ID의 총 물량 확인
        int totalAmount = 0;
        int soldAmount = 0;
        
        foreach (CardItemData card in _allCardsOnGameData)
        {
            if (card.cardIdKey == cardIdKey)
            {
                totalAmount++;
                if (card.cardItemStatusData.state == CardItemState.Sold)
                {
                    soldAmount++;
                }
            }
        }
        
        // 구매 가능한 물량이 있는지 확인
        return soldAmount < totalAmount;
    }

    /// <summary>
    /// 특정 카드 ID의 구매 가능한 카드 아이템 데이터 반환
    /// </summary>
    public CardItemData? GetAvailableCardForPurchase(int cardIdKey)
    {
        // 서버에서만 권위적 데이터 확인
        if (!IsServer)
        {
            Debug.LogWarning("[DeckManager] GetAvailableCardForPurchase: 클라이언트에서 호출됨. 서버 권위성을 위해 서버 RPC를 사용하세요.");
            return null;
        }

        if (!IsCardAvailableForPurchase(cardIdKey))
        {
            return null;
        }

        // 구매 가능한 카드 찾기 (Sold, Solding 상태가 아닌 것)
        foreach (var card in _allCardsOnGameData)
        {
            if (card.cardIdKey == cardIdKey && 
                card.cardItemStatusData.state != CardItemState.Sold && 
                card.cardItemStatusData.state != CardItemState.Solding)
            {
                return card;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 클라이언트에서 안전하게 카드 데이터를 읽기 위한 메서드 (읽기 전용)
    /// </summary>
    public bool IsCardAvailableForPurchaseClient(int cardIdKey)
    {
        // 클라이언트에서는 로컬 데이터를 읽기만 함 (서버 동기화된 데이터)
        int totalAmount = 0;
        int soldAmount = 0;
        
        foreach (CardItemData card in _allCardsOnGameData)
        {
            if (card.cardIdKey == cardIdKey)
            {
                totalAmount++;
                if (card.cardItemStatusData.state == CardItemState.Sold)
                {
                    soldAmount++;
                }
            }
        }
        
        // 구매 가능한 물량이 있는지 확인 (읽기 전용)
        return soldAmount < totalAmount;
    }

    /// <summary>
    /// 클라이언트에서 안전하게 구매 가능한 카드 데이터를 읽기 위한 메서드 (읽기 전용)
    /// </summary>
    public CardItemData? GetAvailableCardForPurchaseClient(int cardIdKey)
    {
        if (!IsCardAvailableForPurchaseClient(cardIdKey))
        {
            return null;
        }

        // 구매 가능한 카드 찾기 (Sold, Solding 상태가 아닌 것)
        foreach (CardItemData card in _allCardsOnGameData)
        {
            if (card.cardIdKey == cardIdKey && 
                card.cardItemStatusData.state != CardItemState.Sold && 
                card.cardItemStatusData.state != CardItemState.Solding)
            {
                return card;
            }
        }
        
        return null;
    }
    #endregion

    #region 카드 구매 처리
    [ClientRpc]
    private void PurchaseResultToCardShopClientRpc(bool success, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            // 해당 클라이언트의 CardShopPresenter 찾기
            CardShopPresenter presenter = FindFirstObjectByType<CardShopPresenter>();
            if (presenter != null)
            {
                // UI 업데이트 로직 직접 호출
                presenter.OnPurchaseResult(success);
            }
        }
    }


    /// <summary>
    /// 살 수 있는지 검증하는 함수
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TryPurchaseCardServerRpc(CardItemData card, ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized card purchase attempt. Requested: {clientId}, Actual: {requesterClientId}");
            PlaySFXClientRpc(DeckSFX.soldFailSFX, clientRpcParams);
            return;
        }   
        
       
        
        //로컬클라이언트의 인벤토리를 조회해서, 인벤의 개수가 max인지 확인. max면 구매 못 함. 로그도 찍기.
        CardInventoryModel myLocalInventoryModel = FindAnyObjectByType<CardInventoryModel>();
        if (myLocalInventoryModel)
        {
            if (myLocalInventoryModel.IsInventoryMaximum())
            {
                Debug.Log($"인벤토리 한도를 초과해서 구매 못 합니다. 인벤토리 한도: {GameConstants.Card.maxCardCount}");
                PlaySFXClientRpc(DeckSFX.soldFailSFX, clientRpcParams);
                PurchaseResultToCardShopClientRpc(false, clientId);
                PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
                return;
            }
        }
        
        
        // 해당 카드가 존재하는지 확인
        int cardItemIdKey = card.cardItemStatusData.cardItemID;
        if (!IsValidCardItemIdKey(cardItemIdKey))
        {
            Debug.Log($"카드가 존재하지 않습니다.");
            PlaySFXClientRpc(DeckSFX.soldFailSFX, clientRpcParams);
            PurchaseResultToCardShopClientRpc(false, clientId);
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
            return;
        }

        // 물량 초과 체크
        if (!IsCardAvailableForPurchase(card.cardIdKey))
        {
            Debug.Log($"물량이 없습니다.");
            PlaySFXClientRpc(DeckSFX.soldFailSFX, clientRpcParams);
            PurchaseResultToCardShopClientRpc(false, clientId);
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
            return;
        }

        // 플레이어 골드 확인
        int playerGold = PlayerHelperManager.Instance.GetPlayerGoldByClientId(clientId);
        if (playerGold < card.cardItemStatusData.price)
        {
            Debug.Log($"돈이 부족합니다.");
            PlaySFXClientRpc(DeckSFX.soldFailSFX, clientRpcParams);
            //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
            PurchaseResultToCardShopClientRpc(false, clientId);
            //구매 실패 여부를 클라이언트에게 전달. (ClientRPC, CardItemData값 보내기)
            PurchaseCardResultClientRpc(false, card, clientId, clientRpcParams);
            return;
        }

        PlaySFXClientRpc(DeckSFX.soldSucceedSFX, clientRpcParams);

        //구매 성공 여부를 CardShop에게 전달. (ClientRPC, bool값 보내기)
        PurchaseResultToCardShopClientRpc(true, clientId);
        //구매 성공 여부를 클라이언트에게 전달. (ClientRPC, CardItemData값 보내기)
        PurchaseCardResultClientRpc(true, card, clientId, clientRpcParams);

    }

    // 카드 판매 처리
    [ServerRpc(RequireOwnership = false)]
    public void TrySellCardServerRpc(CardItemData card, ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        if (clientId != requesterClientId)
            return;

        int cardItemId = card.cardItemStatusData.cardItemID;

        if (!IsValidCardItemIdKey(cardItemId))
            return;

        if (!card.cardDef.isSellableCard)
            return;

        GameObject player = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(clientId);
        if (player == null) return;

        CardInventoryModel inv = player.GetComponent<CardInventoryModel>();
        if (inv == null) return;

        if (!inv.HasCard(cardItemId))
            return;

        int sellPrice = Mathf.FloorToInt(card.cardItemStatusData.price * 0.5f);

        // 골드 지급
        GameManager.Instance.AddPlayerGoldServer(clientId, sellPrice);

        inv.RemoveOwnedCardServerRpc(card);

        // 카드 상태 복귀
        RequestUpdateCardStateServerRpc(cardItemId, CardItemState.None, clientId);
    }

    [ClientRpc]
    private void PlaySFXClientRpc(DeckSFX sfx, ClientRpcParams rpcParams = default )
    {
        switch (sfx)
        {
            case DeckSFX.soldFailSFX:
                SoundManager.Instance.SFXPlay(soldFailSFX.name,soldFailSFX.clip);
                break;
            case DeckSFX.soldSucceedSFX:
                SoundManager.Instance.SFXPlay(solSucceedSFX.name,solSucceedSFX.clip);
                break;
        }
        
    }

    [ClientRpc]
    private void PurchaseCardResultClientRpc(bool success, CardItemData card, ulong clientId, ClientRpcParams sendParams = default)
    {
        if (!success)
        {
            return;
        }

        // GameManager에게 해당 클라이언트의 골드 차감 요청 (책임 분리)
        GameManager.Instance.DeductPlayerGoldServerRpc(clientId, card.cardItemStatusData.price);

        // 서버에게 카드 상태 업데이트 요청 (권위적 데이터 수정)
        RequestUpdateCardStateServerRpc(card.cardItemStatusData.cardItemID, CardItemState.Sold, clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUpdateCardStateServerRpc(int cardItemId, CardItemState newState, ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized card state update attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        // allCardsOnGameData에서 해당 카드 찾아서 상태 업데이트
        for (int i = 0; i < _allCardsOnGameData.Count; i++)
        {
            CardItemData card = _allCardsOnGameData[i];
            if (card.cardItemStatusData.cardItemID == cardItemId)
            {
                CardItemData updatedCard = card;
                updatedCard.cardItemStatusData.state = newState;

                // None으로 복귀 시 진열 정보도 초기화
                if (newState == CardItemState.None)
                {
                    updatedCard.displayingClientId = GameConstants.Card.NOT_DISPLAYING_CLIENT_ID;
                }

                // Sold 상태일 때만 AcquiredTicks 설정 및 인벤토리에 추가
                if (newState == CardItemState.Sold)
                {
                    updatedCard.acquiredTicks = DateTime.Now.Ticks;
                    AddCardToPlayerInventoryClientRpc(updatedCard, clientId, new ClientRpcParams 
                    { 
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } 
                    });
                }
                
                _allCardsOnGameData[i] = updatedCard;
                
                // 모든 클라이언트에게 카드 상태 동기화
                SyncCardStateToAllClientsClientRpc(cardItemId, newState, updatedCard.acquiredTicks, updatedCard.displayingClientId);
                break;
            }
        }
    }

    [ClientRpc]
    private void AddCardToPlayerInventoryClientRpc(CardItemData card, ulong clientId, ClientRpcParams rpcParams = default)
    {
        // 해당 플레이어의 인벤토리에 카드 추가
        GameObject player = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(clientId);
        CardInventoryModel cardInventoryModel = player?.GetComponent<CardInventoryModel>();
        cardInventoryModel?.AddOwnedCardServerRpc(card);
    }

    [ClientRpc]
    private void SyncCardStateToAllClientsClientRpc(int cardItemId, CardItemState newState, long acquiredTicks, ulong displayingClientId)
    {
        // 모든 클라이언트의 CardItemModel에서 해당 카드 상태 업데이트
        CardItemModel[] cardItemModels = FindObjectsByType<CardItemModel>(FindObjectsSortMode.None);
        foreach (CardItemModel model in cardItemModels)
        {
            if (model.CardItemData.cardItemStatusData.cardItemID == cardItemId)
            {
                CardItemData updatedData = model.CardItemData;
                updatedData.cardItemStatusData.state = newState;
                updatedData.acquiredTicks = acquiredTicks;
                updatedData.displayingClientId = displayingClientId;
                model.UpdateCardStateFromServer(updatedData);
            }
        }
    }

    [ClientRpc]
    public void SyncCardDataToAllClientsClientRpc(int cardItemId, CardItemData cardData)
    {
        // 모든 클라이언트의 CardItemModel에서 해당 카드 데이터 동기화
        CardItemModel[] cardItemModels = FindObjectsByType<CardItemModel>(FindObjectsSortMode.None);
        foreach (CardItemModel model in cardItemModels)
        {
            if (model.CardItemData.cardItemStatusData.cardItemID == cardItemId)
            {
                model.UpdateCardStateFromServer(cardData);
                break;
            }
        }
    }

    /// <summary>
    /// 서버에서 카드 진열 요청 처리 (클라이언트별 독립적 진열)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDisplayCardsServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized card display attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        // 해당 클라이언트의 기존 진열 카드들을 None으로 변경
        ClearClientDisplayCards(clientId);

        // 구매 가능한 카드들 수집 (Sold 상태가 아니고 다른 클라이언트가 진열하지 않은 것)
        List<CardItemData> availableCards = new List<CardItemData>();
        foreach (CardItemData card in _allCardsOnGameData)
        {
            if (card.cardItemStatusData.state != CardItemState.Sold && 
                card.displayingClientId == GameConstants.Card.NOT_DISPLAYING_CLIENT_ID)
            {
                availableCards.Add(card);
            }
        }

        // 진열할 카드 수 결정 (최대 5개)
        int displayCount = Mathf.Min(availableCards.Count, 5);
        
        // 랜덤하게 카드 선택하여 Solding 상태로 변경
        List<CardItemData> selectedCards = new List<CardItemData>();
        for (int i = 0; i < displayCount; i++)
        {
            int randomIndex = Random.Range(0, availableCards.Count);
            var selectedCard = availableCards[randomIndex];
            
            // Solding 상태로 변경하고 displayingClientId 설정
            for (int j = 0; j < _allCardsOnGameData.Count; j++)
            {
                var card = _allCardsOnGameData[j];
                if (card.cardItemStatusData.cardItemID == selectedCard.cardItemStatusData.cardItemID)
                {
                    var updatedCard = card;
                    updatedCard.cardItemStatusData.state = CardItemState.Solding;
                    updatedCard.displayingClientId = clientId;
                    _allCardsOnGameData[j] = updatedCard;
                    
                    selectedCards.Add(updatedCard);
                    
                    // 모든 클라이언트에게 상태 동기화
                    SyncCardStateToAllClientsClientRpc(card.cardItemStatusData.cardItemID, CardItemState.Solding, updatedCard.acquiredTicks, clientId);
                    break;
                }
            }
            
            availableCards.RemoveAt(randomIndex);
        }

        // 클라이언트별 진열 개수 업데이트
        _clientDisplayCounts[clientId] = selectedCards.Count;

        // 진열된 카드 정보를 요청한 클라이언트에게 전달
        DisplayCardsResultClientRpc(selectedCards.ToArray(), clientId);
    }

    /// <summary>
    /// 특정 클라이언트의 진열 카드들을 None으로 변경
    /// </summary>
    private void ClearClientDisplayCards(ulong clientId)
    {
        if (!IsHost)
        {
            return;
        }
        // 해당 클라이언트가 진열한 카드들만 None으로 변경
        for (int i = 0; i < _allCardsOnGameData.Count; i++)
        {
            CardItemData card = _allCardsOnGameData[i];
            if (card.cardItemStatusData.state == CardItemState.Solding && 
                card.displayingClientId == clientId)
            {
                CardItemData updatedCard = card;
                updatedCard.cardItemStatusData.state = CardItemState.None;
                updatedCard.displayingClientId = GameConstants.Card.NOT_DISPLAYING_CLIENT_ID;
                _allCardsOnGameData[i] = updatedCard;
                
                // 모든 클라이언트에게 상태 동기화
                SyncCardStateToAllClientsClientRpc(card.cardItemStatusData.cardItemID, CardItemState.None, updatedCard.acquiredTicks, GameConstants.Card.NOT_DISPLAYING_CLIENT_ID);
            }
        }
    }

    [ClientRpc]
    private void DisplayCardsResultClientRpc(CardItemData[] displayedCards, ulong targetClientId)
    {
        // 요청한 클라이언트에게만 진열 결과 전달
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            // CardShopModel에게 진열 결과 전달
            DebugUtils.AssertNotNull(cardShopPresenter, "CardShopPresenter", this);
            cardShopPresenter.OnDisplayCardsResult(displayedCards);
        }
    }



    // 이 메서드는 권위성 위반으로 제거됨
    // 클라이언트가 서버 데이터를 직접 수정 요청하는 것은 금지
    // 대신 RequestUpdateCardStateServerRpc를 사용해야 함

    #endregion

    #region CSV 파싱 메서드들 (CardDataPresenter에서 이동)
    private static IEnumerable<CardDef> ParseCardTable(string csv)
    {
        List<string> rows = SplitRows(csv);
        List<CardDef> list = new List<CardDef>(); 
        if (rows.Count == 0) return list;

        List<string> headers = SplitCols(rows[0]);
        int Idx(string name) { for (int headerIndex = 0; headerIndex < headers.Count; headerIndex++) if (headers[headerIndex].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return headerIndex; return -1; }

        int iID = Idx("CardID"), iName = Idx("CardName"), iTier = Idx("Tier"), iType = Idx("Type"),
            iSub = (Idx("SubType") >= 0 ? Idx("SubType") : Idx("SubType (사용X)")),
            iUni = Idx("IsUniqueCard"), iSell = Idx("IsSellableCard"), iBuyClass = Idx("BuyableClass"),
            iClass = Idx("UsableClass"), iMap = Idx("Map_Restriction"),
            iPrice = Idx("BasePrice"), // iCost = Idx("BaseCost"),
            iDesc = Idx("Description"), iImg = Idx("CardIImagePath"),
            iAmount = Idx("AmountOfCardItem"),
            iValue = Idx("Value"), iIcon = Idx("CardIconResourcePath");

        string S(List<string> columns, int i) => (i >= 0 && i < columns.Count) ? (columns[i]?.Trim() ?? "") : "";

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            try
            {
                List<string> columns = SplitCols(rows[rowIndex]);
                if (columns.Count == 0) continue;
                
                string cardIdStr = (iID >= 0 && iID < columns.Count ? columns[iID].Trim() : "");
                if (!int.TryParse(cardIdStr, out _)) continue;
                
                string valueRaw = TryGetHeaderValue(headers, columns, "Value", out var _v) ? _v : "";
                CardValue value = ToCardValue(valueRaw);

                int usable = ToInt(S(columns, iClass));
                int buyable = (iBuyClass >= 0) ? ToInt(S(columns, iBuyClass)) : usable;

                list.Add(new CardDef
                {
                    cardID = ToInt(S(columns, iID)),
                    cardName = S(columns, iName),
                    tier = ToTier(S(columns, iTier)),
                    type = ToType(S(columns, iType)),
                    subType = ToSubType(S(columns, iSub)),
                    Value = value,
                    isUniqueCard = ToBool(S(columns, iUni)),
                    isSellableCard = ToBool(S(columns, iSell)),
                    buyableClass = buyable,  
                    usableClass = usable,  
                    mapRestriction = ToInt(S(columns, iMap)),
                    basePrice = ToInt(S(columns, iPrice)),
                    // baseCost = ToInt(S(columns, iCost)),
                    description = S(columns, iDesc),
                    cardIImagePath = S(columns, iImg),
                    cardIconResourcePath = S(columns, iIcon),
                    amountOfCardItem = ToInt(S(columns, iAmount)),
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeckManager] Failed to parse card data at row {rowIndex + 1}: {ex.Message}\nRow data: {rows[rowIndex]}");
                throw;
            }
        }
        return list;
    }

    private static IEnumerable<StringRow> ParseStringTable(string csv)
    {
        List<string> rows = SplitRows(csv);
        List<StringRow> list = new List<StringRow>(); 
        if (rows.Count == 0) return list;
        List<string> headers = SplitCols(rows[0]);

        int Idx(params string[] names)
        {
            for (int i = 0; i < headers.Count; i++)
                foreach (string name in names)
                    if (headers[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        int iKey = Idx("Key", "StrID", "StringID"), iKr = Idx("KR", "KO", "Korean"), iEn = Idx("EN", "English");

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> columns = SplitCols(rows[rowIndex]);
            string key = (iKey >= 0 && iKey < columns.Count) ? columns[iKey].Trim() : "";
            if (string.IsNullOrEmpty(key)) continue;
            list.Add(new StringRow { key = key, kr = (iKr >= 0 && iKr < columns.Count ? columns[iKr].Trim() : ""), en = (iEn >= 0 && iEn < columns.Count ? columns[iEn].Trim() : "") });
        }
        return list;
    }

    private static IEnumerable<ResourceRow> ParseResourceTable(string csv)
    {
        var rows = SplitRows(csv);
        var list = new List<ResourceRow>(); 
        if (rows.Count == 0) return list;
        List<string> headers = SplitCols(rows[0]);

        int Idx(params string[] names)
        {
            for (int i = 0; i < headers.Count; i++)
                foreach (string name in names)
                    if (headers[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        int iKey = Idx("Key", "ResID", "CardIImagePath"), iPath = Idx("Path", "ResourcePath", "SpritePath");

        for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            List<string> columns = SplitCols(rows[rowIndex]);
            string key = (iKey >= 0 && iKey < columns.Count) ? columns[iKey].Trim() : "";
            if (string.IsNullOrEmpty(key)) continue;
            list.Add(new ResourceRow { key = key, path = (iPath >= 0 && iPath < columns.Count ? columns[iPath].Trim() : "") });
        }
        return list;
    }

    // HTTP 요청 및 CSV 파싱 유틸리티 (CardDataPresenter에서 이동)
    private static async Task<string> GetTextAsync(string url, CancellationToken ct)
    {
        using UnityWebRequest req = UnityWebRequest.Get(url);
        UnityWebRequestAsyncOperation op = req.SendWebRequest();
        while (!op.isDone) { if (ct.IsCancellationRequested) { req.Abort(); break; } await Task.Yield(); }
        if (req.result != UnityWebRequest.Result.Success) throw new Exception($"GET {url} -> {req.responseCode} {req.error}");
        return req.downloadHandler?.text ?? "";
    }

    // CSV 파싱 유틸리티 (CardDataModel에서 이동)
    private static List<string> SplitRows(string csv)
    {
        var t = (csv ?? "").Replace("\r\n", "\n").Replace("\r", "\n"); 
        return new List<string>(t.Split('\n'));
    }
    
    private static List<string> SplitCols(string line)
    {
        var res = new List<string>(); 
        if (line == null) { res.Add(""); return res; }
        bool q = false; 
        var sb = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"') { if (q && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; } else q = !q; }
            else if (c == ',' && !q) { res.Add(sb.ToString()); sb.Length = 0; }
            else sb.Append(c);
        }
        res.Add(sb.ToString()); 
        return res;
    }
    
    private static int ToInt(string s) 
    { 
        if (string.IsNullOrEmpty(s)) return 0;
        
        s = s.Trim(); 
        if (s == "" || s == "-") return 0;
        
        // 소수점이나 특수문자가 포함된 경우 제거하고 숫자만 추출
        if (s.Contains("."))
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                return (int)d;
            }
        }
        
        // 천 단위 구분자 제거
        s = s.Replace(",", "").Replace(" ", "");
        
        return int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int result) 
            ? result 
            : 0;
    }
    
    private static bool ToBool(string s) 
    { 
        s = (s ?? "").Trim().ToLowerInvariant(); 
        return s == "true" || s == "1" || s == "y"; 
    }

    private static TierEnum ToTier(string s)
    {
        if (int.TryParse(s, out var n)) return (TierEnum)n;
        s = (s ?? "").Trim().ToLowerInvariant();

        return s switch
        {
            "common" => TierEnum.Common,
            "rare" => TierEnum.Rare,
            "special" => TierEnum.Special,

            "none" => TierEnum.None,
            "bronze" => TierEnum.Common,
            "silver" => TierEnum.Rare,
            "sIlver" => TierEnum.Rare,
            "gold" => TierEnum.Special,

            _ => TierEnum.None
        };
    }

    private static TypeEnum ToType(string s)
    {
        if (int.TryParse(s, out var n)) return (TypeEnum)n;
        s = (s ?? "").Trim().ToLowerInvariant();

        return s switch
        {
            "attack" => TypeEnum.Attack,
            "defense" => TypeEnum.Defense,
            "special" => TypeEnum.Special,

            "number" => TypeEnum.Number,
            "operator" => TypeEnum.Operator,
            "roll" => TypeEnum.Roll,

            _ => TypeEnum.None
        };
    }
    private static SubTypeEnum ToSubType(string s)
    {
        if (string.IsNullOrEmpty(s))
            return SubTypeEnum.None;

        // 숫자로 들어오는 경우도 대비
        if (int.TryParse(s, out var n) &&
            Enum.IsDefined(typeof(SubTypeEnum), n))
            return (SubTypeEnum)n;

        s = s.Trim().ToLowerInvariant();

        return s switch
        {
            "const" => SubTypeEnum.Const,
            "n" => SubTypeEnum.N,
            "revolt" => SubTypeEnum.Revolt,
            "farmer" => SubTypeEnum.Farmer,
            "animal" => SubTypeEnum.Animal,

            _ => SubTypeEnum.None
        };
    }

    private static CardValue ToCardValue(string s)
    {
        s = (s ?? "").Trim().ToUpperInvariant();

        // 숫자 0~6
        if (s is "0") return CardValue.V0;
        if (s is "1") return CardValue.V1;
        if (s is "2") return CardValue.V2;
        if (s is "3") return CardValue.V3;
        if (s is "4") return CardValue.V4;
        if (s is "5") return CardValue.V5;
        if (s is "6") return CardValue.V6;

        // 심볼/연산
        return s switch
        {
            "N" => CardValue.N,
            "ADD" => CardValue.ADD,
            "SUB" => CardValue.SUB,
            "MULT" => CardValue.MULT,
            "DIV" => CardValue.DIV,
            _ => CardValue.Unknown
        };
    }

    #endregion
    
    //초기화 함수
    public void Initialize()
    {
        for (int i = 0; i < _allCardsOnGameData.Count; i++)
        {
            CardItemData card = _allCardsOnGameData[i];

            card.cardItemStatusData.state = CardItemState.None;
        
            card.displayingClientId = GameConstants.Card.NOT_DISPLAYING_CLIENT_ID;

            _allCardsOnGameData[i] = card;
        }
        
        _clientDisplayCounts.Clear();
    }
}
