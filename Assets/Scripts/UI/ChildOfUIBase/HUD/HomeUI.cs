using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


//TODO: 방제 무한스크롤(자동스크롤)
public class HomeUI : UIHUD
{
    [SerializeField] private GameObject RoomSlot;
    
    public AudioSource buttonClickSFX;
    
    private GameObject GameStartContents_gameObject;
    private GameObject SettingContents_gameObject;
    private GameObject CreateGameContents_gameObject;
    private GameObject EnterCodeContents_gameObject;
    private GameObject FindGame_gameObject;
    private GameObject Button_Lock_gameObject;
    private GameObject Roomlist_Content_gameObject;
    private GameObject Default_gameObject;


    private TextMeshProUGUI Text_MaxPlayerNum;

    private string sessionName="";
    private string code="";
    private bool buttonLock = false;
    private bool isPrivate = false;
    private Color buttonLockColor;
    private int maxPlayerNum=6;
    
    //새로고침 쿨타임
    //쿨타임 근거: https://unity3.tistory.com/14
    private const float refreshCooltime = 2f;
    private float refreshCooltimeTimer=0f;
    
    
    
    enum Buttons
    {
        Button_GameStart,
        Button_Option,
        Button_CreateGame,
        Button_FindGame,
        Button_EnterGame,
        Button_Back_CreateGameContents,
        Button_Back_EnterGameContents,
        Button_Back_FindGame,
        Button_MaxPlayerNumPlus,
        Button_MaxPlayerNumMinus,
        Button_Lock,
        Button_Create,
        Button_EnterCodeConfirm,
        Button_F5
    }

    enum InputFields
    {
        InputField_SessionName,
        InputField_Code
    }

    enum Texts
    {
        Text_MaxPlayerNum,
    }
    
    enum GameObjects
    {
        //버튼누르면 Img_Info에 활성화되는 애들
        GameStartContents,
        SettingContents,
        CreateGameContents,
        EnterCodeContents,
        //Find Game 버튼누르면 활성화되는 애
        FindGame,
        //룸 slot을 담는 content
        Roomlist_Content,
        //default ui
        Default
    }


    private void Update()
    {

        refreshCooltimeTimer += Time.deltaTime;
  
    }

    private void Start()
    {
        base.Init();
        
        
        Bind<GameObject>(typeof(GameObjects));
        GameStartContents_gameObject = Get<GameObject>((int)GameObjects.GameStartContents).gameObject;
        SettingContents_gameObject = Get<GameObject>((int)GameObjects.SettingContents).gameObject;
        CreateGameContents_gameObject = Get<GameObject>((int)GameObjects.CreateGameContents).gameObject;
        EnterCodeContents_gameObject = Get<GameObject>((int)GameObjects.EnterCodeContents).gameObject;
        FindGame_gameObject = Get<GameObject>((int)GameObjects.FindGame).gameObject;
        Default_gameObject = Get<GameObject>((int)GameObjects.Default).gameObject;
        Roomlist_Content_gameObject = Get<GameObject>((int)GameObjects.Roomlist_Content).gameObject;
        
        Bind<Button>(typeof(Buttons));
        GameObject Button_GameStart_gameObject = Get<Button>((int)Buttons.Button_GameStart).gameObject;
        BindEvent(Button_GameStart_gameObject,OnClicked_GameStart, GameEvents.UIEvent.Click);
        GameObject Button_Option_gameObject = Get<Button>((int)Buttons.Button_Option).gameObject;
        BindEvent(Button_Option_gameObject,OnClicked_Option, GameEvents.UIEvent.Click);
        GameObject Button_CreateGame_gameObject = Get<Button>((int)Buttons.Button_CreateGame).gameObject;
        BindEvent(Button_CreateGame_gameObject,OnClicked_CreateGame, GameEvents.UIEvent.Click);
        GameObject Button_FindGame_gameObject = Get<Button>((int)Buttons.Button_FindGame).gameObject;
        BindEvent(Button_FindGame_gameObject,OnClicked_FindGame, GameEvents.UIEvent.Click);
        GameObject Button_EnterGame_gameObject = Get<Button>((int)Buttons.Button_EnterGame).gameObject;
        BindEvent(Button_EnterGame_gameObject,OnClicked_EnterCode, GameEvents.UIEvent.Click);
        GameObject Button_Back_CreateGameContents_gameObject = Get<Button>((int)Buttons.Button_Back_CreateGameContents).gameObject;
        BindEvent(Button_Back_CreateGameContents_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_Back_EnterGameContents_gameObject = Get<Button>((int)Buttons.Button_Back_EnterGameContents).gameObject;
        BindEvent(Button_Back_EnterGameContents_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        GameObject Button_Back_FindGame_gameObject = Get<Button>((int)Buttons.Button_Back_FindGame).gameObject;
        BindEvent(Button_Back_FindGame_gameObject,OnClicked_Button_Back, GameEvents.UIEvent.Click);
        Button_Lock_gameObject = Get<Button>((int)Buttons.Button_Lock).gameObject;
        buttonLockColor = Button_Lock_gameObject.GetComponent<Image>().color;
        BindEvent(Button_Lock_gameObject, OnClicked_ButtonLock,GameEvents.UIEvent.Click);
        GameObject Button_MaxPlayerNumPlus = Get<Button>((int)Buttons.Button_MaxPlayerNumPlus).gameObject;
        BindEvent(Button_MaxPlayerNumPlus,OnClicked_Button_MaxPlayerNumPlus, GameEvents.UIEvent.Click);
        GameObject Button_MaxPlayerNumMinus = Get<Button>((int)Buttons.Button_MaxPlayerNumMinus).gameObject;
        BindEvent(Button_MaxPlayerNumMinus,OnClicked_Button_MaxPlayerNumMinus, GameEvents.UIEvent.Click);
        GameObject Button_Create = Get<Button>((int)Buttons.Button_Create).gameObject;
        BindEvent(Button_Create,OnClicked_Button_Create, GameEvents.UIEvent.Click );
        GameObject Button_EnterCodeConfirm = Get<Button>((int)Buttons.Button_EnterCodeConfirm).gameObject;
        BindEvent(Button_EnterCodeConfirm,OnClicked_Button_EnterCodeConfirm, GameEvents.UIEvent.Click );
        GameObject Button_F5 = Get<Button>((int)Buttons.Button_F5).gameObject;
        BindEvent(Button_F5,OnClicked_Button_F5, GameEvents.UIEvent.Click );
        
        Bind<TMP_InputField>(typeof(InputFields));
        TMP_InputField InputField_SessionName= Get<TMP_InputField>((int)InputFields.InputField_SessionName);
        InputField_SessionName.onSubmit.AddListener(OnSubmit_SessionName);
        InputField_SessionName.onDeselect.AddListener(OnSubmit_SessionName);
        TMP_InputField InputField_Code= Get<TMP_InputField>((int)InputFields.InputField_Code);
        InputField_Code.onSubmit.AddListener(OnSubmit_Code);
        InputField_Code.onDeselect.AddListener(OnSubmit_Code);
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        Text_MaxPlayerNum = Get<TextMeshProUGUI>((int)Texts.Text_MaxPlayerNum);
        Text_MaxPlayerNum.text = maxPlayerNum.ToString();

    }
    

    private void LobbyManagerOnRoomListPulledAddSlotToRoomListContent(List<Lobby> lobbyList)
    {
        //Debug.Log($"[슬롯 추가] 호출됨! StackTrace: {System.Environment.StackTrace}");
        
        // 기존 자식들 모두 삭제
        for (int i = Roomlist_Content_gameObject.transform.childCount-1; i >= 0; i--)
        {
            Transform child = Roomlist_Content_gameObject.transform.GetChild(i);
            Destroy(child.gameObject);
        }
        
        if (lobbyList == null || lobbyList.Count == 0 )
        {
            return;
        }
        

        foreach (var lobby in lobbyList)
        {
            var roomSlotGameObject = Instantiate(RoomSlot, Roomlist_Content_gameObject.transform);
            RoomSlot roomSlot = roomSlotGameObject.GetComponent<RoomSlot>();
            bool isPrivate =  lobby.Data["IsPrivate"].Value == "True";
            roomSlot.SetIsPrivate(isPrivate);
            roomSlot.SetLobbyName(lobby.Name);
            roomSlot.SetPlayerNum(lobby.Players.Count, lobby.MaxPlayers);
            roomSlot.SetRoomCode(lobby.Data["LobbyCode"].Value, isPrivate);
        }
    }
    

    private void OnSubmit_SessionName(string input)
    {
        sessionName = input;
    }
    private void OnSubmit_Code(string input)
    {
        code = input;
    }
    
    
    private async void OnClicked_Button_Create(PointerEventData data)
    {
        try{
            //사운드
            SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
            await LobbyManager.Instance.CreateLobby(sessionName,isPrivate,maxPlayerNum);
            //로비씬으로 이동
            await SceneManager.LoadSceneAsync(GameScenes.Lobby, LoadSceneMode.Single);
            //로비씬 이동 후, 호스트로써 게임 참가
            LobbyManager.Instance.JoinAsHost();
        }
        
        catch(Exception e){
            Debug.Log(e);
        }
    }
    private async void OnClicked_Button_EnterCodeConfirm(PointerEventData data)
    {
        //방 참가: 코드로
        try{
            //사운드
            SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
            bool joinSuccess = await LobbyManager.Instance.JoinLobbyByCode(code);
            if (joinSuccess)
            {
                //로비씬으로 이동
                await SceneManager.LoadSceneAsync(GameScenes.Lobby, LoadSceneMode.Single);
                //로비씬 이동 후, 클라이언트로써 게임 참가
                LobbyManager.Instance.JoinAsClient();
            }
        }
        
        catch(Exception e){
            Debug.Log(e);
        }
    }
    private void OnClicked_Button_MaxPlayerNumMinus(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        maxPlayerNum--;
        Text_MaxPlayerNum.text = maxPlayerNum.ToString();
    }

    private async void OnClicked_Button_F5(PointerEventData  data = null)
    {
        if(refreshCooltimeTimer<refreshCooltime) return;
        
        refreshCooltimeTimer = 0f;
        
        List<Lobby> lobbyList =  await LobbyManager.Instance.ListLobbies();
        LobbyManagerOnRoomListPulledAddSlotToRoomListContent(lobbyList);
    }


    private void OnClicked_Button_MaxPlayerNumPlus(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        maxPlayerNum++;
        Text_MaxPlayerNum.text = maxPlayerNum.ToString();
    }

    private void OnClicked_ButtonLock(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        buttonLock = !buttonLock;
        isPrivate = buttonLock;
        if (buttonLock)
        {
            //색 진하게
            Button_Lock_gameObject.GetComponent<Image>().color = new Color32(200, 200, 200, 255);
        }
        if (!buttonLock)
        {
            //원래의 색
            Button_Lock_gameObject.GetComponent<Image>().color = buttonLockColor;
        }
    }
    private void OnClicked_GameStart(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GameStartContents_gameObject.SetActive(true);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    
    private void OnClicked_Option(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(true);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_CreateGame(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(true);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_EnterCode(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(true);
        FindGame_gameObject.SetActive(false);
    }
    private void OnClicked_FindGame(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        Default_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(true);
        //리스트 풀
        OnClicked_Button_F5();
    }

    

    private void OnClicked_Button_Back(PointerEventData data)
    {
        //사운드
        SoundManager.Instance.SFXPlay("UIClickSFX", buttonClickSFX.clip);
        //TODO: Stack으로 관리해서 이전 화면이 나타나게 Pop 
        GameStartContents_gameObject.SetActive(false);
        SettingContents_gameObject.SetActive(false);
        CreateGameContents_gameObject.SetActive(false);
        EnterCodeContents_gameObject.SetActive(false);
        FindGame_gameObject.SetActive(false);
        Default_gameObject.SetActive(true);
    }
}
