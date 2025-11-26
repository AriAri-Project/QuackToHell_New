using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerDeadState : NetworkStateBase
{
    [SerializeField] private GameObject head;
    [SerializeField] private Animator animator;
    public AudioSource deathSFX;
    [SerializeField]
    private SpriteRenderer[] spriteRenderers;
    [SerializeField]
    private PlayerModel playerModel;
    const float MOVE_SPEED_MULTIPLIER = 1.5f;
    
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
        if (spriteRenderers[0] != null)
        {
            spriteRenderers[0].flipX = newValue;
        }
    }
    public override void OnStateEnter()
    {
        head.SetActive(false);
        //애니메이션 on
        TriggerWalkAnimation();
        //죽으면 이펙트 
        //Assets/Resources/Prefabs/FX_PF_Electricity_AreaExplosion_Blue.prefab
        GameObject effect = Resources.Load<GameObject>("Prefabs/FX_PF_Electricity_AreaExplosion_Blue");
        if (IsOwner)
        {
            Instantiate(effect,transform.position,Quaternion.identity);   
            SoundManager.Instance.SFXPlay(deathSFX.name, deathSFX.clip);
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
        if(!GetComponent<NetworkObject>().IsOwner) return;
        
        // Input System을 사용하여 키 감지
        if (Keyboard.current != null)
        {
            //*머리 default: 왼쪽 바라봄
            //왼쪽 키 누르면 머리 플립x
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                FlipHeadServerRpc(false);
            }
            //오른쪽 키 누르면 머리 플립
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                FlipHeadServerRpc(true);
            }
        }
    }
    [ServerRpc]
    private void FlipHeadServerRpc(bool flip)
    {
        // 서버에서 머리 플립 상태 변경
        headFlipX.Value = flip;
    }
}
