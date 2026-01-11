using UnityEngine;

public class SabotageTargetItem : MonoBehaviour
{
    public void OnInteracted(GameObject player)
    {
        if (SabotageNetworkManager.Instance == null) return;
        SabotageNetworkManager.Instance.TryResolveForcedInteract(player, gameObject);
    }
}
