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
/// View와 Model 간 중개자 역할
/// - View의 이벤트를 받아서 Model에 전달
/// - Model의 데이터 변경을 받아서 View에 전달
/// - 외부 클래스는 Presenter를 통해 Player와 소통
/// </summary>
public class PlayerPresenter : NetworkBehaviour
{
    [Header("Components")]
    private PlayerModel playerModel;
    private PlayerView playerView;
    private RoleController roleController;
    private PlayerInput playerInput;
    
    [Header("")]
    [SerializeField]    
    private GameObject corpsePrefab;
    [SerializeField] 
    private SpriteRenderer playerSpriteRenderer;

    [Header("UI")] 
    private InteractionHUDController interactionHUDController;

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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        if(scene.name==GameScenes.Village){
            GameObject interactionHUD = GameObject.FindGameObjectWithTag(GameTags.UI_InteractionHUD);
            interactionHUDController = interactionHUD.GetComponent<InteractionHUDController>();
            interactionHUDController.InitializeInteractionHUDUI();
            playerView.InjectHUDController(interactionHUDController);
        }
    }

    private void PlayerView_OnPlayerExited()
    {
        //현재 역할 확인
        PlayerJob playerJob = playerModel.GetPlayerJob();
        if(interactionHUDController!=null)
        {
            interactionHUDController.SetPlayerInteractionUI(playerJob, false,playerView.CanKill);
        }
    }

    private void PlayerView_OnPlayerDetected(GameObject player)
    {
        //현재 역할 확인
        PlayerJob playerJob = playerModel.GetPlayerJob();
        if(interactionHUDController!=null)
        {
            interactionHUDController.SetPlayerInteractionUI(playerJob, true, playerView.CanKill);
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

    public void SetAllPlayerIgnoreMoveInput(bool value)
    {
        playerView.SetIgnoreAllPlayerMoveInputServerRpc(value);
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

        // View -> Presenter -> Model
        playerView.OnMovementInput += HandleMovementInput;
        playerView.OnKillTryInput += HandleKillInput;
        playerView.OnInteractInput += HandleInteractInput;
        playerView.OnCorpseReported += HandleCorpseReported;
        playerView.OnVentTryInput += HandleVentInput;
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
        playerView.onPlayerDetected += PlayerView_OnPlayerDetected;
        playerView.onPlayerExited += PlayerView_OnPlayerExited;
        playerView.OnObjectEntered += HandleObjectEntered;
        playerView.OnObjectExited += HandleObjectExited;

        //model -> presenter
        //민수님: 얘는 ㄱㅊ
        playerModel.PlayerTag.OnValueChanged += HandleTagChanged;
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
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (playerView != null)
        {
            playerView.OnMovementInput -= HandleMovementInput;
            playerView.OnKillTryInput -= HandleKillInput;
            playerView.OnInteractInput -= HandleInteractInput;
            playerView.OnCorpseReported -= HandleCorpseReported;
            playerView.OnVentTryInput -= HandleVentInput;
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
    }

    private void HandleObjectEntered(Collider2D collision)
    {
        if (!IsOwner)
        {
            return;
        }

        if(interactionHUDController == null) return;

        if (collision.CompareTag(GameTags.PlayerCorpse))
        {   
            
            if(playerModel.GetPlayerJob()==PlayerJob.Ghost){
                return;
            }
            interactionHUDController.EnableButton(InteractionHUDController.ButtonName.CorpseReport);
            
        }

        //상호작용 오브젝트 감지
        if (collision.CompareTag(GameTags.ConvocationOfTrial))
        {
            if (IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.ConvocationOfTrial);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.TrialConvocation);
            }
            
        }
        if(collision.CompareTag(GameTags.Vent)){
            if(IsOwner)
            {
                //ventcontroller와 같은 트리거 범위 적용
                
                VentController ventController = collision.gameObject.GetComponent<VentController>();
                Debug.Assert(ventController != null, "VentController not found");
                float dist = Vector3.Distance(transform.position, ventController.transform.position);
                if(dist > ventController.InteractionRadius){
                    return;
                }

                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Vent);

                PlayerJob playerJob = playerModel.GetPlayerJob(); // 현재 역할 확인
                
                if(playerJob == PlayerJob.Animal)
                {
                    // Animal: Interact 버튼 비활성화
                    interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Vent);
                }
                else if(playerJob == PlayerJob.Farmer)
                {
                    // Farmer: Interact 버튼 활성화
                    interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Vent);
                }
            }
        }
        if(collision.CompareTag(GameTags.RareCardShop)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.RareCardShop);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.RareCardShop);
            }
        }
        if(collision.CompareTag(GameTags.Exit)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Exit);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Exit);
            }
        }
        if(collision.CompareTag(GameTags.MiniGame)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.MiniGame);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.MiniGame);
            }
        }
        if(collision.CompareTag(GameTags.Teleport)){
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonImageByObject(GameTags.Teleport);
                interactionHUDController.EnableButton(InteractionHUDController.ButtonName.Teleport);
            }
        }
        
        
   }
    private void HandleObjectExited(Collider2D collision)
    {
        if(interactionHUDController == null) return;

        if (collision.CompareTag(GameTags.PlayerCorpse))
        {
            if (IsOwner)
            {
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.CorpseReport);
            }
        }
        
        //상호작용 오브젝트 종류에서 Trigger Exit되면, 기본 상호작용 버튼 이미지로 변경
        //vent, rarecardshop, exit, minigame, teleport, convocationoftrial
        if(collision.CompareTag(GameTags.Vent))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Vent);
            }
        }
        if(collision.CompareTag(GameTags.RareCardShop))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.RareCardShop);
            }
        }
        if(collision.CompareTag(GameTags.Exit))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Exit);
            }
        }
        if(collision.CompareTag(GameTags.MiniGame))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.MiniGame);
            }
        }
        if(collision.CompareTag(GameTags.Teleport))
        {
            if(IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.Teleport);
            }
            
        }
        if (collision.CompareTag(GameTags.ConvocationOfTrial))
        {
            if (IsOwner)
            {
                interactionHUDController.SetInteractionButtonDefault();
                interactionHUDController.DisableButton(InteractionHUDController.ButtonName.TrialConvocation);
            }
        }   
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
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        
        roleController.CurrentStrategy?.TryKill();
    }

    
    /// <summary>
    /// View에서 받은 상호작용 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleInteractInput()
    {
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleController.CurrentStrategy?.TryInteract();
    }

    


    
    /// <summary>
    /// View에서 받은 시체 리포트를 RoleStrategy에 전달
    /// </summary>
    private void HandleCorpseReported(ulong reporterClientId)
    {
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;

        roleController.CurrentStrategy?.TryReportCorpse();
    }
    
    /// <summary>
    /// View에서 받은 벤트 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleVentInput()
    {
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleController.CurrentStrategy?.TryVent();
    }
    
    /// <summary>
    /// View에서 받은 사보타지 입력을 RoleStrategy에 전달
    /// </summary>
    private void HandleSavotageInput()
    {
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        if (playerModel.GetPlayerAliveState() == PlayerLivingState.Dead) return;
        
        roleController.CurrentStrategy?.TrySabotage();
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
        // 사망 상태로 변경되었을 때 가시성 업데이트
        if (previousValue.AliveState != newValue.AliveState && 
            newValue.AliveState == PlayerLivingState.Dead)
        {
            UpdateVisibilityForAllPlayers();
        }
    }
    
    #endregion

    #region 외부에서 호출 가능한 메서드들 (중개자 역할)
    
    public void ChangeRole(PlayerJob newRole){
        playerModel.ChangeRole(newRole);
    }


  
    


    /// <summary>
    /// 킬 시도 서버 RPC
    /// </summary>
    [ServerRpc]
    public void TryKillServerRpc(ServerRpcParams rpcParams = default)
    {
        // 서버 검증
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        
        if (roleController.CurrentStrategy?.CanKill() != true)
        {
            Debug.LogWarning($"[Server] Player {OwnerClientId} cannot kill");
            return;
        }
        
        // 킬 범위 내의 플레이어 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Player) && collider.gameObject != gameObject)
            {
                PlayerPresenter targetPlayer = collider.GetComponent<PlayerPresenter>();
                if (targetPlayer != null && targetPlayer.playerModel.GetPlayerAliveState() == PlayerLivingState.Alive)
                {
                    if (targetPlayer.playerModel.GetPlayerJob() != PlayerJob.Animal)
                    {
                        Debug.Log("Animal이 아니어서 못 죽임");
                        return;
                    }
                    // 대상 플레이어를 죽임
                    //죽이면 이펙트 
                    //Assets/Resources/Prefabs/FX_PF_Electricity_AreaExplosion_Blue.prefab
                    GameObject effect = Resources.Load<GameObject>("Prefabs/FX_PF_Electricity_AreaExplosion_Blue");
                    if (IsOwner)
                    {
                        Instantiate(effect,transform.position,Quaternion.identity);    
                        ulong senderClientId = rpcParams.Receive.SenderClientId;
                        ClientRpcParams clientRpcParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { senderClientId }
                            }
                        };
                        
                        playerView.PlaySFXClientRpc(PlayerSFX.playerKillSFX, clientRpcParams);
                    }
                    targetPlayer.HandlePlayerDeathServerRpc();
                    Debug.Log($"[Server] Player {OwnerClientId} killed Player {targetPlayer.OwnerClientId}");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"[Server] No valid target found for Player {OwnerClientId}");
    }
    
    
    
    //NOTE: STRATEGY관련된 건 다 밖으로 빼고, MODEL / VIEW 데이터를 겟하는 용도만 가져오기
    
    /// <summary>
    /// 상호작용 시도 서버 RPC
    /// </summary>
    [ServerRpc]
    public void TryInteractServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // 서버 검증
        DebugUtils.AssertNotNull(roleController, "RoleManager", this);
        
        if (roleController.CurrentStrategy?.CanInteract() != true)
        {
            Debug.LogWarning($"[Server] Player {OwnerClientId} cannot interact");
            return;
        }
        
        // 상호작용 범위 내의 오브젝트 찾기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Player))
            {
                continue;
            }

            if (collider.CompareTag(GameTags.Vent))
            {
                PlayerJob currentRole = playerModel.GetPlayerJob();
                if (currentRole != PlayerJob.Farmer)
                {
                    Debug.LogWarning($"[Server] Only Farmer can use vents. Current role: {currentRole}");
                    return;
                }
                //오너클라이언트가 벤트 타기
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { OwnerClientId }
                    }
                };

                TriggerVentClientRpc(clientRpcParams);
                return;
            }

            // IInteractable 인터페이스 활용
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                interactable.Interact(gameObject);
                Debug.Log($"[Server] Player {OwnerClientId} interacted with {collider.name}");
                return;
            }
            
            
            if (collider.CompareTag(GameTags.Exit))
            {
                HandleExitInteraction(collider);
                return;
            }
            
            if (collider.CompareTag(GameTags.Teleport))
            {
                HandleTeleportInteraction(collider);
                return;
            }
            if (collider.CompareTag(GameTags.RareCardShop))
            {
                HandleRareCardShopInteraction(collider);
                return;
            }
            if (collider.CompareTag(GameTags.MiniGame))
            {
                HandleMiniGameInteraction(collider);
                return;
            }

            if (collider.CompareTag(GameTags.ConvocationOfTrial))
            {
                HandleTrialConvocationInteraction(collider);
                return;
            }

            
            
        }
        
        Debug.LogWarning($"[Server] No interactable object found for Player {OwnerClientId}");
    }

    

    [ClientRpc]
    private void TriggerVentClientRpc(ClientRpcParams clientRpcParams = default){
        roleController.CurrentStrategy?.TryVent();
    }

    /// <summary>
    /// TODO: 출입구 상호작용 처리
    /// </summary>
    private void HandleExitInteraction(Collider2D exitCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used exit");
    }

    /// <summary>
    /// TODO: 텔레포트 상호작용 처리
    /// </summary>
    private void HandleTeleportInteraction(Collider2D teleportCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used teleport");
        
    }

    /// <summary>
    /// TODO: 희귀카드상점 상호작용 처리
    /// </summary>
    private void HandleRareCardShopInteraction(Collider2D shopCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used rare card shop");
    }

    /// <summary>
    /// 미니게임 상호작용 처리
    /// </summary>
    private void HandleMiniGameInteraction(Collider2D miniGameCollider)
    {
        MinigameController minigameController = miniGameCollider.GetComponent<MinigameController>();
        if (minigameController != null)
        {
            minigameController.OpenUi(); 
        }
    }

    /// <summary>
    /// 재판소집 상호작용 처리
    /// </summary>
    private void HandleTrialConvocationInteraction(Collider2D trialCollider)
    {
        Debug.Log($"[Server] Player {OwnerClientId} used trial convocation");
        
        // 기존 재판소집 로직 활용
        ConvocationOfTrialController trialController = trialCollider.GetComponent<ConvocationOfTrialController>();
        if (trialController != null)
        {
            trialController.Interact(gameObject); 
            
        }
    }
    

    #endregion

    [ServerRpc]
    public void TryTrialServerRpc(ulong reporterClientId)
    {
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }


    /// <summary>
    /// 시체 리포트 서버 RPC (검증 포함)
    /// </summary>
    /// NOTE: 민수님
    /// mvp관련된 게 아니니까(ui관련도 아니고 모델갱신도 아니니까 별도의 컴포넌트로 빼기: CHARACTER)
    /// mvp는 ui보여주기 위한거라고 생각하기
    /// VIEW도, MODEL도 안 건드리고 있음= > 따라서 여기있을 이유가 없음
    [ServerRpc]
    public void ReportCorpseServerRpc(ulong reporterClientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 1. 요청자가 실제로 이 플레이어의 소유자인지 검증
        if (OwnerClientId != requesterClientId)
        {
            ReportCorpseResultClientRpc(false, "Unauthorized request", requesterClientId);
            return;
        }
        
        // note cba0898: Assert 상황에서 코드 실행 사유 체크 필요. Assert가 아니라 일반적인 if문으로 변경하는게 맞아 보임.
        // 2. 서버에서 플레이어 상태 검증 (유령은 시체 리포트 불가)
        if (!DebugUtils.AssertNotNull(playerModel != null, "playerModel", this))
        {
            ReportCorpseResultClientRpc(false, "PlayerModel not found", requesterClientId);
            return;
        }
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            ReportCorpseResultClientRpc(false, "Ghost cannot report corpses", requesterClientId);
            return;
        }
        
        // 3. 서버에서 실제로 시체가 근처에 있는지 검증
        if (!IsCorpseNearby())
        {
            ReportCorpseResultClientRpc(false, "No corpse nearby", requesterClientId);
            return;
        }
        
        // 4. 모든 플레이어의 움직임 멈추기
        StopAllPlayerMovementServerRpc();
        
        // 5. 서버에게 처리해달라고 하기 (책임클래스는 TrialManager)
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
        
        // 6. 성공 결과 전달
        ReportCorpseResultClientRpc(true, "Corpse reported successfully", requesterClientId);
    }
    //NOTE: 민수님: hud CONTROLLER 관련된것들 여기있을 이유없음
    
    /// <summary>
    /// 시체 리포트 결과 클라이언트 RPC
    /// </summary>
    [ClientRpc]
    private void ReportCorpseResultClientRpc(bool success, string reason, ulong targetClientId)
    {
        if (IsOwner)
        {
            if (success)
            {
                // 성공 시 UI 업데이트 등
            }
            else
            {
                Debug.LogWarning($"Corpse report failed: {reason}");
            }
        }
    }
    
    /// <summary>
    /// 서버에서 시체가 근처에 있는지 검증
    /// </summary>
    private bool IsCorpseNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.PlayerCorpse))
            {
                return true;
            }
        }
        return false;
    }
    
 
    

    /// <summary>
    /// 플레이어 사망 처리 (죽은 플레이어의 presenter에서 실행)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HandlePlayerDeathServerRpc(ServerRpcParams rpcParams=default)
    {
        // 1. 서버에서 플레이어 상태 검증 (이미 죽었는지 확인)
        DebugUtils.AssertNotNull(playerModel, "playerModel", this);
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogWarning($"Server: Player {OwnerClientId} is already dead");
            return;
        }
        
        
        // 시체 프리팹 생성 (서버가 권위적 정보로 처리)
        CreateCorpseServerRpc(transform.position, playerModel.PlayerAppearanceData.Value.ColorIndex);
        
        // 죽은 플레이어에게만 유령 상태로 변경하라고 알림
        ChangeToGhostStateServerRpc();
        
        // 로컬: 유령 상태로 전환 (레이어..)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { playerModel.ClientId }
            }
        };
        ChangeToGhostStateClientRpc(clientRpcParams);
        
        //시각처리: Model의 State.OnValueChanged에서
       
    }


       


    //NOTE: 민수님
    //이거는 MODEL건드리는거니까 여기 있어야하는 거 맞음.
    /// <summary>
    /// 유령 상태로 전환 (서버전용함수)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeToGhostStateServerRpc()
    {
        DebugUtils.AssertNotNull(playerModel, "playerModel", this);

        PlayerStatusData statusData = playerModel.PlayerStatusData.Value;
        // 서버에서 속도 변경 (NetworkVariable로 동기화됨)
        statusData.moveSpeed = statusData.moveSpeed * GameConstants.Player.GhostSpeedMultiplier; // 유령 속도로 설정
        // 서버에서 job변경
        statusData.job =  PlayerJob.Ghost;
        playerModel.PlayerStatusData.Value = statusData;
        // 서버에서 상태 변경
        PlayerStateData newState = new PlayerStateData
        {
            AliveState = PlayerLivingState.Dead,
            AnimationState = playerModel.PlayerStateData.Value.AnimationState  // 기존 값 유지
        };
        playerModel.PlayerStateData.Value = newState;
        // 서버에서 태그 변경 (NetworkVariable로 동기화됨)
        playerModel.PlayerTag.Value = GameTags.PlayerGhost;
        // 서버에서 투명도 변경
        PlayerAppearanceData currentAppearanceData = playerModel.PlayerAppearanceData.Value;
        currentAppearanceData.AlphaValue = GameConstants.Player.GhostTransparency;
        playerModel.PlayerAppearanceData.Value = currentAppearanceData;
    }

    /// <summary>
    /// 로컬: 유령 상태로 전환 (레이어..)
    /// </summary>
    [ClientRpc]
    private void ChangeToGhostStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // 레이어 변경
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.PlayerGhost);
    }


    /// <summary>
    /// 시체 생성 (서버 RPC)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void CreateCorpseServerRpc(Vector3 position, int colorIndex)
    {
        DebugUtils.AssertNotNull(corpsePrefab, "CorpsePrefab", this);

        GameObject corpse = Instantiate(corpsePrefab, position, Quaternion.identity);
        // 시체를 네트워크에 스폰
        if (corpse.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            networkObject.Spawn();
            // NetworkVariable을 통해 색상 동기화 (모든 클라이언트에 자동 동기화)
            if (corpse.TryGetComponent<PlayerCorpse>(out PlayerCorpse playerCorpse))
            {
                PlayerAppearanceData appearanceData = new PlayerAppearanceData
                {
                    ColorIndex = colorIndex
                };
                playerCorpse.AppearanceData.Value = appearanceData;
            }
        }
        else
        {
            Debug.LogError("Corpse prefab doesn't have NetworkObject component!");
        }
        
    }


    /// <summary>
    /// 유령 UI 표시
    /// </summary>
    public void ShowGhostUI()
    {
        // 유령 전용 UI 세팅
        interactionHUDController.DisableButton(InteractionHUDController.ButtonName.CorpseReport);        
    }

    /// <summary>
    /// 다른 플레이어가 이 플레이어를 죽일 때 호출되는 메서드
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void KillPlayerServerRpc()
    {
        if (!IsServer) return;
        
        // 사망 처리
        HandlePlayerDeathServerRpc();
    }

    

    /// <summary>
    /// 모든 플레이어의 가시성 업데이트
    /// </summary>
    public void UpdateVisibilityForAllPlayers()
    {
        //죽은애의 오브젝트에서 실행되는 함수임.
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerPresenter playerPresenter =  PlayerHelperManager.Instance.GetPlayerPresenterByClientId(localClientId);
        PlayerLivingState localPlayerLivingState = playerPresenter.playerModel.GetPlayerAliveState();
        PlayerPresenter[] players= PlayerHelperManager.Instance.GetAllPlayers<PlayerPresenter>();
        
        //내가 죽었는지 체크
        if (localPlayerLivingState == PlayerLivingState.Dead)
        {
            //플레이어 다 끌어와서, 모두 보이도록 하기
            foreach (var player in players)
            {
                player.SetPlayerVisibility(true);
            }
        }
        else//내가 살았으면
        {
            //플레이어 다 끌어와서, 죽은 플레이어만 안 보이게 하기
            foreach (var player in players)
            {
                if (player.playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
                {
                    player.SetPlayerVisibility(false);    
                    Debug.Log("죽은애발견 ");
                }
                else
                {
                    player.SetPlayerVisibility(true);    
                    Debug.Log("살은애발견 ");
                }
            }
        }
    }

 

  

    /// <summary>
    /// 플레이어 가시성 설정
    /// </summary>
    private void SetPlayerVisibility(bool visible)
    {
        // 모든 렌더러 컴포넌트 활성화/비활성화
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
        
        // 닉네임 텍스트도 함께 처리
        DebugUtils.AssertNotNull(playerView, "playerView", this);
        playerView.SetNicknameVisibility(visible);

        // 콜라이더는 항상 활성화 (충돌 감지용)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }

    /// <summary>
    /// 모든 플레이어의 움직임을 멈추는 서버 RPC
    /// </summary>
    [ServerRpc]
    private void StopAllPlayerMovementServerRpc()
    {
        // 모든 플레이어의 움직임 멈추기
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            DebugUtils.AssertNotNull(player, "PlayerView", this);
            player.SetIgnoreAllPlayerMoveInputServerRpc(true);
        }
    }

    #region 외부 인터페이스 (메시지 기반)

    
    public void OnOffNickname(bool onOff)
    {
        playerView.SetNicknameVisibility(onOff);
    }
    
    /// <summary>
    /// 킬 시도 요청: 버튼전용
    /// </summary>
    public void RequestKill()
    {
        if (roleController?.CurrentStrategy != null)
        {
            roleController.CurrentStrategy.TryKill();
        }
    }
    
    /// <summary>
    /// 사보타지 시도 요청
    /// </summary>
    public void RequestSabotage()
    {
        if (roleController?.CurrentStrategy != null)
        {
            roleController.CurrentStrategy.TrySabotage();
        }
    }
    
    /// <summary>
    /// 상호작용 시도 요청: 버튼용
    /// </summary>
    public void RequestInteract()
    {
        if (roleController?.CurrentStrategy != null)
        {
            roleController.CurrentStrategy.TryInteract();
        }
    }
    
    /// <summary>
    /// 시체 리포트 요청
    /// </summary>
    public void RequestReportCorpse()
    {
        if (roleController?.CurrentStrategy != null)
        {
            roleController.CurrentStrategy.TryReportCorpse();
        }
    }

    
    #endregion
}
