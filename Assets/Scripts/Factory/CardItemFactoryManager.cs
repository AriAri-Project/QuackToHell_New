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
public class CardItemFactoryManager : NetworkBehaviour
{
    #region 싱글톤 코드
    //싱글톤로직
    private static CardItemFactoryManager _instance;
    public static CardItemFactoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CardItemFactoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CardItemFactory");
                    _instance = go.AddComponent<CardItemFactoryManager>();
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


    #region 카드 아이템 생성
    [Header ("Card 프리팹을 넣어주세요.")]
    public GameObject cardItemPrefab;

    public GameObject CreateCardForInventory(CardItemData cardItemData)
    {
        #region 유효한 요청인지 확인
        //카드 ID가 존재하는지 확인
        int requestedCardIdKey = cardItemData.CardIdKey;
        if (DeckManager.Instance.IsValidCardIdKey(requestedCardIdKey))
        {
            Debug.LogError($"[CardItemFactory] 유효하지 않은 카드 아이디 요청입니다. CardID: {cardItemData.CardIdKey}");
            return null;
        }
        #endregion

        #region 카드 생성
        //프리팹 생성 
        GameObject cardItemForInventory = Instantiate(cardItemPrefab, Vector3.zero, Quaternion.identity);

        //태그 부여
        cardItemForInventory.tag = QETag.CardForInventory.ToString();
        
        //크기 조정
        RectTransform cardItemForSaleRectTransform = cardItemForInventory.GetComponent<RectTransform>();
        Vector2 newSize = new Vector2(200, 300);
        cardItemForSaleRectTransform.sizeDelta = newSize;

        //Transform 설정
        cardItemForInventory.transform.localScale = Vector3.one;
        cardItemForInventory.transform.localPosition = Vector3.zero;
        #endregion

        return cardItemForInventory;
    }

    public void CreateTotalCardForSale()
    {
        #region 카드생성
        for(int i=0;i < DeckManager.Instance.AllCardsOnGameData.Count; i++)
        {
            //데이터 뽑음
            CardItemData cardItemData = DeckManager.Instance.AllCardsOnGameData[i];
            int cardId = cardItemData.CardIdKey;
            CardDef cardDef = cardItemData.CardDef;
            CardStatusData cardStatusData = cardItemData.CardItemStatusData;

            //프리팹 생성
            GameObject cardItemForSale = Instantiate(cardItemPrefab, Vector3.zero, Quaternion.identity);

            //데이터 주입
            CardItemModel cardItemModel = cardItemForSale.GetComponent<CardItemModel>();
            cardStatusData.State = CardItemState.Solding; // 판매중 카드로 상태 변경
            CardItemData updatedCardItemData = new CardItemData
            {
                CardIdKey = cardId,
                CardDef = cardDef,
                CardItemStatusData = cardStatusData,
                AcquiredTicks = 0 // 판매용 카드는 획득 시간이 없음
            };

            cardItemModel.CardItemData.Value = updatedCardItemData;

            //태그 부여
            cardItemForSale.tag = QETag.CardForSale.ToString();

            //크기 조정
            RectTransform cardItemForSaleRectTransform = cardItemForSale.GetComponent<RectTransform>();
            Vector2 newSize = new Vector2(200, 350);
            cardItemForSaleRectTransform.sizeDelta = newSize;

            // CardForSale 오브젝트의 이름을 CardItemId와 함께 설정
            cardItemForSale.name = $"CardForSale_{cardStatusData.CardItemID}";


    #region 테스트용 코드
    [Obsolete("카드 생성되는지 테스트하는 버튼")]
    public void OnTestCreateCardForSaleButton()
    {
        CardItemFactoryManager.Instance.CreateCardForSale(20000, Vector3.zero);
    }
    #endregion

   
}
