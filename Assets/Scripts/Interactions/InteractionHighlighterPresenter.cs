using UnityEngine;

[DefaultExecutionOrder(60)]
public sealed class InteractionHighlighterPresenter : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject; // Vent/Highlight
    [SerializeField] private MonoBehaviour interactable;  // VentController 등 (IInteractable)
    [SerializeField] private Transform player;

    IInteractable _ia;

    void Reset()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Awake()
    {
        _ia = interactable as IInteractable;
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (highlightObject) highlightObject.SetActive(false);
    }

    void Update()
    {
        if (_ia == null || player == null || highlightObject == null) return;
        // CanInteract가 true일 때만 하이라이트 ON
        bool on = _ia.CanInteract(player.gameObject);
        if (highlightObject.activeSelf != on) highlightObject.SetActive(on);
    }
}
