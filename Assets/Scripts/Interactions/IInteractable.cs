using UnityEngine;

public interface IInteractable
{
    bool CanInteract(GameObject player);
    void Interact(GameObject player);
}
