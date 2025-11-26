using TMPro;
using Unity.Netcode;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public class LobbyUI : UIHUD
{
    public AudioSource buttonClickSFX;
    
    private TMP_Dropdown colorDropdown;
    private TMP_Text codeText;

    enum Dropdowns
    {
        Dropdown_Color,
    }

    enum Texts
    {
        Text_Code,
        Text_Button_StartGame,

    }

    enum Buttons
    {
        Button_Back,
        Button_StartGame,
        Button_CopyCode
    }

    private void Start()
    {
        base.Init();
        
        Bind<TMP_Dropdown>(typeof(Dropdowns));
        colorDropdown = Get<TMP_Dropdown>((int)Dropdowns.Dropdown_Color);
        colorDropdown.onValueChanged.AddListener(OnColorDropdownButton);
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        codeText = Get<TextMeshProUGUI>((int)Texts.Text_Code);
        codeText.text = LobbyManager.Instance.HostLobbyCode;


        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "Game Start";
        }
        else
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "Ready";
        }
        
        Bind<Button>(typeof(Buttons));
        GameObject Button_Back_gameObject =  Get<Button>((int)Buttons.Button_Back).gameObject;
        BindEvent(Button_Back_gameObject, OnClick_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_StartGame_gameObject = Get<Button>((int)Buttons.Button_StartGame).gameObject;
        BindEvent(Button_StartGame_gameObject, OnClick_Button_StartGame, GameEvents.UIEvent.Click);
        GameObject Button_CopyCode_gameObject = Get<Button>((int)Buttons.Button_CopyCode).gameObject;
        BindEvent(Button_CopyCode_gameObject, OnClick_Button_CopyCode, GameEvents.UIEvent.Click);

        //플레이어가 생성된 후에 바인드하기.
        PlayerFactoryManager.Instance.onPlayerSpawned += () =>
        {
            StartCoroutine(BindHandlePlayerStatusChanged());
        };
    }

    IEnumerator BindHandlePlayerStatusChanged()
    {
        //컴포넌트가 초기화 될 떄까지 기다리기
        yield return new WaitForEndOfFrame();
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
        if (localPlayer!=null)
        {
            localPlayer.SubscribeToPlayerReadyStatusChanges(HandlePlayerStatusChanged);
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[LobbyUI] OnDestroy");
    }

    private void OnDisable()
    {
        Debug.Log("[LobbyUI] OnDisable");
    }

    
    private void HandlePlayerStatusChanged(PlayerStatusData previousValue, PlayerStatusData newValue){ 
        
        if(newValue.IsReady){
            var obj= Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame);
            if (obj)
            {
                obj.GetComponentInParent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);   
            }
            
        }
        else{
            var obj = Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame);
            if (obj)
            {
                obj.GetComponentInParent<Image>().color = new Color(0.78f, 0.78f, 0.80f, 1f); 
            }
            
        }
    }


    private void OnClick_Button_StartGame(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            LobbyManager.Instance.StartGame();    
        }
        else
        {
            //ready 변수 켜기
            ToggleReadyState();
        }
    }
    private void ToggleReadyState(){
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
        if(localPlayer!=null){
            localPlayer.ToggleReady();
        }
    }
    private void OnClick_Button_Back(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        LobbyManager.Instance.CleanUpLobby();
    }
    private void OnColorDropdownButton(Int32 colorIndex)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).ChangeColorServerRpc(colorIndex, NetworkManager.Singleton.LocalClientId);
    }

    private void OnClick_Button_CopyCode(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GUIUtility.systemCopyBuffer = codeText.text;
    }
}
