using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractionControllerBase : MonoBehaviour, IInteractable
{
    protected GameObject cachedPlayer;

    protected virtual void Awake()
    {
        if (!cachedPlayer) cachedPlayer = GameObject.FindGameObjectWithTag("Player");
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // 상호작용은 트리거 기준
    }

    public abstract bool CanInteract(GameObject player);
    public abstract void Interact(GameObject player);
}
