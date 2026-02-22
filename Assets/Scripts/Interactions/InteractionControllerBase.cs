using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class InteractionControllerBase : MonoBehaviour, IInteractable
{
    

    protected virtual void Awake()
    {
        Collider2D collider = GetComponent<Collider2D>();
        collider.isTrigger = true; // 상호작용은 트리거 기준
    }

    public abstract bool CanInteract(GameObject player);
    public abstract void Interact(GameObject player);
}
