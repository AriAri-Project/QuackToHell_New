using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class SkillButtonUI : UIHUD
{
    public Action onInteractButton;
    public Action onKillButton;
    public Action onSavotageButton;
    public Action<ulong> onCorpseReportButton;
    
    private Button Button_Savotage;
    private Button Button_Kill;
    private Button Button_Report;
    private Button Button_Interaction;

    private TextMeshProUGUI SavotageCooltime;
    private TextMeshProUGUI KillCooltime;
    
    private PlayerView playerView;
    private PlayerModel playerModel;
    private PlayerPresenter  playerPresenter;
    private IRoleStrategy roleStrategy;
    private PlayerJob playerJob;
    private RoleController roleController;
    
    
    private FarmerStrategy farmerStrategy;
    private GhostStrategy ghostStrategy;

    
    private const float INTERACT_COOLDOWN_MAX = 0.5f;
    private float interactCooldownTimer = 0f;
    private bool canInteract=false;
    public enum Buttons
    {
        Button_Savotage,
        Button_Kill,
        Button_Report,
        Button_Interaction
    }

    enum Texts
    {
        SavotageCooltime,
        KillCooltime,
    }

    private void Start()
    {
        base.Init();
        
        Bind<Button>(typeof(Buttons));
        
        Button_Interaction = Get<Button>((int)Buttons.Button_Interaction);
        BindEvent(Button_Interaction.gameObject, OnDynamicInteractionButton, GameEvents.UIEvent.Click);
        Button_Kill = Get<Button>((int)Buttons.Button_Kill);
        BindEvent(Button_Kill.gameObject, OnKillButton, GameEvents.UIEvent.Click);
        Button_Report = Get<Button>((int)Buttons.Button_Report);
        BindEvent(Button_Report.gameObject, OnCorpseReportButton, GameEvents.UIEvent.Click);
        Button_Savotage = Get<Button>((int)Buttons.Button_Savotage);
        BindEvent(Button_Savotage.gameObject, OnSavotageButton, GameEvents.UIEvent.Click);
        
        Bind<TextMeshProUGUI>(typeof(Texts));
        SavotageCooltime =  Get<TextMeshProUGUI>((int)Texts.SavotageCooltime);
        KillCooltime = Get<TextMeshProUGUI>((int)Texts.KillCooltime);
        
        
        playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(NetworkManager.Singleton.LocalClientId);
        playerView.OnObjectEntered += HandleObjectEntered;
        playerView.OnObjectExited += HandleObjectExited;
        playerView.onCorpseDetected += OnCorpseDetected;
        playerView.onCorpseExited += OnCorpseExited;
        
        
        roleStrategy = playerView.GetComponent<RoleController>().CurrentStrategy;
        roleController =  playerView.GetComponent<RoleController>();
        
        if (roleStrategy is FarmerStrategy)
        {
            farmerStrategy = roleStrategy as FarmerStrategy;
            farmerStrategy.OnKillSuccess += OnKillSuccessed;
            farmerStrategy.OnSavotageSuccess += OnSavotageSuccessed;
            farmerStrategy.OnKillCooldownReady += HandleKillCooldownReady; 
            farmerStrategy.OnSavotageCooldownReady += HandleSavotageCooldownReady;
            farmerStrategy.OnVentEnter += HandleVentEnter;
        }
        
        if (roleStrategy is GhostStrategy)
        {
            ghostStrategy =  roleStrategy as GhostStrategy;
            ghostStrategy.onDead += ShowGhostUI;
        }

        playerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId);
        playerJob = playerModel.GetPlayerCurrentJob();
        playerPresenter = playerModel.GetComponent<PlayerPresenter>();
        
        SetUpButtons();
    }

    private void Update()
    {
        if (interactCooldownTimer < INTERACT_COOLDOWN_MAX && !canInteract)
        {
            interactCooldownTimer += Time.deltaTime;
        }
        else
        {
            canInteract = true;
        }
        
        if (playerJob == PlayerJob.Farmer && farmerStrategy != null && playerView != null)
        {
            // 타겟이 있고 쿨타임이 준비되었으면 버튼 활성화
            bool shouldBeEnabled = playerView.TargetPlayerCache != null && farmerStrategy.CanKill;
        
            if (shouldBeEnabled && !Button_Kill.interactable)
            {
                EnableButton(Buttons.Button_Kill);
            }
            else if (!shouldBeEnabled && Button_Kill.interactable)
            {
                DisableButton(Buttons.Button_Kill);
            }
            
            //킬,사보 쿨타임 업데이트
            ShowKillCooltime();
            ShowSavotageCooltime();
        }
        
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Alive && playerView != null)
        {
            bool reportShouldBeEnabled = playerView.TargetCorpseCache != null;
            if (reportShouldBeEnabled && !Button_Report.interactable)
            {
                EnableButton(Buttons.Button_Report);
            }
            else if (!reportShouldBeEnabled && Button_Report.interactable)
            {
                DisableButton(Buttons.Button_Report);
            }
        }
    }

    private void ShowKillCooltime()
    {
        float remainTime = farmerStrategy.KillCooltimeMax - farmerStrategy.KillCooltimer;
        KillCooltime.text = Mathf.CeilToInt(remainTime).ToString();
    }

    private void ShowSavotageCooltime()
    {
        float remainTime = farmerStrategy.SavotageCooltimeMax - farmerStrategy.SavotageCooltimer;
        SavotageCooltime.text = Mathf.CeilToInt(remainTime).ToString();
    }
    
    private void HandleKillCooldownReady()
    {
        if (playerView != null && playerView.TargetPlayerCache != null)
        {
            EnableButton(Buttons.Button_Kill);
        }
    }

    private void HandleVentEnter()
    {
        EnableButton(Buttons.Button_Interaction);
        farmerStrategy.IsVentEntered = true; 
    }
    private void HandleSavotageCooldownReady()
    {
        EnableButton(Buttons.Button_Savotage);
    }

    private void OnDestroy()
    {
        // 1. FarmerStrategy 관련 이벤트 해제
        if (farmerStrategy != null)
        {
            farmerStrategy.OnKillSuccess -= OnKillSuccessed;
            farmerStrategy.OnKillCooldownReady -= HandleKillCooldownReady;
            farmerStrategy.OnSavotageSuccess -= OnSavotageSuccessed;
            farmerStrategy.OnSavotageCooldownReady -= HandleSavotageCooldownReady;
            farmerStrategy.OnVentEnter -= HandleVentEnter;
        }

        // 2. GhostStrategy 관련 이벤트 해제 
        if (ghostStrategy != null)
        {
            ghostStrategy.onDead -= ShowGhostUI;
        }
        
        // 3. PlayerView 관련 이벤트 해제
        if (playerView != null)
        {
            playerView.OnObjectEntered -= HandleObjectEntered;
            playerView.OnObjectExited -= HandleObjectExited;
            playerView.onCorpseDetected -= OnCorpseDetected; 
            playerView.onCorpseExited -= OnCorpseExited;    
        }
    }

    private void OnKillSuccessed()
    {
        DisableButton(Buttons.Button_Kill);
    }

    
    private void OnSavotageSuccessed()
    {
        DisableButton(Buttons.Button_Savotage);
    }
    

  
    private void HandleObjectEntered(GameObject targetObject)
    {
        if (targetObject.CompareTag(GameTags.PlayerCorpse))
        {   
            if(playerModel.GetPlayerCurrentJob()==PlayerJob.Ghost){
                return;
            }
            EnableButton(Buttons.Button_Report);
        }

        //상호작용 오브젝트 감지
        if (targetObject.CompareTag(GameTags.ConvocationOfTrial))
        {
            SetInteractionButtonImageByObject(GameTags.ConvocationOfTrial);
            EnableButton(Buttons.Button_Interaction);
        }
        
        if(targetObject.CompareTag(GameTags.Vent)){

            SetInteractionButtonImageByObject(GameTags.Vent);

            PlayerJob playerJob = playerModel.GetPlayerCurrentJob(); // 현재 역할 확인
                
            if(playerJob == PlayerJob.Animal)
            {
                // Animal: Interact 버튼 비활성화
                DisableButton(Buttons.Button_Interaction);
            }
            else if(playerJob == PlayerJob.Farmer)
            {
                // Farmer: Interact 버튼 활성화
                EnableButton(Buttons.Button_Interaction);
            }
            
        }
       
        if(targetObject.CompareTag(GameTags.MiniGame)){
           SetInteractionButtonImageByObject(GameTags.MiniGame);
           EnableButton(Buttons.Button_Interaction);
        }
    }

    
    

    private void HandleObjectExited(GameObject targetObject)
    {
        if(farmerStrategy!=null)
        {
            if (farmerStrategy.IsVentEntered)
            {
                return;
            }
        }
        
        
        if (targetObject.CompareTag(GameTags.PlayerCorpse))
        {
            DisableButton(Buttons.Button_Report);   
        }
        
        //상호작용 오브젝트 종류에서 Trigger Exit되면, 기본 상호작용 버튼 이미지로 변경
        //vent, rarecardshop, exit, minigame, teleport, convocationoftrial
        if(targetObject.CompareTag(GameTags.Vent))
        {
            SetInteractionButtonDefault();
            DisableButton(Buttons.Button_Interaction);
        }
        
        if(targetObject.CompareTag(GameTags.MiniGame))
        {
            
            SetInteractionButtonDefault();
            DisableButton(Buttons.Button_Interaction);
            
        }
        
        if (targetObject.CompareTag(GameTags.ConvocationOfTrial))
        {
            SetInteractionButtonDefault();
            DisableButton(Buttons.Button_Interaction);
        }   
    }

    public void OnCorpseDetected(GameObject corpse)
    {
        EnableButton(Buttons.Button_Report);
    }
    public void OnCorpseExited(GameObject corpse)
    {
        DisableButton(Buttons.Button_Report);
    }
    

    /// <summary>
    /// 유령 UI 표시
    /// </summary>
    public void ShowGhostUI()
    {
        // 유령 전용 UI 세팅
        SetUpButtons();
    }
    public void SetInteractionButtonImageByObject(string objectTag ){
        //현재 플레이어 역할을 확인하고 적절한 이미지로 변경
        //Resources/Sprites/InteractionButtons/ 에서 이미지를 찾아서 변경
        PlayerJob playerJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerCurrentJob();
        string spritePath = GetSpritePathByTag(objectTag);
        if(spritePath.Contains("Vent")){
            if(playerJob == PlayerJob.Farmer){
                spritePath = "Sprites/InteractionButtons/InteractionButtonVent";
            }
            else if(playerJob == PlayerJob.Animal){
                spritePath = "Sprites/InteractionButtons/InteractionButtonDefault";
            }
        }
            
        if (!string.IsNullOrEmpty(spritePath))
        {
            Sprite interactionSprite = Resources.Load<Sprite>(spritePath);
            DebugUtils.AssertNotNull(interactionSprite, $"interactionSprite for {objectTag}", this);

            Image interactButtonImage = Button_Interaction.GetComponent<Image>();
            DebugUtils.AssertNotNull(interactButtonImage, "interactButtonImage", this);
            interactButtonImage.sprite = interactionSprite;
        }   
    }
    // 태그에 따른 스프라이트 경로 반환
    private string GetSpritePathByTag(string objectTag)
    {
        return objectTag switch
        {
            GameTags.ConvocationOfTrial => "Sprites/InteractionButtons/InteractionButtonTrialConvocation",
            GameTags.Vent => "Sprites/InteractionButtons/InteractionButtonVent",
            GameTags.MiniGame => "Sprites/InteractionButtons/InteractionButtonMinigame",
            _ => "Sprites/InteractionButtons/InteractionButtonDefault" // 기본 이미지
        };
    }
    
    private void SetUpButtons()
    {
        var model = PlayerHelperManager.Instance
            .GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId);

        PlayerJob currentJob = model.GetPlayerCurrentJob();

        PlayerStatusData status = model.GetPlayerStatusData();
        PlayerJob initialJob = status.initialJob;   
        
        // Ghost면 initialJob을, 그 외에는 현재 job을 사용
        PlayerJob baseJob = currentJob == PlayerJob.Ghost ? initialJob : currentJob;

        // 모든 버튼 기본 상태 초기화
        SetAllButtonsActiveState(baseJob);
        SetInteractionButtonDefault();

        switch (baseJob)
        {
            case PlayerJob.Farmer:
                SetupFarmerButtons();
                break;

            case PlayerJob.Animal:
                SetupAnimalButtons();
                break;

            default:
                Debug.Log($"[SkillButtonUI] baseJob={baseJob} -> 버튼 세팅 스킵");
                break;
        }
    }
    private void SetAllButtonsActiveState(PlayerJob playerJob)
    {
        Button_Kill.interactable = false;
        Button_Report.interactable = false;
        Button_Interaction.interactable = false; 
        Button_Savotage.interactable = false; 
    }

    public void SetInteractionButtonDefault()
    {
        Sprite defaultSprite = Resources.Load<Sprite>("Sprites/InteractionButtons/InteractionButtonDefault");
        DebugUtils.AssertNotNull(defaultSprite, "defaultSprite", this);
        Image interactButtonImage = Button_Interaction.GetComponent<Image>();
        DebugUtils.AssertNotNull(interactButtonImage, "interactButtonImage", this);
        interactButtonImage.sprite = defaultSprite;
    }

    private void SetupFarmerButtons()
    {
        Button_Savotage.gameObject.SetActive(true);
        Button_Kill.gameObject.SetActive(true);
        Button_Report.gameObject.SetActive(true);
        Button_Interaction.gameObject.SetActive(true);
    }

    private void SetupAnimalButtons()
    {
        Button_Savotage.gameObject.SetActive(false);
        Button_Kill.gameObject.SetActive(false);
        Button_Report.gameObject.SetActive(true);
        Button_Interaction.gameObject.SetActive(true);
    }
    
    public void EnableButton(Buttons buttonName)
    {
        switch(buttonName){
            case Buttons.Button_Interaction:
                Button_Interaction.interactable = true;
                break;
            case Buttons.Button_Kill:
                Button_Kill.interactable = true;
                break;
            case Buttons.Button_Report:
                Button_Report.interactable = true;
                break;
            case Buttons.Button_Savotage:
                Button_Savotage.interactable = true;
                break;
        }
    }

    public void DisableButton(Buttons buttonName)
    {
        switch(buttonName){
            case Buttons.Button_Interaction:
                Button_Interaction.interactable = false;
                break;
            case Buttons.Button_Kill:
                Button_Kill.interactable = false;
                break;
            case Buttons.Button_Report:
                Button_Report.interactable = false;
                break;
            case Buttons.Button_Savotage:
                Button_Savotage.interactable = false;
                break;
        }
    }
    
    //버튼입력 이벤트들
    public void OnKillButton(PointerEventData eventData){
        onKillButton?.Invoke();
    }

    public void OnSavotageButton(PointerEventData eventData){
        
        onSavotageButton?.Invoke();
    }
    
    public void OnDynamicInteractionButton(PointerEventData eventData){
        if (!canInteract) return;
        interactCooldownTimer = 0f;
        canInteract = false;
        onInteractButton?.Invoke();;
    }
    
    public void OnCorpseReportButton(PointerEventData eventData)
    { 
        ulong targetCorpseId = playerView.TargetCorpseCache.GetComponent<PlayerCorpse>().ClientId;
       onCorpseReportButton?.Invoke(targetCorpseId);
    }
}
