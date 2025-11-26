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
    
    private void Start()
    {
        // NetworkVariable 값 변경 이벤트 구독
        headFlipX.OnValueChanged += OnHeadFlipChanged;
        // 초기 값 적용
        OnHeadFlipChanged(false, headFlipX.Value);
        
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
        walkSFX.loop = true;
        walkSFX.Play();
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
        walkSFX.loop = false;
        walkSFX.Stop();
    }

    public override void OnStateUpdate()
    {
        if(!GetComponent<NetworkObject>().IsOwner) return;
        
        // Input System을 사용하여 키 감지
        if (Keyboard.current != null)
        {
            //*머리 default: 오른쪽 바라봄
            //왼쪽 키 누르면 머리 플립
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                FlipHeadServerRpc(true);
            }
            //오른쪽 키 누르면 머리 플립x
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                FlipHeadServerRpc(false);
            }
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
