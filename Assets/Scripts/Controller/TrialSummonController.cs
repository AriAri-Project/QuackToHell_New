using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class TrialSummonController : InteractionControllerBase
{
    [Header("Interaction")]
    [SerializeField, Range(0.5f, 5f)] private float interactionRadius = 1f;
    [SerializeField, Range(0f, 5f)] private float cooldownSec = 0.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool alsoSpace = true;

    [Header("Events")]
    public UnityEvent onSummonRequested;  // 기획자가 GameManager 등 연결해서 처리

    private Transform _tr;
    private readonly System.Collections.Generic.Dictionary<int, float> _lastUse = new();

    protected override void Awake()
    {
        base.Awake();
        _tr = transform;
    }

    void Update()
    {
        var player = cachedPlayer;
        if (!player) return;

        if (!CanInteract(player)) return;

        if (Input.GetKeyDown(interactKey) || (alsoSpace && Input.GetKeyDown(KeyCode.Space)))
        {
            Interact(player);
        }
    }

    public override bool CanInteract(GameObject player)
    {
        if (!player) return false;

        int id = player.GetInstanceID();
        if (_lastUse.TryGetValue(id, out float last))
            if (Time.time - last < cooldownSec) return false;

        float dist = Vector3.Distance(player.transform.position, _tr.position);
        return dist <= interactionRadius;
    }

    public override void Interact(GameObject player)
    {
        // 쿨타임 기록
        _lastUse[player.GetInstanceID()] = Time.time;

        // 이벤트 호출(실제 소집 로직은 GameManager 등에서 구현)
        onSummonRequested?.Invoke();
        // 필요하면 여기서 UI 열기/사운드 재생 등 훅 추가 가능
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "TrialSummon");
    }
#endif
}
