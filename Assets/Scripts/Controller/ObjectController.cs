using UnityEngine;
using UnityEngine.Rendering;

public enum RenderRule
{
    AlwaysBehindPlayer,
    AlwaysInFrontOfPlayer,
    ByApproachDirection
}

public enum InteractionType
{
    None,
    Vent,
    Minigame,
    TrialSummon,
    Teleport,
    RareCardShop,
    Entrance
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(40)]
public sealed class ObjectController : MonoBehaviour
{
    [Header("Resource")]
    [SerializeField] private ResourceTable resourceTable;
    [SerializeField] private string resourcePathKey;

    [Header("Collision")]
    [SerializeField] private bool passThrough = true;

    [Header("Rendering")]
    [SerializeField] private RenderRule renderRule = RenderRule.AlwaysBehindPlayer;
    [SerializeField, Range(-50, 50)] private int orderOffset = 0;

    [Header("Interaction")]
    [SerializeField] private bool isInteractable = false;
    [SerializeField] private InteractionType interactionType = InteractionType.None;

    [Header("Refs (자동)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D collider2D;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private Transform player;

    // 내부 상태
    private int _lastAppliedOrder = int.MinValue;

    #region Unity Hooks
    void Reset()
    {
        TryCacheRefs();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Awake()
    {
        TryCacheRefs();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        ApplyAll(); // 런타임에서도 즉시 적용
    }

    void OnValidate()
    {
        TryCacheRefs();
        ApplyAll(); // 인스펙터 변경 즉시 반영
    }

    void LateUpdate()
    {
        // 접근 방향 규칙은 프레임마다 갱신
        if (renderRule == RenderRule.ByApproachDirection)
        {
            ApplySortingDynamic();
        }
    }
    #endregion

    #region Apply Pipeline
    private void ApplyAll()
    {
        // 리소스 테이블 주입
        if (resourceTable != null) ResourceRegistry.Inject(resourceTable);

        ApplySprite();
        ApplyCollision();
        ApplySortingInitial();
        EnsureInteractionController();
    }

    private void ApplySprite()
    {
        if (spriteRenderer == null) return;
        if (string.IsNullOrWhiteSpace(resourcePathKey)) return;

        var s = ResourceRegistry.GetSprite(resourcePathKey);
        if (s != null) spriteRenderer.sprite = s;
    }

    private void ApplyCollision()
    {
        if (collider2D == null) return;
        collider2D.enabled = true;
        collider2D.isTrigger = passThrough; // 통과 O → Trigger
    }

    private void ApplySortingInitial()
    {
        if (renderRule == RenderRule.ByApproachDirection)
        {
            // 동적 규칙은 LateUpdate에서 처리
            ApplySortingDynamic();
            return;
        }

        int baseOrder = 0;
        switch (renderRule)
        {
            case RenderRule.AlwaysBehindPlayer: baseOrder = -100; break;
            case RenderRule.AlwaysInFrontOfPlayer: baseOrder = 100; break;
            default: baseOrder = 0; break;
        }
        SetSortingOrder(baseOrder + orderOffset);
    }

    private SortingGroup _playerSG;
    private SpriteRenderer _playerSR;

    private int GetPlayerSortingOrder()
    {
        if (player == null) return 0;

        if (_playerSG == null) _playerSG = player.GetComponent<SortingGroup>();
        if (_playerSR == null) _playerSR = player.GetComponentInChildren<SpriteRenderer>();

        if (_playerSG != null) return _playerSG.sortingOrder;
        if (_playerSR != null) return _playerSR.sortingOrder;

        return 0; // 플레이어에 정렬 정보 없으면 0 기준
    }

    private void ApplySortingDynamic()
    {
        if (player == null) return;

        int playerOrder = GetPlayerSortingOrder();

        // 상대 오프셋: ±10 정도 권장
        int relative = (player.position.y > transform.position.y) ? +10 : -10;

        int desired = playerOrder + relative + orderOffset;
        SetSortingOrder(desired);
    }

    private void SetSortingOrder(int order)
    {
        if (_lastAppliedOrder == order) return;
        _lastAppliedOrder = order;

        if (sortingGroup != null) sortingGroup.sortingOrder = order;
        else if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
    }

    private void EnsureInteractionController()
    {
        // 상호작용 비활성: 존재하던 컨트롤러 제거
        if (!isInteractable || interactionType == InteractionType.None)
        {
#if UNITY_EDITOR
            RemoveIfExists<VentController>();
            RemoveIfExists<TrialSummonController>();
            RemoveIfExists<EntranceController>();
            // 다른 상호작용이 추가되면 여기에 추가
#endif
            return;
        }

        // 타입별 존재 보장 (중복 Add 방지)
        switch (interactionType)
        {
            case InteractionType.Vent:
                AddIfMissing<VentController>();
                break;
            case InteractionType.TrialSummon:
                AddIfMissing<TrialSummonController>();
                break;
            case InteractionType.Entrance:
                AddIfMissing<EntranceController>();
                break;
                // 다른 상호작용이 추가되면 여기에 추가
        }
    }
    #endregion

    #region Helpers
    private void TryCacheRefs()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (collider2D == null) collider2D = GetComponent<Collider2D>();
        if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
    }

    private T AddIfMissing<T>() where T : Component
    {
        var t = GetComponent<T>();
        if (t == null) t = gameObject.AddComponent<T>();
        return t;
    }

#if UNITY_EDITOR
    private void RemoveIfExists<T>() where T : Component
    {
        var t = GetComponent<T>();
        if (t != null)
        {
            // Editor 모드에서만 안전 제거
            if (Application.isPlaying) Destroy(t);
            else DestroyImmediate(t);
        }
    }
#endif

    // 인스펙터에서 바꾸기 쉬운 공개 Setter
    public void SetResourceKey(string key) { resourcePathKey = key; ApplySprite(); }
    public void SetPassThrough(bool v) { passThrough = v; ApplyCollision(); }
    public void SetRenderRule(RenderRule r) { renderRule = r; ApplySortingInitial(); }
    public void SetInteractable(bool v) { isInteractable = v; EnsureInteractionController(); }
    public void SetInteractionType(InteractionType t) { interactionType = t; EnsureInteractionController(); }
    #endregion
}
