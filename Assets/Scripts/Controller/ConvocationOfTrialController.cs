
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 재판 소집 상호작용 Controller (IInteractable 패턴 적용)
/// </summary>
public class ConvocationOfTrialController : InteractionControllerBase
{
    [Header("Settings")]
    [SerializeField] private float detectionRadius = 1.0f;
    
    private bool activateKey = false;
    private GameObject reporter;
    
    protected override void Awake()
    {
        base.Awake(); // InteractionControllerBase의 Awake 호출
    }
    
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 씬이 이미 로드된 상태라면 수동으로 호출
        if (SceneManager.GetActiveScene().name.Equals(GameScenes.Village))
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || gameObject == null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        if (scene.name.Equals(GameScenes.Village))
        {
            // 콜라이더 설정 (InteractionControllerBase에서 이미 isTrigger = true 설정됨)
            Collider2D collider = GetComponent<Collider2D>();
            if (collider is CircleCollider2D circleCollider)
            {
                circleCollider.radius = detectionRadius;
            }
            
            activateKey = true;
        }
    }

    #region IInteractable 구현
    
    /// <summary>
    /// 상호작용 가능 여부 확인
    /// </summary>
    public override bool CanInteract(GameObject player)
    {
        if (!activateKey || player == null) return false;
        
        // 거리 체크
        float distance = Vector3.Distance(player.transform.position, transform.position);
        if (distance > detectionRadius) return false;
        
        // 살아있는 플레이어만 재판 소집 가능
        PlayerModel playerModel = player.GetComponent<PlayerModel>();
        if (playerModel != null)
        {
            return playerModel.GetPlayerAliveState() == PlayerLivingState.Alive;
        }
        
        return true;
    }

    /// <summary>
    /// 재판 소집 상호작용 실행
    /// </summary>
    public override void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;
        
        PlayerPresenter playerPresenter = player.GetComponent<PlayerPresenter>();
        if (playerPresenter != null)
        {
            ulong reporterClientId = playerPresenter.OwnerClientId;
            
            // TrialManager를 통해 재판 시작
            if (TrialManager.Instance != null)
            {
                TrialManager.Instance?.TryTrialServerRpc(reporterClientId);
                Debug.Log($"[ConvocationOfTrialController] Player {reporterClientId} started trial convocation");
            }
            else
            {
                Debug.LogError("[ConvocationOfTrialController] TrialManager.Instance is null");
            }
        }
    }
    
    #endregion

    #region 트리거 
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameTags.Player))
        {
            reporter = collision.gameObject;
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(GameTags.Player))
        {
            reporter = null;
        }
    }
    
    #endregion
}