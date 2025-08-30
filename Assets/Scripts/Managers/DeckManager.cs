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
    private NetworkList<CardItemData> allCardsOnGameData = new NetworkList<CardItemData>();
    public NetworkList<CardItemData> AllCardsOnGameData => allCardsOnGameData;
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
                    cardItemStatusData = new CardStatusData
                    {
                        CardID = card.Key,
                        CardItemID = card.Key + i,
                        Price = card.Value.BasePrice,
                        Cost = card.Value.BaseCost,
                        State = CardItemState.None
                    }
                };
            }

            allCardsOnGameData.Add(cardItemData);
        }
        await Task.CompletedTask;
    }

    #endregion
}
