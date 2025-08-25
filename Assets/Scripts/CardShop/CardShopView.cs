using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public interface ICardShopView
{
    event Action<int, ulong, int> OnClickBuy;
    event Action OnClickLock;
    event Action OnClickReRoll;

    void ShowLoading(bool on);
    void ShowResult(bool success, string msg);

    void SetLockedVisual(bool locked);
    void SetRefreshInteractable(bool interactable);
}

public sealed class CardShopView : MonoBehaviour
{
    [Header("UI")]

    [SerializeField] private Button lockButton;
    [SerializeField] private Button rerollButton;

    public event Action<int, ulong, int> OnClickBuy;
    public event Action OnClickLock;
    public event Action OnClickReRoll;

    private void Awake()
    {

        if (lockButton) lockButton.onClick.AddListener(() => OnClickLock?.Invoke());
        if (rerollButton) rerollButton.onClick.AddListener(() => OnClickReRoll?.Invoke());
    }

    public void SetRefreshInteractable(bool interactable)
    {
        if (rerollButton) rerollButton.interactable = interactable;
    }


    #region x버튼 바인딩 함수

    private GameObject cardShopInstance;
    private void Start()
    {
        cardShopInstance = GameObject.FindWithTag("CardShop");
        cardShopInstance.SetActive(false);
    }

    /// <summary>
    /// X버튼 바인딩 함수
    /// </summary>
    public void XButton_OnClick()
    {
        Debug.Log("[CardShopView] XButton_OnClick");
        cardShopInstance.GetComponent<Animator>().SetBool("Active", false);
    }    

    #endregion


}
