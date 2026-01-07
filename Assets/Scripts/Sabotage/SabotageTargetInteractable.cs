using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
public sealed class SabotageTargetInteractable : InteractionControllerBase
{
    [Header("Settings")]
    [Tooltip("ForcedInteract 사보타지가 활성화일 때만 상호작용 허용")]
    [SerializeField] private bool onlyWhenForcedSabotageActive = true;

    public override bool CanInteract(GameObject player)
    {
        if (player == null) return false;

        // NetworkObject가 있어야 서버에서 itemNetId로 판정 가능
        if (GetComponent<NetworkObject>() == null) return false;

        if (onlyWhenForcedSabotageActive)
        {
            // 네트워크 매니저가 없거나, 강제 사보타지가 아니면 상호작용 불가(하이라이트도 꺼짐)
            if (SabotageNetworkManager.Instance == null) return false;
            if (!SabotageNetworkManager.Instance.IsForcedInteractActiveClient()) return false;
        }

        return true;
    }

    public override void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;

        SabotageNetworkManager.Instance.TryResolveForcedInteract(player, gameObject);
    }
}
