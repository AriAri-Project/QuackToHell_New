using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    public void OnVentEnterAnimationComplete()
    {
        PlayerVentEnterState ventState = GetComponentInParent<PlayerVentEnterState>();
        ventState?.OnVentEnterAnimationComplete();
    }

    public void OnVentExitAnimationComplete()
    {
        PlayerVentEnterState ventState = GetComponentInParent<PlayerVentEnterState>();
        ventState?.OnVentExitAnimationComplete();
    }
}
