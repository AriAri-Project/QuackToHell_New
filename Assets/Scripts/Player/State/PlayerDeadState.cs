using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.InputSystem;
public class PlayerDeadState : NetworkStateBase
{
    [SerializeField] private GameObject head;
    [SerializeField] private Animator animator;

    [SerializeField]
    private PlayerModel playerModel;
    
    
    public override void OnStateEnter()
    {
        head.SetActive(false);
        //애니메이션 on
        TriggerWalkAnimation();
        
        
        //모든플레이어의 가시성 업뎃
        UpdateVisibilityForAllPlayers();

    }
    
    /// <summary>
    /// 모든 플레이어의 가시성 업데이트
    /// </summary>
    public void UpdateVisibilityForAllPlayers()
    {
        //죽은애의 오브젝트에서 실행되는 함수임.
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerModel playerModel =  PlayerHelperManager.Instance.GetPlayerModelByClientId(localClientId);
        PlayerLivingState localPlayerLivingState = playerModel.GetPlayerAliveState();
        PlayerView[] players= PlayerHelperManager.Instance.GetAllPlayers<PlayerView>();
        
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
                PlayerModel targetPlayerModel = player.GetComponent<PlayerModel>();
                if (targetPlayerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
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
    

    
    // 트리거 방식으로 애니메이션 제어
    public void TriggerWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }
        else
        {
            Debug.LogError("PlayerWalkState: Animator not found! Please assign in Inspector.");
        }
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }

    [ClientRpc]
    public void KilledClientRpc(FixedString128Bytes skillPrefabPath, ClientRpcParams rpcParams = default)
    {
        //죽으면 이펙트 
        GameObject effect = Resources.Load<GameObject>(skillPrefabPath.Value);
        if (IsOwner)
        {
            Instantiate(effect,transform.position,Quaternion.identity);
            AudioClip clip = Resources.Load<AudioClip>("Audio/Die");
            Debug.Log("Die 재생됨");
            SoundManager.Instance.SFXPlay(clip.name, clip);
        }
    }
}
