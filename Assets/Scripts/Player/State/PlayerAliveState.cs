using System;
using UnityEngine;

public class PlayerAliveState : NetworkStateBase
{
    [SerializeField] private Animator animator;


    public override void OnStateEnter()
    {
        var controller = Resources.Load<RuntimeAnimatorController>("Animations/Player");
        animator.runtimeAnimatorController = controller;
        
        // 죽음/기타 상태 플래그 리셋
        animator.SetBool("IsDead", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsVent", false);
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }

   
}
