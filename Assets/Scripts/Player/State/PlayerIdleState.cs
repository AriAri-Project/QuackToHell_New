using UnityEngine;

/// <summary>
/// Idle상태일 때 하는 행동 정의
/// </summary>
public class PlayerIdleState : NetworkStateBase
{
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    [Header("References")]
    [SerializeField] private Animator animator;

    [SerializeField] private GameObject head;
    public override void OnStateEnter()
    {
        TriggerIdleAnimation();
        head.SetActive(false);
    }
    
    // 트리거 방식으로 애니메이션 제어
    public void TriggerIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(IsWalking, false);
        }
        else
        {
            Debug.LogError("PlayerIdleState: Animator not found! Please assign in Inspector.");
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {

    }
}
