using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    public void OnVentEnterAnimationComplete()
    {
        PlayerVentState ventState = GetComponentInParent<PlayerVentState>();
        ventState?.OnVentEnterAnimationComplete();
    }

    public void OnVentExitAnimationComplete()
    {
        PlayerVentState ventState = GetComponentInParent<PlayerVentState>();
        ventState?.OnVentExitAnimationComplete();
    }
}
