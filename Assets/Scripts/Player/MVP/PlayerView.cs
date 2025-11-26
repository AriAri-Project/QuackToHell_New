using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
/// <summary>
/// 시각 및 입력 처리 (완전 Input System 방식)
/// </summary>
public class PlayerView : NetworkBehaviour
{
    public enum PlayerSFX
    {
        playerKillSFX,
        
    }

    [SerializeField] private float killCooltimeMax = 20f;
    [Header("SFX")]
    [SerializeField] private AudioSource playerKillSFX;


    // PlayerView.cs에 추가
    [Header("Player Detection")]
    [SerializeField] private float playerDetectionRadius = 2f;
    private HashSet<GameObject> previouslyDetectedPlayers = new HashSet<GameObject>();

    public Action<GameObject> onPlayerDetected;
    public Action onPlayerExited;
    private Camera localCamera = null;
    private float killCooltimer = 0f;
    private bool canKill = true;
    public bool CanKill => canKill;

    private NetworkVariable<bool> ignoreMoveInput = new NetworkVariable<bool>(false);
    public bool IgnoreMoveInput
    {
        get { return ignoreMoveInput.Value; }
        set
        {
            if (!IsHost)
            {
                return;
            }
            ignoreMoveInput.Value = value;   
        }
    }

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;

    private InteractionHUDController interactionHUDController;

    [ClientRpc]
    public void PlaySFXClientRpc(PlayerSFX sfx, ClientRpcParams rpcParams = default )
    {
        switch (sfx)
        {
            case PlayerSFX.playerKillSFX:
                SoundManager.Instance.SFXPlay(playerKillSFX.name,playerKillSFX.clip);
                break;
        }
        
    }
    protected void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }
    private void Update() {
        #region 플레이어감지
        // 소유자만 감지 실행
        if (!IsOwner) return;
        
        
        DetectNearbyPlayers();
        #endregion
        killCooltimer+= Time.deltaTime;
        if(killCooltimer >= killCooltimeMax)
        {
            killCooltimer = 0f;
            canKill = true;
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
    private void DetectNearbyPlayers()
    {
        // 현재 감지된 플레이어들
        HashSet<GameObject> currentlyDetectedPlayers = new HashSet<GameObject>();
        
        // 범위 내 모든 콜라이더 검색
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, playerDetectionRadius);
        
        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.CompareTag(GameTags.Player) && collider.gameObject != gameObject)
            {
                GameObject detectedPlayer = collider.gameObject;
                currentlyDetectedPlayers.Add(detectedPlayer);
                
                // 새로 감지된 플레이어 (Enter 이벤트)
                if (!previouslyDetectedPlayers.Contains(detectedPlayer))
                {
                    onPlayerDetected?.Invoke(detectedPlayer);
                }
            }
        }
        
        // Exit 이벤트 처리 (필요한 경우)
        foreach (GameObject previousPlayer in previouslyDetectedPlayers)
        {
            if (!currentlyDetectedPlayers.Contains(previousPlayer))
            {
                onPlayerExited?.Invoke(); // 필요시 추가
            }
        }
        
        // 이전 상태 업데이트
        previouslyDetectedPlayers = currentlyDetectedPlayers;
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

        InputAction reportAction = playerInput.actions[$"{GameInputs.ActionMaps.Player}/{GameInputs.Actions.Report}"];
        DebugUtils.AssertNotNull(reportAction, "ReportAction", this);
        reportAction.performed += OnReportInput;

        InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
        DebugUtils.AssertNotNull(killAction, "KillAction", this);
        killAction.performed += OnKillInput;
        // 사보타지 입력 추가 (e키)
        InputAction savotageAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Savotage}"];
        DebugUtils.AssertNotNull(savotageAction, "SavotageAction", this);
        savotageAction.performed += OnSavotageInput;

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
                reportAction.performed -= OnReportInput;
            }

            InputAction killAction = playerInput.actions[$"{GameInputs.ActionMaps.Farmer}/{GameInputs.Actions.Kill}"];
            if (DebugUtils.AssertNotNull(killAction, "KillAction", this))
            {
                killAction.performed -= OnKillInput;
            }
        }
        
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOwner)
        {
            // 씬 변경 시 카메라 초기화
            if (localCamera != null)
            {
                Destroy(localCamera.gameObject);
                localCamera = null;
                Debug.Log("[PlayerView] OnSceneLoaded - localCamera is null");
            }
            SetupLocalCamera();
        }
        if(scene.name == GameScenes.Village && IsOwner)
        {
            SetIgnoreAllPlayerMoveInputServerRpc(false);
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

    // Input System을 사용한 이동 입력 처리
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (ignoreMoveInput.Value) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        
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
                player.IgnoreMoveInput = value;
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
                spriteRenderer.color = ColorUtils.GetColorByIndex(colorIndex);
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

        if (!canKill)
        {
            Debug.Log($"쿨타임때문에 죽일 수 x. 남은 쿨타임: {killCooltimeMax-killCooltimer}");
            return;
        }
        OnKillTryInput?.Invoke();
        canKill = false;
        
    }

    
    #endregion
    public Action OnVentTryInput;
    public Action OnSavotageTryInput;


    private void OnSavotageInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        OnSavotageTryInput?.Invoke();
    }

    #region Interact (Input System) - 모든 사람 가능 (Ghost 제외)
    public Action OnInteractInput;
    
    // Input System을 사용한 Interact 입력 처리
    private void OnInteractInputHandler(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        OnInteractInput?.Invoke();
    }

   
    #endregion

    #region 시체발견 (LeftShift키) - 모든 사람 가능 (Ghost 제외)
    public Action<ulong> OnCorpseReported;

    

    // LeftShift키로 시체 리포트 처리
    private void OnReportInput(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        
        OnCorpseReported?.Invoke(OwnerClientId);
        
        
    }
    public void InjectHUDController(InteractionHUDController interactionHUDController)
    {
        this.interactionHUDController = interactionHUDController;
    }

    public Action<Collider2D> OnObjectEntered;
    public Action<Collider2D> OnObjectExited;

    //note: 민수님 /이것도 view가 가지면 어색함
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
    
        OnObjectEntered?.Invoke(collision); 
    }
    //note: 민수님 /이것도 view가 가지면 어색함
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;
    
        OnObjectExited?.Invoke(collision); 
    }
    #endregion
}
