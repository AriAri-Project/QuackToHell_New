using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class EntranceController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool forceTrigger = true;

    [Header("Events")]
    public UnityEvent onEntered; // 플레이어가 출입구에 진입했을 때(건물 내부 진입 처리)
    public UnityEvent onExited;  // 플레이어가 출입구에서 나갔을 때(건물 외부로 나간 처리)

    [Header("Refs (자동)")]
    [SerializeField] private Collider2D col;

    private bool _inside;

    void Reset()
    {
        if (col == null) col = GetComponent<Collider2D>();
    }

    void Awake()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (forceTrigger && col != null)
        {
            col.enabled = true;
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (_inside) return;
            _inside = true;
            onEntered?.Invoke();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!_inside) return;
            _inside = false;
            onExited?.Invoke();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.25f);
        var b = col.bounds;
        Gizmos.DrawCube(b.center, b.size);

        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Entrance");
    }
#endif
}
