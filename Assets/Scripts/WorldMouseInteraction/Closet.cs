using UnityEngine;
using WorldMouseInteraction;

public class Closet : MonoBehaviour, IClickableWorldObj
{
    public void OnClick()
    {
        UIManager.Instance.ShowPopupUI<LobbyClosetPopup>();
    }
}
