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
    public GameObject cardItemPrefab;

    // CardItemId 요청을 관리하는 딕셔너리
    private readonly Dictionary<int, System.Action<int>> _pendingCardItemIdRequests = new();
    private int _nextRequestId = 0;





  

    public GameObject CreateCardForInventory(InventoryCard inventoryCard)
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
            return null;
        }
        #endregion

        #region 카드 생성

        // inventoryCard에 이미 있는 cardItemId를 그대로 사용
        

        //프리팹 생성 
        GameObject cardItemForInventory = Instantiate(cardItemPrefab, Vector3.zero, Quaternion.identity);

        //태그 부여
        cardItemForInventory.tag = QETag.CardForInventory.ToString();
        
        //크기 조정
        RectTransform cardItemForSaleRectTransform = cardItemForInventory.GetComponent<RectTransform>();
        Vector2 newSize = new Vector2(200, 300);
        cardItemForSaleRectTransform.sizeDelta = newSize;

        //주입할 데이터 생성
        CardItemStatusData cardItemStatusData = new CardItemStatusData
        {
            CardItemID = inventoryCard.CardItemId,
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
        #endregion

        return cardItemForInventory;
    }

    public bool CreateCardForSale(int cardId, Vector3 inputPosition)
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
            return false;
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
                    return false;
                }
                break;
            }
        }

        #endregion
        #region 카드생성
        //프리팹 생성
        GameObject cardItemForSale = Instantiate(cardItemPrefab, inputPosition, Quaternion.identity);
        
        //태그 부여
        cardItemForSale.tag = QETag.CardForSale.ToString();
        //크기 조정
        RectTransform cardItemForSaleRectTransform = cardItemForSale.GetComponent<RectTransform>();
        Vector2 newSize = new Vector2(200, 350);
        cardItemForSaleRectTransform.sizeDelta = newSize;

        //물량 감소
        DeckManager.Instance.DecreaseCardTotalCount(cardId);

        //card item id 생성
        int cardItemId = cardId + DeckManager.Instance.GetCardTotalCount(cardId);

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
        #endregion
        return true;
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
