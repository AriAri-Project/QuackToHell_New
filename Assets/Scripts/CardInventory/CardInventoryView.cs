using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;

public class CardInventoryView : MonoBehaviour
{
    private static readonly int Active = Animator.StringToHash("Active");

    #region 인벤토리 모습 업데이트
    [Header("인벤토리 UI의 하위 오브젝트: Content를 넣어주세요.")]
    [SerializeField]
    private GameObject content;
    
    // [Header("인벤토리 UI의 하위 오브젝트: Show Card Shop Button을 넣어주세요.")]
    // [SerializeField]
    // private Button showCardShopButton;

    [Header("인벤토리 UI의 하위 오브젝트: Close Inventory Button을 넣어주세요.")]
    [SerializeField] 
    private Button closeButton; 
    /*
    [Header("UI References")]
    private GameObject cardShopPanel;
    private Animator cardShopPanelAnimator;
    //private ulong clientId = 0;
    */
    public event Action OnShowCardShopClicked;
    public event Action OnCloseInventoryClicked;

    private void Start()
    {
        // DebugUtils.AssertNotNull(showCardShopButton, "showCardShopButton", this);
        DebugUtils.AssertNotNull(closeButton, "closeButton", this);
        // showCardShopButton.onClick.AddListener(ShowCardShopButton_OnClick);
        closeButton.onClick.AddListener(CloseInventoryButton_OnClick);
        /*
        if (SceneManager.GetActiveScene().name == GameScenes.Village)
        {
            GameObject cardShopCanvas = GameObject.FindGameObjectWithTag(GameTags.UI_CardShopCanvas);
            DebugUtils.AssertNotNull(cardShopCanvas, "UI_CardShopCanvas", this);
            cardShopPanel = cardShopCanvas.transform.GetChild(0).gameObject;
            DebugUtils.AssertNotNull(cardShopPanel,"cardShopPanel", this);
            cardShopPanelAnimator = cardShopPanel.GetComponent<Animator>();
            DebugUtils.AssertNotNull(cardShopPanelAnimator,"cardShopPanelAnimator", this);
        }
        
        clientId = NetworkManager.Singleton.LocalClientId;
        */
    }

    private void OnDestroy()
    {
        // if (showCardShopButton != null)
        {
            // showCardShopButton.onClick.RemoveListener(ShowCardShopButton_OnClick);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseInventoryButton_OnClick);
        }
    }
    
    public void UpdateInventoryView(NetworkList<CardItemData> ownedCards)
    {
        //TODO 인벤토리 모습 업데이트
        //1. content 산하 오브젝트 삭제
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        
        //2. 팩토리에서 카드 아이템 생성
        foreach (CardItemData card in ownedCards)
        {
            GameObject cardItemForInventory = CardItemFactoryManager.Instance.CreateCardForInventory(card);
            //캔버스 부착: 인벤토리 오브젝트의 산하의 Content오브젝트 아래에 카드 부착
            cardItemForInventory.transform.SetParent(content.transform, false);
        }
    }

 
    
    #endregion

    public void CloseInventory()
    {
        gameObject.SetActive(false);
    }

    #region 버튼

    // private void ShowCardShopButton_OnClick()
    // {
        // OnShowCardShopClicked?.Invoke();
    // }

    private void CloseInventoryButton_OnClick()
    {
        OnCloseInventoryClicked?.Invoke();
    }

    // note cba0898: MVP 패턴 위반 - CardShopPresenter에서 카드 표시 요청
    /*
    public void PlusButton_OnClick()
    {
        // 카드샵 패널 켜기
        cardShopPanel.SetActive(true);
        // 애니메이션 트리거
        if (cardShopPanelAnimator != null)
        {
            cardShopPanelAnimator.SetBool(Active, true);
        }

        // 카드샵 열릴 때 카드 표시 요청
        RequestCardShopDisplay();
    }
    
    private void RequestCardShopDisplay()
    {
        // 각 클라이언트에서 개별적으로 카드 표시
        CardShopPresenter cardShopPresenter = FindFirstObjectByType<CardShopPresenter>();
        if (cardShopPresenter != null)
        {
            cardShopPresenter.RequestDisplayCards(clientId);
        }
    }*/
    #endregion
}
