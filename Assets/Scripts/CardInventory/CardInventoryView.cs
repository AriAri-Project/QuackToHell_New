using UnityEngine;
using System.Collections.Generic;

public class CardInventoryView : MonoBehaviour
{
    #region 인벤토리 모습 업데이트
    [Header("인벤토리 UI의 하위 오브젝트: Content를 넣어주세요.")]
    [SerializeField]
    private GameObject content;

    [Header("CardShop 프리팹을 넣어주세요.")]
    [SerializeField]
    private GameObject cardShop;

    private GameObject cardShopInstance;

    private void Start()
    {
        var cardShopCanvasInstance = Instantiate(cardShop);
        cardShopInstance = GameObject.FindWithTag("CardShop");
        
    }
    public void UpdateInventoryView(List<InventoryCard> ownedCards)
    {
        //TODO 인벤토리 모습 업데이트
        //1. content 산하 오브젝트 삭제
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        //2. 팩토리에서 카드 아이템 생성
        foreach (var card in ownedCards)
        {
            CardItemFactory.Instance.CreateCardForInventory(card);
        }
    }
    #endregion

    #region 버튼
    public void XButton_OnClick()
    {
        gameObject.SetActive(false);
    }
    public void PlusButton_OnClick()
    {
        
        cardShopInstance.SetActive(true);
        cardShopInstance.GetComponent<Animator>().SetBool("Active", true);
    }
    #endregion
}
