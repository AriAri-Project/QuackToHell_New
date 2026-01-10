using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UISwitcher;

//TODO: 처형자 정보표시 - Lobbymanager에서 필드 가져가서 누가 투표에서 죽었는지 표시하게 구현자에게 인수인계하기 (투표기능 구현자에게)
public class LobbySettingPopup : UIPopup
{
    enum GameObjects
    {
        Tap1ScrollView,
        Tap2ScrollView
    }

    enum Buttons
    {
        Tap1Button,
        Tap2Button,
        XButton
    }

    enum Switches
    {
        PrivateLobbySettingSwitcher,
        DisplayExecutorInformationSettingSwitcher
    }

    enum Sliders
    {
        MaximumNumberOfPlayersSettingSlider,
        MaximumNumberOfFarmersSettingSlider,
        SabotageCooltimeSettingSlider,
        KillCooltimeSettingSlider,
        EyeSightInnerRangeSettingSlider,
        EyeSightOuterRangeSettingSlider

    }

    enum Texts
    {
        MaximumNumberOfPlayersSettingSlderValueText,
        MaximumNumberOfFarmersSettingSlderValueText,
        SabotageCooltimeSettingSlderValueText,
        KillCooltimeSettingSlderValueText,
        EyeSightInnerRangeSettingValueText,
        EyeSightOuterRangeSettingValueText
    }
    
    private static readonly Color TAP_ACTIVED_COLOR = new Color(214f / 255f, 214f / 255f, 214f / 255f, 1f);
    private static readonly Color TAP_DEACTIVED_COLOR = new Color(127f / 255f, 127f / 255f, 127f / 255f, 1f);
    
    private GameObject Tap1ScrollView;
    private GameObject Tap2ScrollView;
    
    private Image Tap1Button_Image;
    private Image Tap2Button_Image;

    private UISwitcher.UISwitcher PrivateLobbySettingSwitcher;
    private UISwitcher.UISwitcher DisplayExecutorInformationSettingSwitcher;

    private Slider MaximumNumberOfPlayersSettingSlider;
    private Slider MaximumNumberOfFarmersSettingSlider;
    private Slider SabotageCooltimeSettingSlider;
    private Slider KillCooltimeSettingSlide;
    private Slider  EyeSightInnerRangeSettingSlider;
    private Slider  EyeSightOuterRangeSettingSlider;

    private TextMeshProUGUI MaximumNumberOfPlayersSettingSlderValueText;
    private TextMeshProUGUI MaximumNumberOfFarmersSettingSlderValueText;
    private TextMeshProUGUI SabotageCooltimeSettingSlderValueText;
    private TextMeshProUGUI KillCooltimeSettingSlderValueText;
    private TextMeshProUGUI EyeSightInnerRangeSettingValueText;
    private TextMeshProUGUI EyeSightOuterRangeSettingValueText;

    
    private void Start()
    {
        base.Init();
        
        Bind<Button>(typeof(Buttons));
        Button Tap1Button = Get<Button>((int)Buttons.Tap1Button);
        Tap1Button_Image = Tap1Button.GetComponent<Image>();
        BindEvent(Tap1Button.gameObject, OnClick_Tap1Button, GameEvents.UIEvent.Click);
        Button Tap2Button = Get<Button>((int)Buttons.Tap2Button);
        Tap2Button_Image = Tap2Button.GetComponent<Image>();
        BindEvent(Tap2Button.gameObject, OnClick_Tap2Button, GameEvents.UIEvent.Click);
        Button  XButton = Get<Button>((int)Buttons.XButton);
        BindEvent(XButton.gameObject, OnClick_XButton, GameEvents.UIEvent.Click);
        
        Bind<GameObject>(typeof(GameObjects));
        Tap1ScrollView = Get<GameObject>((int)GameObjects.Tap1ScrollView);
        Tap2ScrollView = Get<GameObject>((int)GameObjects.Tap2ScrollView);
        
        Bind<UISwitcher.UISwitcher>(typeof(Switches));
        PrivateLobbySettingSwitcher = Get<UISwitcher.UISwitcher>((int)Switches.PrivateLobbySettingSwitcher);
        PrivateLobbySettingSwitcher.onValueChanged.AddListener(OnValueChanged_PrivateLobbySettingSwitcher);
        DisplayExecutorInformationSettingSwitcher = Get<UISwitcher.UISwitcher>((int)Switches.DisplayExecutorInformationSettingSwitcher);
        DisplayExecutorInformationSettingSwitcher.onValueChanged.AddListener(OnValueChanged_DisplayExecutorInformationSettingSwitcher);
        
        Bind<Slider>(typeof(Sliders));
        MaximumNumberOfPlayersSettingSlider = Get<Slider>((int)Sliders.MaximumNumberOfPlayersSettingSlider);
        MaximumNumberOfPlayersSettingSlider.value = LobbyManager.Instance.LobbyData.maxPlayerNum;
        MaximumNumberOfPlayersSettingSlider.onValueChanged.AddListener(OnValueChanged_MaximumNumberOfPlayersSettingSlider);
         MaximumNumberOfFarmersSettingSlider =  Get<Slider>((int)Sliders.MaximumNumberOfFarmersSettingSlider);
        MaximumNumberOfFarmersSettingSlider.value = LobbyManager.Instance.LobbyData.FarmerNum;
        MaximumNumberOfFarmersSettingSlider.onValueChanged.AddListener(OnValueChanged_MaximumNumberOfFarmersSettingSlider);
         SabotageCooltimeSettingSlider =  Get<Slider>((int)Sliders.SabotageCooltimeSettingSlider);
        SabotageCooltimeSettingSlider.value = LobbyManager.Instance.LobbyData.savotageCooltime;
        SabotageCooltimeSettingSlider.onValueChanged.AddListener(OnValueChanged_SabotageCooltimeSettingSlider);
         KillCooltimeSettingSlide =  Get<Slider>((int)Sliders.KillCooltimeSettingSlider);
        KillCooltimeSettingSlide.value = LobbyManager.Instance.LobbyData.killCooltime;
        KillCooltimeSettingSlide.onValueChanged.AddListener(OnValueChanged_KillCooltimeSettingSlide);
        EyeSightInnerRangeSettingSlider =   Get<Slider>((int)Sliders.EyeSightInnerRangeSettingSlider);
        EyeSightInnerRangeSettingSlider.value = LobbyManager.Instance.LobbyData.innerEyesightValue;
        EyeSightInnerRangeSettingSlider.onValueChanged.AddListener((value) => {
            EyeSightInnerRangeSettingValueText.text = $"Slider value: {value}";
        });
        BindEvent(EyeSightInnerRangeSettingSlider.gameObject, OnEndDrag_EyeSightInnerRangeSettingSlider, GameEvents.UIEvent.EndDrag);
        EyeSightOuterRangeSettingSlider =    Get<Slider>((int)Sliders.EyeSightOuterRangeSettingSlider);
        EyeSightOuterRangeSettingSlider.value = LobbyManager.Instance.LobbyData.outerEyesightValue;
        EyeSightOuterRangeSettingSlider.onValueChanged.AddListener((value) => {
            EyeSightOuterRangeSettingValueText.text = $"Slider value: {value}";
        });
        BindEvent(EyeSightOuterRangeSettingSlider.gameObject, OnEndDrag_EyeSightOuterRangeSettingSlider, GameEvents.UIEvent.EndDrag);
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        MaximumNumberOfPlayersSettingSlderValueText = Get<TextMeshProUGUI>((int)Texts.MaximumNumberOfPlayersSettingSlderValueText);
        MaximumNumberOfFarmersSettingSlderValueText = Get<TextMeshProUGUI>((int)Texts.MaximumNumberOfFarmersSettingSlderValueText);
        SabotageCooltimeSettingSlderValueText = Get<TextMeshProUGUI>((int)Texts.SabotageCooltimeSettingSlderValueText);
        KillCooltimeSettingSlderValueText = Get<TextMeshProUGUI>((int)Texts.KillCooltimeSettingSlderValueText);
        EyeSightInnerRangeSettingValueText = Get<TextMeshProUGUI>((int)Texts.EyeSightInnerRangeSettingValueText);
        EyeSightOuterRangeSettingValueText =  Get<TextMeshProUGUI>((int)Texts.EyeSightOuterRangeSettingValueText);
        
        //init
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        Init(lobbyData.isPrivateRoom, lobbyData.maxPlayerNum, lobbyData.FarmerNum, lobbyData.savotageCooltime, lobbyData.killCooltime,lobbyData.isShowKillerInfo, lobbyData.innerEyesightValue, lobbyData.outerEyesightValue);
    }

    
    private void Init(bool isPrivateLobby, int maxPlayerNum, int FarmerNum, int savotageCooltime, int killCooltime, bool isShowKillerInfo, int innerEyesightValue, int  outerEyesightValue)
    {
        //TAP1: 비공개 로비 (T/F), 최대 플레이어 수 (슬라이더), 농장주 수 (슬라이더), 사보타지 쿨타임 (슬라이더), 킬 쿨타임 (슬라이더), 처형자 정보 표시 (T/F)
        PrivateLobbySettingSwitcher.isOn = isPrivateLobby;
    
        MaximumNumberOfPlayersSettingSlider.value = maxPlayerNum;
        MaximumNumberOfPlayersSettingSlderValueText.text = $"Slider value: {maxPlayerNum}";  
    
        MaximumNumberOfFarmersSettingSlider.value = FarmerNum;
        MaximumNumberOfFarmersSettingSlderValueText.text = $"Slider value: {FarmerNum}";  
    
        SabotageCooltimeSettingSlider.value = savotageCooltime;
        SabotageCooltimeSettingSlderValueText.text = $"Slider value: {savotageCooltime}"; 
    
        KillCooltimeSettingSlide.value = killCooltime;
        KillCooltimeSettingSlderValueText.text = $"Slider value: {killCooltime}";  
        
        EyeSightInnerRangeSettingSlider.value = innerEyesightValue;
        EyeSightOuterRangeSettingSlider.value = outerEyesightValue;
        EyeSightInnerRangeSettingValueText.text = $"Slider value: {innerEyesightValue}";
        EyeSightOuterRangeSettingValueText.text =  $"Slider value: {outerEyesightValue}";
    
        DisplayExecutorInformationSettingSwitcher.isOn = isShowKillerInfo;
    
        //TODO:TAP2
    }

    private void OnValueChanged_MaximumNumberOfPlayersSettingSlider(float value)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.maxPlayerNum = (int)value;
        LobbyManager.Instance.LobbyData = lobbyData;
        //text반영
        MaximumNumberOfPlayersSettingSlderValueText.text = $"Slider value: {(int)value}";
    }
        
    
    private void OnValueChanged_MaximumNumberOfFarmersSettingSlider(float value)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.FarmerNum = (int)value;
        LobbyManager.Instance.LobbyData = lobbyData;
        //text반영
        MaximumNumberOfFarmersSettingSlderValueText.text = $"Slider value: {(int)value}";
    }
    private void OnValueChanged_SabotageCooltimeSettingSlider(float value)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.savotageCooltime = (int)value;
        LobbyManager.Instance.LobbyData = lobbyData;
        //text반영
        SabotageCooltimeSettingSlderValueText.text = $"Slider value: {(int)value}";
    }
    private void OnValueChanged_KillCooltimeSettingSlide(float value)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.killCooltime = (int)value;
        LobbyManager.Instance.LobbyData = lobbyData;
        //text반영
        KillCooltimeSettingSlderValueText.text = $"Slider value: {(int)value}";
    }

    private void OnEndDrag_EyeSightInnerRangeSettingSlider(PointerEventData eventData)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.innerEyesightValue = (int)EyeSightInnerRangeSettingSlider.value;
        LobbyManager.Instance.LobbyData = lobbyData;
    }
    private void OnEndDrag_EyeSightOuterRangeSettingSlider(PointerEventData eventData)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.outerEyesightValue = (int)EyeSightOuterRangeSettingSlider.value;
        LobbyManager.Instance.LobbyData = lobbyData;
    }
    
    
    
    private void OnValueChanged_PrivateLobbySettingSwitcher(bool isOn)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.isPrivateRoom = isOn;
        LobbyManager.Instance.LobbyData = lobbyData;
    }
    private void OnValueChanged_DisplayExecutorInformationSettingSwitcher(bool isOn)
    {
        //lobby 데이터에 set
        Data.LobbyData lobbyData = LobbyManager.Instance.LobbyData;
        lobbyData.isShowKillerInfo = isOn;
        LobbyManager.Instance.LobbyData = lobbyData;
    }

    private void OnClick_Tap1Button(PointerEventData data)
    {
        Tap1ScrollView.SetActive(true);
        Tap2ScrollView.SetActive(false);
        Tap1Button_Image.color = TAP_ACTIVED_COLOR;
        Tap2Button_Image.color = TAP_DEACTIVED_COLOR;
    }
    private void OnClick_Tap2Button(PointerEventData data)
    {
        Tap1ScrollView.SetActive(false);
        Tap2ScrollView.SetActive(true);
        Tap1Button_Image.color = TAP_DEACTIVED_COLOR;
        Tap2Button_Image.color = TAP_ACTIVED_COLOR;
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
