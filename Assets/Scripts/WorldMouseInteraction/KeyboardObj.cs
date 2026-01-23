using UnityEngine;
using WorldMouseInteraction;

public class KeyboardObj : MonoBehaviour,IClickableWorldObj
{
    public void OnClick()
    {
        UIManager.Instance.ShowPopupUI<LobbySettingPopup>();
    }
}
