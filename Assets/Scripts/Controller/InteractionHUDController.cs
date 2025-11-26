using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class InteractionHUDController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField]
    private GameObject savotageButton;
    [SerializeField]
    private GameObject killButton;
    [SerializeField]
    private GameObject reportButton;
    [SerializeField]
    private GameObject interactButton;
    private Button savotageButtonButton;
    private Button killButtonButton;
    private Button reportButtonButton;
    private Button interactButtonButton;

    [Header("Player")]
    private PlayerPresenter playerPresenter;


    public void InitializeInteractionHUDUI(){
        playerPresenter = PlayerHelperManager.Instance.GetPlayerPresenterByClientId(NetworkManager.Singleton.LocalClientId);
        savotageButtonButton = savotageButton.GetComponent<Button>();
        killButtonButton = killButton.GetComponent<Button>();
        reportButtonButton = reportButton.GetComponent<Button>();
        interactButtonButton = interactButton.GetComponent<Button>();
        SetUpButtons();
    }
    public void SetInteractionButtonImageByObject(string objectTag ){
        //현재 플레이어 역할을 확인하고 적절한 이미지로 변경
        //Resources/Sprites/InteractionButtons/ 에서 이미지를 찾아서 변경
        PlayerJob playerJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerJob();
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

            Image interactButtonImage = interactButton.GetComponent<Image>();
            DebugUtils.AssertNotNull(interactButtonImage, "interactButtonImage", this);
            interactButtonImage.sprite = interactionSprite;
        }   
    }
    public enum ButtonName{
        TrialConvocation,
        CorpseReport,
        Vent,
        RareCardShop,
        Exit,
        MiniGame,
        Teleport,
        Kill,
    }
    public void EnableButton(ButtonName buttonName)
    {
        switch(buttonName){
            case ButtonName.Kill:
                killButtonButton.interactable = true;
                break;
            case ButtonName.CorpseReport:
                reportButtonButton.interactable = true;
                break;
            case ButtonName.TrialConvocation:
                interactButtonButton.interactable = true;
                break;
            case ButtonName.Vent:
                interactButtonButton.interactable = true;
                break;
            case ButtonName.RareCardShop:
                interactButtonButton.interactable = true;
                break;
            case ButtonName.Exit:
                interactButtonButton.interactable = true;
                break;
            case ButtonName.MiniGame:
                interactButtonButton.interactable = true;
                break;
            case ButtonName.Teleport:
                interactButtonButton.interactable = true;
                break;
        }
    }

    public void DisableButton(ButtonName buttonName)
    {
        switch(buttonName){
            case ButtonName.Kill:
                killButtonButton.interactable = false;
                break;
            case ButtonName.CorpseReport:
                reportButtonButton.interactable = false;
                break;
            case ButtonName.TrialConvocation:
                interactButtonButton.interactable = false;
                break;
                case ButtonName.Vent:
                interactButtonButton.interactable = false;
                break;
            case ButtonName.RareCardShop:
                interactButtonButton.interactable = false;
                break;
            case ButtonName.Exit:
                interactButtonButton.interactable = false;
                break;
            case ButtonName.MiniGame:
                interactButtonButton.interactable = false;
                break;
            case ButtonName.Teleport:
                interactButtonButton.interactable = false;
                break;
        }
    }

    public void SetPlayerInteractionUI(PlayerJob role, bool isDetected, bool canInteract){
        //플레이어 감지시, 역할에 맞는 고정 버튼을 활성화
        //farmer: isDetected상태에 따라 kill버튼 활성화
        if(role == PlayerJob.Farmer){
            if (isDetected)
            {
                //TODO: 쿨타임따라 enable
                if (canInteract)
                {
                    EnableButton(ButtonName.Kill);
                }
            }
            else{
                DisableButton(ButtonName.Kill);
            }
            
        }
        //animal: 변화 x
    }
    //-------------버튼-------------

    public void OnKillButton(){
        if (!killButtonButton.interactable) return;
        playerPresenter?.RequestKill();
    }

    public void OnSavotageButton(){
        if (!savotageButtonButton.interactable) return;
        playerPresenter?.RequestSabotage();
    }
    
    public void OnDynamicInteractionButton(){
        if (!interactButtonButton.interactable) return;
        playerPresenter?.RequestInteract();
    }
    
    public void OnCorpseReportButton(){
        if (!reportButtonButton.interactable) return;
        playerPresenter?.RequestReportCorpse();
    }

    #region Private method
    // 태그에 따른 스프라이트 경로 반환
    private string GetSpritePathByTag(string objectTag)
    {
        return objectTag switch
        {
            GameTags.ConvocationOfTrial => "Sprites/InteractionButtons/InteractionButtonTrialConvocation",
            GameTags.Vent => "Sprites/InteractionButtons/InteractionButtonVent",
            GameTags.RareCardShop => "Sprites/InteractionButtons/InteractionButtonRareCardShop",
            GameTags.Exit => "Sprites/InteractionButtons/InteractionButtonExit",
            GameTags.MiniGame => "Sprites/InteractionButtons/InteractionButtonMinigame",
            GameTags.Teleport => "Sprites/InteractionButtons/InteractionButtonTeleport",
            _ => "Sprites/InteractionButtons/InteractionButtonDefault" // 기본 이미지
        };
    }
    private void SetUpButtons(){
    //로컬플레이어의 역할에 따라 다르게 버튼을 활성화
    PlayerJob playerJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerJob();
    
    // 모든 버튼 기본 설정
    SetAllButtonsActiveState();
    SetInteractionButtonDefault();
    
    switch(playerJob){
        case PlayerJob.Farmer:
            SetupFarmerButtons();
            break;
        case PlayerJob.Animal:
            SetupAnimalButtons();
            break;
        default:
            Debug.Log("유령이어서 Button Setup 안 함");
            break;
        }
    }

    private void SetAllButtonsActiveState()
    {
        killButtonButton.interactable = false;
        reportButtonButton.interactable = false;
        interactButtonButton.interactable = false; 
        PlayerJob playerJob = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId).GetPlayerJob();
        if(playerJob == PlayerJob.Farmer)
        {
            savotageButtonButton.interactable = true; // 항상 활성화
        }
        else
        {
            savotageButtonButton.interactable = false; // Animal은 비활성화
        }
    }

    public void SetInteractionButtonDefault()
    {
        Sprite defaultSprite = Resources.Load<Sprite>("Sprites/InteractionButtons/InteractionButtonDefault");
        DebugUtils.AssertNotNull(defaultSprite, "defaultSprite", this);
        Image interactButtonImage = interactButton.GetComponent<Image>();
        DebugUtils.AssertNotNull(interactButtonImage, "interactButtonImage", this);
        interactButtonImage.sprite = defaultSprite;
    }

    private void SetupFarmerButtons()
    {
        savotageButton.SetActive(true);
        killButton.SetActive(true);
        reportButton.SetActive(true);
        interactButton.SetActive(true);
    }

    private void SetupAnimalButtons()
    {
        savotageButton.SetActive(false);
        killButton.SetActive(false);
        reportButton.SetActive(true);
        interactButton.SetActive(true);
    }
    
   #endregion
}
