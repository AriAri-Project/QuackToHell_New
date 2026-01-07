using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SabotagePopup : UIPopup
{
    
    enum Buttons
    {
        XButton,
        SabotageAButton,
        SabotageBButton
    }

    private RoleController roleController;
    private void Start()
    {
        base.Init();

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        roleController = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(localClientId).GetComponent<RoleController>();
        
        Bind<Button>(typeof(Buttons));
        Button XButton = Get<Button>((int)Buttons.XButton);
        BindEvent(XButton.gameObject, OnClick_XButton, GameEvents.UIEvent.Click);
        Button SabotageAButton = Get<Button>((int)Buttons.SabotageAButton);
        BindEvent(SabotageAButton.gameObject, OnClick_SabotageAButton, GameEvents.UIEvent.Click);
        Button SabotageBButton = Get<Button>((int)Buttons.SabotageBButton);
        BindEvent(SabotageBButton.gameObject, OnClick_SabotageBButton, GameEvents.UIEvent.Click);
    }

    private void OnClick_XButton(PointerEventData data)
    {
        //최상단 팝업이 사보타지여야함
        UIManager.Instance.ClosePopupUI();
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnClick_SabotageAButton(PointerEventData data)
    {
        roleController.CurrentStrategy?.Savotage(SabotageType.LightsOff);
    }
    private void OnClick_SabotageBButton(PointerEventData data)
    {
        roleController.CurrentStrategy?.Savotage(SabotageType.ForcedInteract);

    }

}
