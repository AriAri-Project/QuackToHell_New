using TMPro;
using Unity.Netcode;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUI : UIHUD
{
    public AudioSource buttonClickSFX;
    

    private TMP_Text codeText;

    
    private PlayerModel _boundPlayer;          // 현재 Ready 이벤트를 구독한 플레이어
    private Action _spawnCallback;             // PlayerFactoryManager에 등록할 콜백 캐시
    enum Texts
    {
        Text_Code,
        Text_Button_StartGame,

    }

    enum Buttons
    {
        Button_Back,
        Button_StartGame,
        Button_CopyCode,
    }

    private void Start()
    {
        base.Init();
        
 
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        codeText = Get<TextMeshProUGUI>((int)Texts.Text_Code);
        codeText.text = LobbyManager.Instance.HostLobbyCode;


        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "게임 시작";
        }
        else
        {
            Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame).text = "준비";
        }
        
        Bind<Button>(typeof(Buttons));
        GameObject Button_Back_gameObject =  Get<Button>((int)Buttons.Button_Back).gameObject;
        BindEvent(Button_Back_gameObject, OnClick_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_StartGame_gameObject = Get<Button>((int)Buttons.Button_StartGame).gameObject;
        BindEvent(Button_StartGame_gameObject, OnClick_Button_StartGame, GameEvents.UIEvent.Click);
        GameObject Button_CopyCode_gameObject = Get<Button>((int)Buttons.Button_CopyCode).gameObject;
        BindEvent(Button_CopyCode_gameObject, OnClick_Button_CopyCode, GameEvents.UIEvent.Click);
        
        

        // 이미 로컬 플레이어가 있는지 먼저 확인
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        var localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);

        if (localPlayer != null)
        {
            // 플레이어가 이미 스폰되어 있다면 곧바로 바인딩
            StartCoroutine(BindHandlePlayerStatusChanged());
        }
        else
        {
            // 아니면 기존처럼 스폰 이벤트 기다리기
            _spawnCallback = () =>
            {
                StartCoroutine(BindHandlePlayerStatusChanged());
            };
            PlayerFactoryManager.Instance.onPlayerSpawned += _spawnCallback;
        }
    }

    IEnumerator BindHandlePlayerStatusChanged()
    {
        //컴포넌트 초기화까지 대기
        yield return new WaitForEndOfFrame();

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel localPlayer = PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);

        if (localPlayer == null)
            yield break;

        // 1) 이전에 다른 PlayerModel에 묶여 있었다면 해제
        if (_boundPlayer != null)
        {
            _boundPlayer.PlayerStatusData.OnValueChanged -= HandlePlayerStatusChanged;
            _boundPlayer = null;
        }

        // 2) 새 로컬 플레이어에 구독
        localPlayer.SubscribeToPlayerReadyStatusChanges(HandlePlayerStatusChanged);
        _boundPlayer = localPlayer;

        // 3) onPlayerSpawned 콜백은 한 번 쓰고 나면 제거
        if (_spawnCallback != null && PlayerFactoryManager.Instance != null)
        {
            PlayerFactoryManager.Instance.onPlayerSpawned -= _spawnCallback;
            _spawnCallback = null;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[LobbyUI] OnDestroy");

        if (_boundPlayer != null)
        {
            _boundPlayer.PlayerStatusData.OnValueChanged -= HandlePlayerStatusChanged;
            _boundPlayer = null;
        }

        if (_spawnCallback != null && PlayerFactoryManager.Instance != null)
        {
            PlayerFactoryManager.Instance.onPlayerSpawned -= _spawnCallback;
            _spawnCallback = null;
        }
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
                // #C8C8C8 (밝은 회색) 적용
                obj.GetComponentInParent<Image>().color = new Color(0.7843f, 0.7843f, 0.7843f, 1f); 
            }
            
        }
        else{
            var obj = Get<TextMeshProUGUI>((int)Texts.Text_Button_StartGame);
            if (obj)
            {
                obj.GetComponentInParent<Image>().color = new Color(1f, 1f, 1f, 1f); 
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
        //로비 나가기
        LobbyManager.Instance.CleanUpLobby();
    }


    private void OnClick_Button_CopyCode(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GUIUtility.systemCopyBuffer = codeText.text;
    }
}
