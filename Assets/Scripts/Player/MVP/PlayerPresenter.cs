using UnityEngine;
using static PlayerView;
using System;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;
using System.Collections;

//NOTE: 민수님
//hudCONTROLLER관련->uiMANAGER로 빼기, 
//STATEGY관련된거중에 MODEL 이나 뷰를 변경하지않는거 (혹은 호출하지 않는 거) : 따로 클래스 빼기
//TAG는 자체가 mvp관련이 아니니까 다른 클래스로 뺴기
//mvp는 유아이와 모델간의 소통이다
//MVP는 유니티코리아의 영상보면 개념이해될듯. 

/// <summary>
/// *MVP를 잘 몰랐을 떄 썼으므로 아래로 의미를 재정의함
/// 플레이어에 활용된 MVP의 의미는 기존 UI용 MVP와 조금 다름:
/// <PlayerModel>           <PlayerPresenter>       <PlayerView>
/// [입력들온거로로직처리] <-   [Model/적절한 곳에 전달]   <-    [TriggerEnter, 키입력 감지]
/// [데이터변화]          ->   [View/적절한 곳에 전달]    ->    [변화한 데이터를 외형에 반영]
/// </summary>
public class PlayerPresenter : NetworkBehaviour
{
    [Header("Components")]
    private PlayerModel playerModel;
    private PlayerView playerView;
    private RoleController roleController;
    private PlayerInput playerInput;
    private FarmerStrategy farmerStrategy;
    private SkillButtonUI skillButtonUI;
    
    [Header("")]
    [SerializeField]    
    private GameObject corpsePrefab;
    [SerializeField] 
    private SpriteRenderer playerSpriteRenderer;
    
    private ChatTestView chatTestView;
    

    // 외부 접근 제한 - 메시지 기반 인터페이스만 사용
    private void Start()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 이벤트 바인딩 (중개자 역할)
        BindEvents();
        
        // 초기 설정
        SetupInitialState();

        

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner)
        {
            //내 오너캐릭터만 입력받기
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
        
        if (!IsOwner)
        {
            return;
        }
        
        
                    
        //챗
        if (SceneManager.GetActiveScene().name == GameScenes.Lobby || SceneManager.GetActiveScene().name == GameScenes.Court)
        {
            chatTestView = FindAnyObjectByType<ChatTestView>();
            if(chatTestView==null)
            {
                StartCoroutine(WaitForSecondsAndBindChatFocusUnFocusEvent(0.5f));
                return;
            }
            chatTestView.OnFocusInputField += OnInputFieldFocused;
            chatTestView.OnUnFocusInputField += OnInputFieldUnfocused;
        }

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsOwner)
        {
            return;
        }
                    
        //챗
        if (SceneManager.GetActiveScene().name == GameScenes.Lobby || SceneManager.GetActiveScene().name == GameScenes.Court)
        {
            chatTestView = FindAnyObjectByType<ChatTestView>();
            if(chatTestView==null)
            {
                StartCoroutine(WaitForSecondsAndBindChatFocusUnFocusEvent(0.5f));
                return;
            }
            chatTestView.OnFocusInputField -= OnInputFieldFocused;
            chatTestView.OnUnFocusInputField -= OnInputFieldUnfocused;
        }
    }


    public override void OnDestroy()
    {
        UnbindEvents();
        
        base.OnDestroy();
    }

    /// <summary>
    /// 클라이언트 연결 시 캐시 무효화
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        PlayerHelperManager.Instance.InvalidateCache();
        Debug.Log($"[PlayerPresenter] Client {clientId} connected - Cache invalidated");
    }

    /// <summary>
    /// 클라이언트 연결 해제 시 캐시 무효화
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        PlayerHelperManager.Instance.InvalidateCache();
        Debug.Log($"[PlayerPresenter] Client {clientId} disconnected - Cache invalidated");
    }


    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        playerModel = GetComponent<PlayerModel>();
        playerView = GetComponent<PlayerView>();
        roleController = GetComponent<RoleController>();
        playerInput = GetComponent<PlayerInput>();
        farmerStrategy = GetComponent<FarmerStrategy>();
        
        DebugUtils.AssertComponent(playerModel, "PlayerModel", this);
        DebugUtils.AssertComponent(playerView, "PlayerView", this);
        DebugUtils.AssertComponent(roleController, "RoleManager", this);
        DebugUtils.AssertComponent(playerInput, "PlayerInput", this);
    }
    
    /// <summary>
    /// 이벤트 바인딩 (중개자 역할)
    /// </summary>
    private void BindEvents()
    {
        
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnLoaded;
        
        // View -> Presenter -> Model
        playerView.OnMovementInput += HandleMovementInput;
        
        playerView.OnKillTryInput += HandleKillInput;
        playerView.OnInteractInput += HandleInteractInput;
        playerView.OnCorpseReported += HandleCorpseReported;
        playerView.OnSavotageTryInput += HandleSavotageInput;

        // Model -> Presenter -> View
        playerModel.PlayerStatusData.OnValueChanged += HandleStatusChanged;
        playerModel.PlayerAppearanceData.OnValueChanged += HandleAppearanceChanged;
        playerModel.PlayerStateData.OnValueChanged += HandleStateChanged;
    
        //note: 민수님
        //프레젠터가 view / model끝까지 토스할필요없이, 중간에서 끊겨도됨
        //프레젠터는 모델이랑 뷰를 이어주는거니까, 모델 / 뷰에 갱신 필요없는데 여기있을 이유x
        //이 4개 함수 관련해서 이벤트(뷰에있던거)랑, 구독함수 character같은 컴포넌트로 통째로 빼기
        
        // View -> Presenter


        //model -> presenter
        //민수님: 얘는 ㄱㅊ
        playerModel.PlayerTag.OnValueChanged += HandleTagChanged;
    }

    private void OnSceneUnLoaded(Scene scene)
    {
        //스킬버튼
        if (scene.name == GameScenes.Village)
        {
            skillButtonUI = FindAnyObjectByType<SkillButtonUI>();
            if (skillButtonUI != null)
            {
                skillButtonUI.onKillButton -= HandleKillInput;
                skillButtonUI.onInteractButton -= HandleInteractInput;
                skillButtonUI.onCorpseReportButton -= HandleCorpseReported;
                skillButtonUI.onSavotageButton -= HandleSavotageInput;
            }
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //스킬버튼
        if (scene.name == GameScenes.Village)
        {
            skillButtonUI = FindAnyObjectByType<SkillButtonUI>();
            if (skillButtonUI != null)
            {
                skillButtonUI.onKillButton += HandleKillInput;
                skillButtonUI.onInteractButton += HandleInteractInput;
                skillButtonUI.onCorpseReportButton += HandleCorpseReported;
                skillButtonUI.onSavotageButton += HandleSavotageInput;
            }
        }
    }
    


    private IEnumerator WaitForSecondsAndBindChatFocusUnFocusEvent(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        chatTestView = FindAnyObjectByType<ChatTestView>();
        if (chatTestView != null)
        {
            chatTestView.OnFocusInputField += OnInputFieldFocused;
            chatTestView.OnUnFocusInputField += OnInputFieldUnfocused;
        }

    }
    
 

    private void OnInputFieldFocused()
    {
        playerView.SetIgnorePlayerMoveInputServerRpc(true);
    }
    private void OnInputFieldUnfocused()
    {
        playerView.SetIgnorePlayerMoveInputServerRpc(false);
    }

    private void UnbindEvents()
    {
        
        //note:민수님: 생명주기관련고민 
        //메모장에 써놓고 코딩한다거나 , awake(본인거) start(참조할때) enabled disabled 는 기본적으로알아두고, 나머지는 작성해놓고 보기
        //*awake는 하이어라키 순서 따라도 달라짐.
        
        //싱글톤vs스태틱 클래스
        //note: 민수님
        //싱글톤: 게임오브젝트 객체로써 필요. 싱글톤안에서 딕셔너리 / 리스트/ 같은 데이터를 관리하고있고 그걸 전역적으로 쓰기위해
        //static클래스: 유틸같은거. vector 거리 계산같은거..
        //필드가있는건 싱글톤
        //*싱글톤은 null체크하는 게 말이안됨. 있다고 가정해서 쓰는거니까
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // static event는 null 체크 필요 없음

        if (playerView != null)
        {
            playerView.OnMovementInput -= HandleMovementInput;
            playerView.OnKillTryInput -= HandleKillInput;
            playerView.OnInteractInput -= HandleInteractInput;
            playerView.OnCorpseReported -= HandleCorpseReported;
            playerView.OnSavotageTryInput -= HandleSavotageInput;
        }

        if (playerModel != null)
        {
            playerModel.PlayerStatusData.OnValueChanged -= HandleStatusChanged;
            playerModel.PlayerAppearanceData.OnValueChanged -= HandleAppearanceChanged;
            playerModel.PlayerStateData.OnValueChanged -= HandleStateChanged;
        }

        //note: 민수님
        //mvp자체가 UI만질라고 하는거니까, ui와 data간 소통있는 게 아니면, 다른 컴포넌트 빼는 게 낫다. 
        //클래스 새로만들기 너무 별로다 할때만 넣기.(웬만하면 분리추천)
        if (playerModel.PlayerTag != null)
        {
            playerModel.PlayerTag.OnValueChanged -= HandleTagChanged;
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
    }

    

    private void HandleTagChanged(FixedString64Bytes previousTag, FixedString64Bytes newTag)
    {
        gameObject.tag = newTag.ToString();
        Debug.Log($"Player tag changed from {previousTag} to {newTag}");
    }
    
    /// <summary>
    /// 초기 설정
    /// </summary>
    private void SetupInitialState()
    {
        // 초기 닉네임 설정
        playerView.UpdateNickname(playerModel.PlayerStatusData.Value.Nickname);
        
        // 초기 색상 설정
        HandleAppearanceChanged(playerModel.PlayerAppearanceData.Value, playerModel.PlayerAppearanceData.Value);
    }


    #region 이벤트 핸들러들 (중개자 역할)
    
    /// <summary>
    /// View에서 받은 움직임 입력을 Model에 전달
    /// </summary>
    private void HandleMovementInput(object sender, EventArgs e)
    {
        OnMovementInputEventArgs onMovementInputEventArgs = (OnMovementInputEventArgs)e;
        playerModel.MovePlayerServerRpc(onMovementInputEventArgs.XDirection, onMovementInputEventArgs.YDirection);
    }
    
    /// <summary>
    /// View에서 받은 킬 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleKillInput()
    {
        if (playerView.TargetPlayerCache == null)
        {
            return;
        }
        
        PlayerModel targetModel = playerView.TargetPlayerCache.GetComponent<PlayerModel>();
        if (targetModel == null)
        {
            return;
        }
        
        ulong targetClientId = targetModel.NetworkObject.OwnerClientId;
        roleController.CurrentStrategy?.Kill(targetClientId);
    }

    
    /// <summary>
    /// View에서 받은 상호작용 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleInteractInput()
    {
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
       
        //벤트 탈출
        if (farmerStrategy.enabled)
        {
            if (farmerStrategy.IsVentEntered)
            {
                if (farmerStrategy.InteratingVentNetworkId != 0)
                {
                    farmerStrategy.ExitVent();
                    return;
                }
            }
        }

        GameObject interactionObj = playerView.InteractObjCache;
        
        if(interactionObj==null)
        {
            return;
        }
        
        //옷장 상호작용
        if(interactionObj.CompareTag(GameTags.Closet))
        {
            UIManager.Instance.ShowPopupUI<LobbyClosetPopup>();
            //로비씬이므로 리턴.(아직 역할배정 전)
            return;
        }
        
        //키보드 상호작용
        if(interactionObj.CompareTag(GameTags.Keyboard))
        {
            UIManager.Instance.ShowPopupUI<LobbySettingPopup>();
            //로비씬이므로 리턴.(아직 역할배정 전)
            return;
        }
        
        string targetObjTag= interactionObj.tag;
        ulong targetObjectId=0;
        if (interactionObj.GetComponent<NetworkObject>() != null)
        {
            targetObjectId =  interactionObj.GetComponent<NetworkObject>().NetworkObjectId;
        }

        if (targetObjTag == GameTags.Vent && playerModel.GetPlayerJob()==PlayerJob.Farmer )
        {
            playerView.SetIgnorePlayerMoveInputServerRpc(true);
        }

         
        
        roleController.CurrentStrategy?.Interact(targetObjTag,targetObjectId);
    }

    
    /// <summary>
    /// View에서 받은 시체 리포트를 RoleStrategy에 전달
    /// </summary>
    private void HandleCorpseReported(ulong corpseId)
    {
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleController.CurrentStrategy?.ReportCorpse(corpseId);
    }
    

    
    /// <summary>
    /// View에서 받은 사보타지 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleSavotageInput()
    {
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
        if (playerModel.GetPlayerJob() != PlayerJob.Farmer) return;
        UIManager.Instance.ShowPopupUI<SabotagePopup>();
    }
    
    /// <summary>
    /// Model에서 받은 상태 변경을 View에 전달
    /// </summary>
    private void HandleStatusChanged(PlayerStatusData previousValue, PlayerStatusData newValue)
    {
        // 닉네임 업데이트
        if(newValue.nickname != previousValue.nickname){
            playerView.UpdateNickname(newValue.nickname);
        }
        
        // 역할 변경 감지
        if (previousValue.job != newValue.job)
        {
            roleController?.ChangeRole(newValue.job);
        }
    }
    
    /// <summary>
    /// Model에서 받은 외관 변경을 View에 전달
    /// </summary>
    private void HandleAppearanceChanged(PlayerAppearanceData previousValue, PlayerAppearanceData newValue)
    {
        playerView.ChangeApprearence(newValue);
    }
    
    
    
    /// <summary>
    /// Model에서 받은 상태 변경 처리 (MVP 패턴 준수)
    /// </summary>
    private void HandleStateChanged(PlayerStateData previousValue, PlayerStateData newValue)
    {
            
    }
    
    #endregion
    
}
