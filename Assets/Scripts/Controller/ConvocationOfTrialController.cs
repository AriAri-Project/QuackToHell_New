
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// 재판 소집 상호작용 Controller (IInteractable 패턴 적용)
/// </summary>
public class ConvocationOfTrialController : InteractionControllerBase
{
    
    private GameObject reporter;
    private bool canInteract = false;
    protected override void Awake()
    {
        base.Awake(); // InteractionControllerBase의 Awake 호출
    }
    
   
    #region IInteractable 구현
    
    /// <summary>
    /// 상호작용 가능 여부 확인
    /// </summary>
    public override bool CanInteract(GameObject player)
    {
        if ( player == null) return false;
        if (!canInteract) return false;
        
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
            canInteract = true;
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(GameTags.Player))
        {
            reporter = null;
            canInteract = false;
        }
    }
    
    #endregion
}