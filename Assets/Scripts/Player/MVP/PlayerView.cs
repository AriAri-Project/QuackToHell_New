using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
/// <summary>
/// *MVP를 잘 몰랐을 떄 썼으므로 아래로 의미를 재정의함
/// 플레이어에 활용된 MVP의 의미는 기존 UI용 MVP와 조금 다름:
/// <PlayerModel>           <PlayerPresenter>       <PlayerView>
/// [입력들온거로로직처리] <-   [Model/적절한 곳에 전달]   <-    [TriggerEnter, 키입력 감지]
/// [데이터변화]          ->   [View/적절한 곳에 전달]    ->    [변화한 데이터를 외형에 반영]
/// </summary>
public class PlayerView : NetworkBehaviour
{

    [Header("For Trigger Detect Colliders")]
    [Tooltip("this is for hide player behind shadow.")] 
    [SerializeField] private Collider2D forShadowCollider;
    [Tooltip("this is for interact.")] 
    [SerializeField] private Collider2D forInteractCollider;
    
    public Action<GameObject> onPlayerDetected;
    public Action<GameObject> onPlayerExited;
    public Action<GameObject> onCorpseDetected;
    public Action<GameObject> onCorpseExited;
    public Action<GameObject> OnObjectEntered;
    public Action<GameObject> OnObjectExited;
    
    private Camera localCamera = null;

    List<GameObject> overlappingAliveAnimalPlayers = new List<GameObject>();
    List<GameObject> overlappingPlayers = new List<GameObject>();

    public List<GameObject> OverlappingPlayers
    {
        get { return overlappingPlayers; }
    }
    
    private GameObject killTargetPlayerCache = null;

    public GameObject TargetPlayerCache
    {
        get { return GetClosestPlayer(); }
    }
    
    private GameObject targetCorpseCache = null;
    

    public GameObject TargetCorpseCache
    {
        get
        {
            if (targetCorpseCache == null)
            {
                return null;
                
            }
            ShadowHider shadowHider = GetComponentInChildren<ShadowHider>();
            if (shadowHider != null && shadowHider.IsTargetHiddenByShadow(targetCorpseCache))
            {
                return null;
            }
            return targetCorpseCache;
        }
    }
    
    private GameObject interactObjCache = null;

    public GameObject InteractObjCache
    {
        get { return interactObjCache; }
    }

    private NetworkVariable<bool> ignoreMoveInput = new NetworkVariable<bool>(false);

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;


    private const float INTERACT_COOLDOWN_MAX = 0.5f;
    private float interactCooldownTimer = 0f;
    private bool canInteract=false;

    protected void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }

    
    //note: 민수님
    //view가 하는 게 아님. 
    //view는 텍스트  / 이미지 / UI 갱신..
    //view는 보여줄 것에 관련된것. 
    //view에는 버튼/ 이미지, UI, .. 모델에서 변화 일어나면 프레젠터가 받아서 view가 갱신.
    //view가 입력감지하면 presenter가 받아서 처리. 
    //model은 데이터만 관련(로직 다 넣는 게 아님)
    //모델에 로직 때려넣고싶으면, partial로 분리하기. (관리 용이 하니까)
    //partial로 분리한다면, 
    //>>별도의 컴포넌트 파는 거 추천<<하시나, partial로 분리한다면: detect, character, ...
    //보통은 하나에 때려넣는거보다는 컴포넌트로 쪼개서 재활용하는게 좋음. 
    //MVP는 UI쓸 떄 많이쓰는거임. 다른곳에도 우겨넣을필요x
    //보통은 클래스를 쪼개서 재활용을 높이는 게 베스트.
    //첨에 만들 땐 불편한데 유지보수가 편리(트레이드오프)
    
    //note: 민수님 /이것도 view가 가지면 어색함
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        
        
        GameObject detectedObject = collision.gameObject;

        //for shadow collider는 플레이어 및 시체만 감지되도록 인스펙터 설정되어있으므로 코드에서 태그비교x
        if (forShadowCollider.IsTouching(collision))
        {
            //중복방지
            if (!overlappingPlayers.Contains(detectedObject))
            {
                overlappingPlayers.Add(detectedObject);
            }
        }

        //다 감지되도록 인스펙터에서 설정되어있으므로 코드에서 태그비교x
        if (forInteractCollider.IsTouching(collision))
        {
            if (collision.CompareTag(GameTags.Player))
            {
                if (!detectedObject.Equals(this.gameObject))
                {
                    // 직업이 Animal이고 Alive인 플레이어만 추가
                    PlayerModel detectedPlayerModel = detectedObject.GetComponent<PlayerModel>();
                    if (detectedPlayerModel == null) return;
    
                    if (detectedPlayerModel.GetPlayerCurrentJob() != PlayerJob.Animal || 
                        detectedPlayerModel.GetPlayerAliveState() != PlayerLivingState.Alive)
                    {
                        return; // Animal이 아니거나 죽은 플레이어는 추가하지 않음
                    }

                    if (!overlappingAliveAnimalPlayers.Contains(detectedObject))
                    {
                        overlappingAliveAnimalPlayers?.Add(detectedObject);
                        onPlayerDetected?.Invoke(detectedObject);
                    }
                
                }
            }
            else if (collision.CompareTag(GameTags.PlayerCorpse))
            {
                targetCorpseCache =  collision.gameObject;
                onCorpseDetected?.Invoke(targetCorpseCache);
            }
            else if(collision.CompareTag(GameTags.MiniGame)||collision.CompareTag(GameTags.Vent)||collision.CompareTag(GameTags.ConvocationOfTrial)||collision.CompareTag(GameTags.Closet)||collision.CompareTag(GameTags.Keyboard))
            {
                interactObjCache = collision.gameObject;
                OnObjectEntered?.Invoke(detectedObject);
            }
        }
        
        
        
        
        
    }
    
    //note: 민수님 /이것도 view가 가지면 어색함
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;
        GameObject detectedObject = collision.gameObject;
        
        //for shadow collider는 플레이어 및 시체만 감지되도록 인스펙터 설정되어있으므로 코드에서 태그비교x
        if (!forShadowCollider.IsTouching(collision))
        {
            //중복방지
            if (overlappingPlayers.Contains(detectedObject))
            {
                overlappingPlayers?.Remove(detectedObject);
            }
        }
        
        if (collision.CompareTag(GameTags.Player))
        {
            if (detectedObject.Equals(this.gameObject))
            {
                return;
            }
        

            // 직업이 Animal이고 Alive인 플레이어만 제거
            if (overlappingAliveAnimalPlayers.Contains(detectedObject))
            {
                overlappingAliveAnimalPlayers?.Remove(detectedObject);
                onPlayerExited?.Invoke(detectedObject);
            }
            
        }
        else if (collision.CompareTag(GameTags.PlayerCorpse))
        {
            targetCorpseCache =  null;
            onCorpseExited?.Invoke(targetCorpseCache);
        }
        else if(collision.CompareTag(GameTags.MiniGame)||collision.CompareTag(GameTags.Vent)||collision.CompareTag(GameTags.ConvocationOfTrial)||collision.CompareTag(GameTags.Closet)||collision.CompareTag(GameTags.Keyboard))
        {
            interactObjCache = null;
            OnObjectExited?.Invoke(detectedObject);
        }
        
        
    }

    private GameObject GetClosestPlayer()
    {
        if (overlappingAliveAnimalPlayers?.Count == 0)
        {
            return null;
        }
        
        GameObject closestPlayer = null;
        float minDistance = float.MaxValue;
        
        // 현재 플레이어가 Farmer인지 확인
        PlayerModel myModel = GetComponent<PlayerModel>();
        ShadowHider myShadowHider = GetComponentInChildren<ShadowHider>();
        
        foreach(GameObject player in overlappingAliveAnimalPlayers)
        {
            if (player == null) continue;
        
            PlayerModel playerModel = player.GetComponent<PlayerModel>();
            if (playerModel == null) continue;
        
            // Animal이고 Alive인 플레이어만 고려
            if (playerModel.GetPlayerCurrentJob() != PlayerJob.Animal || 
                playerModel.GetPlayerAliveState() != PlayerLivingState.Alive)
            {
                continue;
            }
            
            // Farmer인 경우: 벽에 가려진 플레이어는 제외
            if (myModel != null && myModel.GetPlayerCurrentJob() == PlayerJob.Farmer && myShadowHider != null)
            {
                if (myShadowHider.IsTargetHiddenByShadow(player))
                {
                    continue; // 벽에 가려진 플레이어는 스킵
                }
            }
            
            //나와 상대방 간 거리
            float distance = Vector3.Distance(player.transform.position, transform.position);
            //최소거리찾기
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPlayer = player;
            }
        }
        return closestPlayer;
    }


    public void RemoveDeadPlayerFromOverlappingPlayers(GameObject player)
    {
        //기록용: 상태체크하여 remove하니 성공(그냥 gameobject바로 넣어서 remove하니 실패했었음)
        if (overlappingAliveAnimalPlayers == null) return;
  
        for (int i = overlappingAliveAnimalPlayers.Count - 1; i >= 0; i--)
        {
            GameObject p = overlappingAliveAnimalPlayers[i];
            if (p == null) continue;
        
            PlayerModel model = p.GetComponent<PlayerModel>();
            if (model == null) continue;
        
            if (p == player || 
                model.GetPlayerAliveState() != PlayerLivingState.Alive ||
                model.GetPlayerCurrentJob() != PlayerJob.Animal)
            {
                overlappingAliveAnimalPlayers.RemoveAt(i);
            }
        }
        
        // OverlappingPlayers에서도 제거
        if (overlappingPlayers != null && overlappingPlayers.Contains(player))
        {
            overlappingPlayers.Remove(player);
        }
    }

    
    protected void Start()
    {
        Canvas canvas = gameObject.GetComponentInChildren<Canvas>();
        DebugUtils.AssertNotNull(canvas, "Canvas", this);
        nicknameText = canvas.GetComponentInChildren<TextMeshProUGUI>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (IsOwner)
        {
            SetupLocalCamera();
        }
        
        SetupInputSystem();
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
    }
    

    private void SetupInputSystem()
    {
        DebugUtils.AssertNotNull(playerInput, "PlayerInput", this);
        DebugUtils.AssertNotNull(playerInput.actions, "PlayerInput.actions", this);

        InputAction moveAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Move}"];
        DebugUtils.AssertNotNull(moveAction, "MoveAction", this);
        moveAction.performed += OnMoveInput;
        moveAction.canceled += OnMoveInput;

        InputAction interactAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Interact}"];
        DebugUtils.AssertNotNull(interactAction, "InteractAction", this);
        interactAction.performed += OnInteractInputHandler;

        InputAction corpseReportAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Report}"];
        DebugUtils.AssertNotNull(corpseReportAction, "ReportAction", this);
        corpseReportAction.performed += OnCorpseReportInput;

        InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
        DebugUtils.AssertNotNull(killAction, "KillAction", this);
        killAction.performed += OnKillInput;
        // 사보타지 입력 추가 (e키)
        InputAction savotageAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Savotage}"];
        DebugUtils.AssertNotNull(savotageAction, "SavotageAction", this);
        savotageAction.performed += OnSavotageInput;
        
        //close 입력 추가(esc키)
        InputAction escAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Close}"];
        DebugUtils.AssertNotNull(escAction, "EscapeAction", this);
        escAction.performed += OnESCInput;

    }
    
    public override void OnDestroy()
    {
        // note cba0898: OnDestroy() 시점에는 다른 오브젝트가 삭제되었을 수 있으므로, AssertNotNull 말고 if문으로 처리하시는 것을 추천드립니다.
        // ex) if (playerInput != null && playerInput.actions != null) { ... }
        // assert: 절대 일어나면 안 되는 상황 감지용.
        // ondestroy에서 if쓰는 이유는, 있을수도없을수도있는데 있으면 일케 처리하겠다.
        if (DebugUtils.AssertNotNull(playerInput, "PlayerInput", this) && 
            DebugUtils.AssertNotNull(playerInput.actions, "PlayerInput.actions", this))
        {
            InputAction moveAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Move}"];
            if (DebugUtils.AssertNotNull(moveAction, "MoveAction", this))
            {
                moveAction.performed -= OnMoveInput;
                moveAction.canceled -= OnMoveInput;
            }

            InputAction interactAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Interact}"];
            if (DebugUtils.AssertNotNull(interactAction, "InteractAction", this))
            {
                interactAction.performed -= OnInteractInputHandler;
            }

            InputAction reportAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Report}"];
            if (DebugUtils.AssertNotNull(reportAction, "ReportAction", this))
            {
                reportAction.performed -= OnCorpseReportInput;
            }

            InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
            if (DebugUtils.AssertNotNull(killAction, "KillAction", this))
            {
                killAction.performed -= OnKillInput;
            }
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            if (scene.name == "ResultScene")
                return;

            // 씬 변경 시 카메라 초기화
            if (localCamera != null)
            {
                Destroy(localCamera.gameObject);
                localCamera = null;
                Debug.Log("[PlayerView] OnSceneLoaded - localCamera is null");
            }
            //재판장 씬에서는 해당 방식으로 카메라가 변경되면 안 되기 때문에 막음. 추후 switch문으로 확장할 것.
            if (!(SceneManager.GetActiveScene().name == "CourtScene"))
            {
                SetupLocalCamera();
            }
        }
    }
    
    #region 카메라
    private void SetupLocalCamera()
    {
        // Owner인 플레이어만 카메라 설정
        if (!IsOwner) return;
        
        if (localCamera == null)
        {
            // Main Camera를 찾아서 플레이어에 붙이기
            GameObject mainCameraObj = Camera.main.gameObject;
            if (mainCameraObj != null)
            {
                localCamera = mainCameraObj.GetComponent<Camera>();
                mainCameraObj.transform.SetParent(transform);
                mainCameraObj.transform.localPosition = new Vector3(0, 0, -10);
                mainCameraObj.layer = LayerMask.NameToLayer("Player");
                mainCameraObj.SetActive(true);
                localCamera.enabled = true;
            }
        }
        //씬 내에서 Canvas인 오브젝트 모두 찾아서, 렌더모드가 Camera라면 내 카메라 넣어주기
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            canvas.worldCamera = localCamera;
        }
        
        //월드 마우스 상호작용용 스크립트 부착
        localCamera.gameObject.AddComponent<WorldMouseInteractionController>();
    }
    #endregion

    #region 닉네임
    [SerializeField]
    private TextMeshProUGUI nicknameText;
    
    public void UpdateNickname(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }

    /// <summary>
    /// 닉네임 가시성 설정
    /// </summary>
    public void SetNicknameVisibility(bool visible)
    {
        if (nicknameText != null)
        {
            nicknameText.enabled = visible;
        }
    }
    
    #endregion

    #region 움직임 (Input System)
    public EventHandler OnMovementInput;

    public class OnMovementInputEventArgs: EventArgs{
        public int XDirection { get; private set; }
        public int YDirection { get; private set; }

        public OnMovementInputEventArgs(int inputXDirection, int inputYDirection)
        {
            XDirection = inputXDirection;
            YDirection = inputYDirection;
        }
    }
    // 마지막 입력 방향 저장
    private Vector2 lastMoveInput = Vector2.zero;

    // Input System을 사용한 이동 입력 처리
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        
        Vector2 moveInput = context.ReadValue<Vector2>();
        ProcessMoveInput(moveInput);
        
    }

    public void ProcessMoveInput(Vector2 moveInput)
    {
        if (!IsOwner) return;
        
        if (ignoreMoveInput.Value)
        {
            
            OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(0, 0));
            return;
        }
        
        lastMoveInput = moveInput;
        // 정수로 변환하여 전송
        int xDirection = Mathf.RoundToInt(moveInput.x);
        int yDirection = Mathf.RoundToInt(moveInput.y);
        
    
        OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(xDirection, yDirection));
    }
    #endregion
    
    #region 움직임 제한
    [ServerRpc(RequireOwnership = false)]
    public void SetIgnoreAllPlayerMoveInputServerRpc(bool value)
    {
        //1. 모든 플레이어 객체 가져오기
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            if (player != null)
            {
                player.ignoreMoveInput.Value = value;
            }
        }
    }

    //로컬 플레이어 움직임 제한
    [ServerRpc]
    public void SetIgnorePlayerMoveInputServerRpc(bool  value, ServerRpcParams rpcParams = default)
    {
        //yield return new WaitForSeconds(time);
        ulong senderClientID=  rpcParams.Receive.SenderClientId;
        PlayerView playerView =  PlayerHelperManager.Instance.GetPlayerViewlByClientId(senderClientID);
        playerView.ignoreMoveInput.Value = value;
        
        if (value)
        {
            PlayerModel playerModel = playerView.GetComponent<PlayerModel>();
            if (playerModel != null)
            {
                playerModel.StopMovementServerRpc();
                playerModel.MovePlayerServerRpc(0,0);
            }
            
        }
    }

    
    
    #endregion

    /// <summary>
    /// Appreance 데이터가 바뀌면 그에 맞게 모습이 바뀜
    /// </summary>
    public void ChangeApprearence(PlayerAppearanceData playerAppearanceData)
    {
        int colorIndex = playerAppearanceData.ColorIndex;
        float alphaValue = playerAppearanceData.AlphaValue;
        
        //색 바꾸기
        SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer.gameObject.name.Contains("Body"))
            {
                spriteRenderer.color = AppearanceUtils.GetColorByIndex(colorIndex);
            }

            Color color = spriteRenderer.color;
            color.a = alphaValue;
            spriteRenderer.color =  color;
        }
    }

    #region Kill (Input System) - Farmer만 가능
    public Action OnKillTryInput;

    // Input System을 사용한 Kill 입력 처리
    private void OnKillInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        //village scene아니면 리턴
        if (SceneManager.GetActiveScene().name != GameScenes.Village)
        {
            return;
        }
        
        OnKillTryInput?.Invoke();
    }

    
    #endregion
    public Action OnSavotageTryInput;

    private void OnSavotageInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;        
        //village scene아니면 리턴
        if (SceneManager.GetActiveScene().name != GameScenes.Village)
        {
            return;
        }
        
        OnSavotageTryInput?.Invoke();
    }

    private void OnESCInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        //팝업창이 존재할 시 최상단 팝업창 닫기
        if (UIManager.Instance.PopupStack.Count > 0)
        {
            UIManager.Instance.ClosePopupUI();
        }
    }

    #region Interact (Input System) - 모든 사람 가능 (Ghost 제외)
    public Action OnInteractInput;
    
    // Input System을 사용한 Interact 입력 처리
    private void OnInteractInputHandler(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!canInteract) return;

        interactCooldownTimer = 0f;
        canInteract = false;
        
        OnInteractInput?.Invoke();
    }

   
    #endregion

    #region 시체발견 (LeftShift키) - 모든 사람 가능 (Ghost 제외)
    public Action<ulong> OnCorpseReported;
    
    // LeftShift키로 시체 리포트 처리
    private void OnCorpseReportInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        //village scene아니면 리턴
        if (SceneManager.GetActiveScene().name != GameScenes.Village)
        {
            return;
        }
        
        ulong targetCorpseId = targetCorpseCache.GetComponent<PlayerCorpse>().ClientId;
        OnCorpseReported?.Invoke(targetCorpseId);
    }
    
    /// <summary>
    /// 플레이어 가시성 설정
    /// </summary>
    public void SetPlayerVisibility(bool visible)
    {
        // 모든 렌더러 컴포넌트 활성화/비활성화
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        // 닉네임 텍스트도 함께 처리
        SetNicknameVisibility(visible);

        // 콜라이더는 항상 활성화 (충돌 감지용)
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }
    }
    
    #endregion
    
    
    /// <summary>
    /// 플레이어 오브젝트 파괴 전에 카메라를 분리
    /// </summary>
    public void DetachCamera()
    {
        if (localCamera != null)
        {
            // 카메라를 플레이어에서 분리
            localCamera.transform.SetParent(null);
        
            // 카메라 위치 초기화
            localCamera.transform.position = new Vector3(0, 0, -10);
            localCamera.gameObject.SetActive(true);
            localCamera.enabled = true;
        
            localCamera = null;
        }
    }
    
    //초기화 함수
    public void Initialize()
    {
        Debug.Log("캐시 초기화함!");
        if (IsServer)
        {
            ignoreMoveInput.Value = false;
        }
        

        interactObjCache = null;
        targetCorpseCache = null;
        
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.Player);
    
        SetPlayerVisibility(true);
    
        overlappingPlayers.Clear();
        overlappingAliveAnimalPlayers.Clear();
    
    }
}
