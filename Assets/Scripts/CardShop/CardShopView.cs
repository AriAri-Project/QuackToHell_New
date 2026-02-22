using UnityEngine;
using System;
using UnityEngine.UI;

public interface ICardShopView
{

    event Action OnClickLock;
    event Action OnClickReRoll;

    void ShowLoading(bool on);
    void ShowResult(bool success, string msg);

    void SetLockedVisual(bool locked);
    void SetRefreshInteractable(bool interactable);
}

public class CardShopView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button lockButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private GameObject cardShopPanel;
    [SerializeField] private Image lockButtonImage;

    public event Action OnClickLock;
    public event Action OnClickReRoll;
    public event Action OnClickX;

    private bool isLocked=false;
    private void Awake()
    {
        DebugUtils.AssertNotNull(lockButton, "lockButton", this);
        DebugUtils.AssertNotNull(rerollButton, "rerollButton", this);
        
        lockButton.onClick.AddListener(() => 
        { 
            OnClickLock?.Invoke();
            isLocked = !isLocked;
            if (isLocked)
            {
                lockButtonImage.color = new Color32(200, 200, 200, 255);    
            }
            else
            {
                lockButtonImage.color = new Color32(255, 255, 255, 255);
            }
            
        }); 
        rerollButton.onClick.AddListener(() => OnClickReRoll?.Invoke());
    }


    private void OnDestroy()
    {
        if (lockButton != null)
        {
            lockButton.onClick.RemoveListener(() => OnClickLock?.Invoke());
        }
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(() => OnClickReRoll?.Invoke());
        }
        
    }
    
    public void SetRefreshInteractable(bool interactable)
    {
        if (rerollButton) rerollButton.interactable = interactable;
    }

    public void ToggleCardShopUI(bool isActive)
    {
        DebugUtils.AssertNotNull(cardShopPanel, "cardShopPanel", this);

        cardShopPanel.SetActive(isActive);
    }


}
