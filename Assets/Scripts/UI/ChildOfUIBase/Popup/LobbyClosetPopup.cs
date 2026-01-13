using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyClosetPopup : UIPopup
{
    
    enum Dropdowns
    {
        Dropdown_Color,
        Dropdown_Skill
    }

    enum InputFields
    {
        NicknameSettingInputField,
    }

    enum Buttons
    {
        XButton,
    }

    
    private TMP_Dropdown colorDropdown;
    private TMP_Dropdown skillDropdown;
    
    private TMP_InputField nicknameSettingInputField;
    
    private PlayerModel playerModel;
    private PlayerView playerView;
    private void Start()
    {
        playerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId);
        playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(NetworkManager.Singleton.LocalClientId);
        
        base.Init();
        Bind<TMP_Dropdown>(typeof(Dropdowns));
        colorDropdown = Get<TMP_Dropdown>((int)Dropdowns.Dropdown_Color);
        colorDropdown.onValueChanged.AddListener(OnColorDropdownButton);
        skillDropdown =  Get<TMP_Dropdown>((int)Dropdowns.Dropdown_Skill);
        skillDropdown.onValueChanged.AddListener(OnSkillDropdownButton);
        
        Bind<TMP_InputField>(typeof(InputFields));
        nicknameSettingInputField = Get<TMP_InputField>((int)InputFields.NicknameSettingInputField);
        nicknameSettingInputField.onEndEdit.AddListener(OnNicknameSettingInputField);
        nicknameSettingInputField.onSelect.AddListener((_) => { playerView.SetIgnorePlayerMoveInputServerRpc(true); });
        nicknameSettingInputField.onDeselect.AddListener((_) => { playerView.SetIgnorePlayerMoveInputServerRpc(false); });
        
        Bind<Button>(typeof(Buttons));
        Button  XButton = Get<Button>((int)Buttons.XButton);
        BindEvent(XButton.gameObject, OnClick_XButton, GameEvents.UIEvent.Click);
    }

    private void OnNicknameSettingInputField(string value)
    {
        playerModel.ChangeNicknameServerRpc(value);
    }
    private void OnColorDropdownButton(Int32 colorIndex)
    {
        PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).ChangeColorServerRpc(colorIndex, NetworkManager.Singleton.LocalClientId);
    }

    private void OnSkillDropdownButton(Int32 skillIndex)
    {
        //플레이어 farmer strategy에 스킬을 저장
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        FarmerStrategy farmerStrategy = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(localClientId).GetComponent<FarmerStrategy>();
        farmerStrategy.ChangeSkillServerRpc(skillIndex);
    }
    
    private void OnClick_XButton(PointerEventData data)
    {
        //최상단 팝업이 LobbySettingPopup이어야함.
        UIManager.Instance.ClosePopupUI();
        if (gameObject!=null)
        {
            Destroy(gameObject);
        }
    }
}
