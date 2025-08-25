using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// 카드 아이템 생성 팩토리
/// 카드 정보를 미리 로드한 뒤에, 생성해야 할 때 사용
/// </summary>
public class CardItemFactory : NetworkBehaviour
{
    #region 싱글톤 코드
    //싱글톤로직
    private static CardItemFactory _instance;
    public static CardItemFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CardItemFactory>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CardItemFactory");
                    _instance = go.AddComponent<CardItemFactory>();
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

    #region 생성할 카드 청사진(Definition)
    private readonly Dictionary<int, CardDef> _cards = new();
    public IReadOnlyDictionary<int, CardDef> Cards => _cards;

    public async Task SetCardData(CardKeyValuePair[] cardKeyValuePairs)
    {
        if (cardKeyValuePairs == null)
        {
            Debug.LogError("[CardItemFactory] 전달받은 카드 데이터가 null입니다.");
            return;
        }

        // 기존 데이터 클리어
        _cards.Clear();
        
        // 새로운 데이터 추가
        foreach (var card in cardKeyValuePairs)
        {
            _cards.Add(card.Key, card.Value);
        }
        
        Debug.Log($"[CardItemFactory] {_cards.Count}개 카드 데이터 설정 완료");
        foreach (var card in _cards)
        {
            Debug.Log($"[CardItemFactory] CardID: {card.Key}, CardNameKey: {card.Value.CardNameKey}");
        }
    }

    #endregion

    #region 카드 아이템 생성
    public GameObject cardItemForSalePrefab;
    public GameObject cardItemForInventoryPrefab;

    // CardItemId 요청을 관리하는 딕셔너리
    private readonly Dictionary<int, System.Action<int>> _pendingCardItemIdRequests = new();
    private int _nextRequestId = 0;

    private void Start()
    {
        // DeckManager의 CardItemId 할당 이벤트를 구독
        DeckManager.OnCardItemIdAssigned += OnCardItemIdAssigned;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        DeckManager.OnCardItemIdAssigned -= OnCardItemIdAssigned;
    }

    /// <summary>
    /// CardItemId 할당 결과를 처리합니다.
    /// </summary>
    private void OnCardItemIdAssigned(int cardId, int cardItemId, int requestId)
    {
        if (_pendingCardItemIdRequests.TryGetValue(requestId, out var callback))
        {
            callback(cardItemId);
            _pendingCardItemIdRequests.Remove(requestId);
        }
    }

    /// <summary>
    /// CardItemId를 요청하고 결과를 콜백으로 받습니다.
    /// </summary>
    private void RequestCardItemId(int cardId, System.Action<int> callback)
    {
        int requestId = _nextRequestId++;
        _pendingCardItemIdRequests[requestId] = callback;
        DeckManager.Instance.GetNextCardItemIdServerRpc(cardId, requestId);
    }

    public void CreateCardForInventory(InventoryCard inventoryCard)
    {
        #region 유효한 요청인지 확인
        //카드 ID가 존재하는지 확인
        CardDef cardDef = default;
        bool cardFound = false;
        
        foreach (var card in _cards)
        {
            if (card.Key == inventoryCard.CardID)
            {
                cardDef = card.Value;
                cardFound = true;
                break;
            }
        }
        
        if (!cardFound)
        {
            Debug.LogError($"[CardItemFactory] 카드 아이디에 맞는 카드 데이터가 없습니다. CardID: {inventoryCard.CardID}");
            return;
        }
        #endregion

        // inventoryCard에 이미 있는 cardItemId를 그대로 사용
        CreateCardForInventoryWithId(inventoryCard, cardDef, inventoryCard.CardItemId);
    }

    /// <summary>
    /// CardItemId를 받은 후 실제 카드를 생성합니다.
    /// </summary>
    private void CreateCardForInventoryWithId(InventoryCard inventoryCard, CardDef cardDef, int cardItemId)
    {
        //캔버스 부착: 인벤토리 오브젝트의 산하의 Content오브젝트 아래에 카드 부착
        var cardInventory = GameObject.FindWithTag("CardInventory");
        var content = cardInventory.GetComponentInChildren<GridLayoutGroup>().gameObject;
        
        //프리팹 생성 (부모를 미리 설정하여 NetworkObject reparenting 오류 방지)
        GameObject cardItemForInventory = Instantiate(cardItemForInventoryPrefab, Vector3.zero, Quaternion.identity, content.transform);
        
        //주입할 데이터 생성
        CardItemStatusData cardItemStatusData = new CardItemStatusData
        {
            CardItemID = cardItemId,
            CardID = inventoryCard.CardID,
            Price = inventoryCard.Status.Price,
            Cost = inventoryCard.Status.Cost,
            State = inventoryCard.Status.State
        };

        //데이터 주입
        var cardItemModel = cardItemForInventory.GetComponentInChildren<CardItemModel>();
        cardItemModel.CardDefData = cardDef;
        cardItemModel.CardItemStatusData = cardItemStatusData;
        Debug.Log($"[CardItemFactory] 카드 아이템 생성 완료. CardID: {inventoryCard.CardID}, CardName: {cardDef.CardNameKey}, Price: {cardItemStatusData.Price}, Cost: {cardItemStatusData.Cost}, CardItemID: {cardItemStatusData.CardItemID}");
        
        //Transform 설정
        cardItemForInventory.transform.localScale = Vector3.one;
        cardItemForInventory.transform.localPosition = Vector3.zero;
    }

    public void CreateCardForSale(int cardId, Vector3 inputPosition)
    {
        #region 유효한 요청인지 확인
        //카드 ID가 존재하는지 확인
        CardDef cardDef = default;
        bool cardFound = false;

        foreach (var card in _cards)
        {
            if (card.Key == cardId)
            {
                cardDef = card.Value;
                cardFound = true;
                break;
            }
        }

        if (!cardFound)
        {
            Debug.LogError($"[CardItemFactory] 카드 아이디에 맞는 카드 데이터가 없습니다. CardID: {cardId}");
            return;
        }

        //물량 있는지 확인
        var totalCardsOnGame = DeckManager.Instance.TotalCardsOnGame;
        foreach(var cardStock in totalCardsOnGame)
        {
            if (cardStock.cardId == cardId)
            {
                if (cardStock.cardTotalCount <= 0)
                {
                    Debug.LogError($"[CardItemFactory] 해당 카드의 남은 물량이 없습니다. CardID: {cardId}");
                    return;
                }
                break;
            }
        }

        #endregion

        // DeckManager에서 다음 CardItemId를 요청합니다
        RequestCardItemId(cardId, (cardItemId) => {
            CreateCardForSaleWithId(cardId, cardDef, cardItemId, inputPosition);
        });
    }

    /// <summary>
    /// CardItemId를 받은 후 실제 카드를 생성합니다.
    /// </summary>
    private void CreateCardForSaleWithId(int cardId, CardDef cardDef, int cardItemId, Vector3 inputPosition)
    {
        //프리팹 생성
        GameObject cardItemForSale = Instantiate(cardItemForSalePrefab, inputPosition, Quaternion.identity);
        
        // CardForSale 오브젝트의 이름을 CardItemId와 함께 설정
        cardItemForSale.name = $"CardForSale_{cardItemId}";
        
        //주입할 데이터 생성
        CardItemStatusData cardItemStatusData = new CardItemStatusData
        {
            CardItemID = cardItemId,
            CardID = cardId,
            Price = cardDef.BasePrice,
            Cost = cardDef.BaseCost,
            State = CardItemState.None
        };

        //데이터 주입
        var cardItemModel = cardItemForSale.GetComponentInChildren<CardItemModel>();
        cardItemModel.CardDefData = cardDef;
        cardItemModel.CardItemStatusData = cardItemStatusData;
        Debug.Log($"[CardItemFactory] 카드 아이템 생성 완료. CardID: {cardId}, CardName: {cardDef.CardNameKey}, Price: {cardItemStatusData.Price}, Cost: {cardItemStatusData.Cost}, CardItemID: {cardItemStatusData.CardItemID}");
        //캔버스 부착
        cardItemForSale.transform.SetParent(GameObject.FindWithTag("CardShop").transform);
        cardItemForSale.transform.localScale = Vector3.one;
        cardItemForSale.transform.localPosition = inputPosition;
    }
    #endregion

    #region 테스트용 코드
    [Obsolete("카드 생성되는지 테스트하는 버튼")]
    public void OnTestCreateCardForSaleButton()
    {
        CardItemFactory.Instance.CreateCardForSale(20000, Vector3.zero);
    }
    #endregion
}
