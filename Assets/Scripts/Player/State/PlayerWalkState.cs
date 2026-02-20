using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWalkState : NetworkStateBase
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer head;
    [SerializeField] private SpriteRenderer body;
    [Header("SFX")]
    public AudioSource walkSFX;
    
    private NetworkVariable<bool> headFlipX = new NetworkVariable<bool>();

    private PlayerModel playerModel;
    private void Start()
    {
        // NetworkVariable 값 변경 이벤트 구독
        headFlipX.OnValueChanged += OnHeadFlipChanged;
        // 초기 값 적용
        OnHeadFlipChanged(false, headFlipX.Value);
        // 모델 가져오기
        playerModel = GetComponent<PlayerModel>();
    }



    private void OnHeadFlipChanged(bool previousValue, bool newValue)
    {
        // 모든 클라이언트에서 머리 플립 적용
        if (body != null)
        {
            body.flipX = newValue;
        }
    }

    public override void OnStateEnter()
    {
        head.gameObject.SetActive(false);
        TriggerWalkAnimation();
        if (playerModel == null)
        {
            playerModel = GetComponent<PlayerModel>();
        }
        //죽으면 walk사운드x
        if (playerModel.PlayerStateData.Value.IsDead)
        {
            return;
        }
        
        walkSFX.loop = true;
        
        if (walkSFX.clip != null)
        {
            float volume = SoundVolumeSettings.Instance.GetVolume(walkSFX.clip);
            walkSFX.volume = volume;
        }
        
        if (!walkSFX.isPlaying)
        {
            walkSFX.Play();
        }
    }

    // 트리거 방식으로 애니메이션 제어
    public void TriggerWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            Debug.LogError("PlayerWalkState: Animator not found! Please assign in Inspector.");
        }
    }

    public override void OnStateExit()
    {
        if (playerModel == null)
        {
            playerModel = GetComponent<PlayerModel>();
        }
        
        //죽으면 walk사운드x
        if (playerModel.PlayerStateData.Value.IsDead)
        {
            return;
        }

        walkSFX.loop = false;
    }

    public override void OnStateUpdate()
    {
        if(!GetComponent<NetworkObject>().IsOwner) return;
        
        if (playerModel == null)
        {
            playerModel = GetComponent<PlayerModel>();
        }
        
        Vector2 moveDirection = playerModel.MoveDirection;
        if (moveDirection.x < 0)
        {
            FlipHeadServerRpc(true);
        }
        else if(moveDirection.x > 0)
        {
            FlipHeadServerRpc(false);
        }
    }

    [ServerRpc]
    private void FlipHeadServerRpc(bool flip)
    {
        // 서버에서 머리 플립 상태 변경
        headFlipX.Value = flip;
    }

    public override void OnDestroy()
    {
        // 이벤트 구독 해제
        if (headFlipX != null)
        {
            headFlipX.OnValueChanged -= OnHeadFlipChanged;
        }

        base.OnDestroy();
    }
}
