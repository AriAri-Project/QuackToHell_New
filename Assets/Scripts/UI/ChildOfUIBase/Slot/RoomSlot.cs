using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RoomSlot : UISlot
{
    [SerializeField] private float lobbyNameScrollSpeed = 0.5f;
    [SerializeField] private float scrollIntervalMax = 0.5f;
    private float scrollIntervalTimer = 0f;
    private TextMeshProUGUI PlayerNum_Text;
    private TextMeshProUGUI RoomCode_Text;
    private TextMeshProUGUI IsPrivate_Text;
    private TextMeshProUGUI LobbyName_Text;
    private Scrollbar LobbyName_Scrollbar;
    private TMP_Text Enter_Button_Text;
    private Button Enter_Button;
    enum Texts
    {
        LobbyName_Text,
        IsPrivate_Text,
        PlayerNum_Text,
        RoomCode_Text,
        Enter_Button_Text
    }

    enum Buttons
    {
        Enter_Button
    }

    enum Scrollbars
    {
        LobbyName_Scrollbar,
    }
    
    // Awake를 사용하는 이유: HomeUI에서 Instantiate 직후 SetIsPrivate()를 호출하므로,
    // Start()보다 먼저 실행되는 Awake()에서 컴포넌트 참조를 초기화해야 함
    private void Awake()
    {
        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
        PlayerNum_Text = Get<TextMeshProUGUI>((int)Texts.PlayerNum_Text);
        RoomCode_Text = Get<TextMeshProUGUI>((int)Texts.RoomCode_Text);
        IsPrivate_Text = Get<TextMeshProUGUI>((int)Texts.IsPrivate_Text);
        LobbyName_Text = Get<TextMeshProUGUI>((int)Texts.LobbyName_Text);
        Enter_Button_Text = Get<TextMeshProUGUI>((int)Texts.Enter_Button_Text);
        Bind<Scrollbar>(typeof(Scrollbars));
        LobbyName_Scrollbar = Get<Scrollbar>((int)Scrollbars.LobbyName_Scrollbar);
        Bind<Button>(typeof(Buttons));
        Enter_Button = Get<Button>((int)Buttons.Enter_Button);
        BindEvent(Enter_Button.gameObject,OnClick_Enter_Button_gameObject, GameEvents.UIEvent.Click);
    }

    private void Update()
    {
        if (LobbyName_Scrollbar.value >= 1)
        {
            scrollIntervalTimer+=Time.deltaTime;
        }

        if (scrollIntervalTimer >= scrollIntervalMax)
        {
            LobbyName_Scrollbar.value=0;
            scrollIntervalTimer=0;
        }
        
        LobbyName_Scrollbar.value+=Time.deltaTime*lobbyNameScrollSpeed;
    }

    private async void OnClick_Enter_Button_gameObject(PointerEventData data)
    {
        if(IsPrivate_Text.text=="Private") return;
        bool joinSuccess = await LobbyManager.Instance.JoinLobbyByCode(RoomCode_Text.text);
        if (joinSuccess)
        {
            //로비씬으로 이동
            await SceneManager.LoadSceneAsync(GameScenes.Lobby, LoadSceneMode.Single);
            //로비씬 이동 후, 클라이언트로써 게임 참가
            LobbyManager.Instance.JoinAsClient();
        }
    }


    public void SetLobbyName(string lobbyName)
    {
        LobbyName_Text.text = lobbyName;
    }

    public void SetIsPrivate(bool isPrivate)
    {
        IsPrivate_Text.text = isPrivate ? "Private" : "Public";
        Enter_Button_Text.text = isPrivate ? "" : "Join";
        Enter_Button.enabled = !isPrivate;
    }

    public void SetPlayerNum(int currentPlayerNum, int maxPlayerNum)
    {
        PlayerNum_Text.text = $"{currentPlayerNum}/{maxPlayerNum}";
    }

    public void SetRoomCode(string roomCode, bool isPrivate)
    {
        if (isPrivate)
        {
            RoomCode_Text.text = "";
            return;
        }
        RoomCode_Text.text = roomCode;
    }

}
