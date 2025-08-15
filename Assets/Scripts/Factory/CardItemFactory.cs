using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 아이템 생성 팩토리
/// 카드 정보를 미리 로드한 뒤에, 생성해야 할 때 사용
/// </summary>
public class CardItemFactory : MonoBehaviour
{
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

    
    // 카드 데이터
    private Dictionary<int, CardDef> cardDataDictionary;
    private bool isCardDataInitialized = false;

    /// <summary>
    /// LobbyController에서 전달받은 카드 데이터를 설정
    /// </summary>
    /// <param name="cardData">카드 데이터 딕셔너리</param>
    public void SetCardData(IReadOnlyDictionary<int, CardDef> cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("[CardItemFactory] 전달받은 카드 데이터가 null입니다.");
            return;
        }

        // 딕셔너리를 복사하여 저장
        cardDataDictionary = new Dictionary<int, CardDef>(cardData);
        isCardDataInitialized = true;
        
        Debug.Log($"[CardItemFactory] {cardDataDictionary.Count}개 카드 데이터 설정 완료");
    }

    /// <summary>
    /// 특정 ID의 카드 데이터 가져오기
    /// </summary>
    /// <param name="cardId">카드 ID</param>
    /// <param name="cardDef">카드 데이터 (출력 매개변수)</param>
    /// <returns>성공 여부</returns>
    public bool TryGetCardData(int cardId, out CardDef cardDef)
    {
        if (!isCardDataInitialized || cardDataDictionary == null)
        {
            cardDef = default;
            Debug.LogWarning("[CardItemFactory] 카드 데이터가 아직 초기화되지 않았습니다.");
            return false;
        }

        return cardDataDictionary.TryGetValue(cardId, out cardDef);
    }

    /// <summary>
    /// 모든 카드 데이터 가져오기
    /// </summary>
    /// <returns>카드 데이터 딕셔너리 (읽기 전용)</returns>
    public IReadOnlyDictionary<int, CardDef> GetAllCardData()
    {
        if (!isCardDataInitialized || cardDataDictionary == null)
        {
            Debug.LogWarning("[CardItemFactory] 카드 데이터가 아직 초기화되지 않았습니다.");
            return new Dictionary<int, CardDef>();
        }

        return cardDataDictionary;
    }

    /// <summary>
    /// 카드 데이터 초기화 상태 확인
    /// </summary>
    public bool IsCardDataInitialized => isCardDataInitialized;

    /// <summary>
    /// 카드 데이터 개수
    /// </summary>
    public int CardDataCount => isCardDataInitialized && cardDataDictionary != null ? cardDataDictionary.Count : 0;
}
