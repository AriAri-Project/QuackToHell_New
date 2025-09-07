using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class VentController : InteractionControllerBase
{
    [Header("Identification")]
    [SerializeField] private string ventId;

    [Header("Vent Animations")]
    [SerializeField] private AnimationClip enterAnimation;
    [SerializeField] private AnimationClip exitAnimation;
    [SerializeField] private Animator animator;

    [Header("Interaction")]
    [SerializeField, Range(0.5f, 5f)] private float interactionRadius = 1f;
    [SerializeField, Range(0f, 2f)] private float cooldownSec = 0.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool alsoSpace = true;
    [SerializeField] private Vector2 exitOffset = new(0f, 0.5f);

    [Header("Links (Max 4)")]
    public List<VentController> linkedVents = new();

    [Header("Arrow Placement")]
    [SerializeField, Min(0.1f)] private float arrowDistanceFromSource = 1.1f; // A에서 떨어질 절대 거리
    [SerializeField, Min(0.1f)] private float keepAwayFromTarget = 0.4f;      // B로부터 최소 이격
    [SerializeField, Range(-0.5f, 0.5f)] private float arrowNormalOffset = 0.0f;

    [Header("Player Handling While Inside")]
    [Tooltip("벤트 탑승 중 비활성화할 플레이어 컴포넌트(이동/입력 스크립트 등)를 여기에 드래그하세요.")]
    [SerializeField] private List<Behaviour> disableWhileInside = new();
    [Tooltip("Renderer.enabled를 꺼서 완전히 숨길지, 알파만 낮출지 선택")]
    [SerializeField] private bool hideByDisableRenderer = true;
    [SerializeField, Range(0f, 1f)] private float insideAlpha = 0.15f;
    [Tooltip("Rigidbody2D가 있으면 physics를 멈춰 고정합니다.")]
    [SerializeField] private bool freezeRigidbody2D = true;

    // 점유/상태
    private bool _isOccupied;
    private GameObject _currentPlayer;
    private readonly Dictionary<int, float> _lastExitTimeByPlayer = new();

    // 화살표
    [SerializeField] private GameObject arrowPrefab; // 반드시 연결
    private readonly List<GameObject> _spawnedArrows = new();

    // 캐시
    private Transform _tr;
    private Rigidbody2D _playerRb;
    private bool _rbPrevSimulated;
    private List<SpriteRenderer> _playerRenderers = new();
    private List<bool> _rendererPrevEnabled = new();
    private List<Color> _rendererPrevColor = new();

    // 링크 변경 감지(탑승 중 편집 반영)
    private int _linksHash;

    // 벤트 탑승 및 탈출 애니메이션
    private void PlayEnterAnimation()
    {
        if (animator && enterAnimation)
            animator.Play(enterAnimation.name);
    }

    private void PlayExitAnimation()
    {
        if (animator && exitAnimation)
            animator.Play(exitAnimation.name);
    }

    protected override void Awake()
    {
        base.Awake();
        _tr = transform;

        // 넘버링
        if (string.IsNullOrEmpty(ventId))
            ventId = System.Guid.NewGuid().ToString("N").Substring(0, 6);
        name = $"Vent_{ventId}";
    }

    void Update()
    {
        var player = cachedPlayer;
        if (!player) return;

        // 탑승 중 → 탈출 입력
        if (_isOccupied && _currentPlayer == player)
        {
            if (Input.GetKeyDown(interactKey) || (alsoSpace && Input.GetKeyDown(KeyCode.Space)))
            {
                ExitVent(player);
                return;
            }

            // 탑승 중에 링크 변경되면 즉시 화살표 갱신
            int h = ComputeLinksHash();
            if (h != _linksHash)
            {
                DespawnArrows();
                SpawnArrows();
                _linksHash = h;
            }
        }
        // 비점유 상태 → 진입 입력
        else
        {
            float dist = Vector3.Distance(player.transform.position, _tr.position);
            bool inRange = dist <= interactionRadius;
            if (inRange && CanInteract(player) &&
                (Input.GetKeyDown(interactKey) || (alsoSpace && Input.GetKeyDown(KeyCode.Space))))
            {
                EnterVent(player);
            }
        }
    }

    public override bool CanInteract(GameObject player)
    {
        if (_isOccupied) return false; // 다른 플레이어가 점유 중
        int id = player.GetInstanceID();
        if (_lastExitTimeByPlayer.TryGetValue(id, out float last))
            if (Time.time - last < cooldownSec) return false; // 쿨타임
        float dist = Vector3.Distance(player.transform.position, _tr.position);
        return dist <= interactionRadius;
    }

    public override void Interact(GameObject player)
    {
        // 클릭 등 외부 호출 시 동일한 규칙 적용
        if (_isOccupied && _currentPlayer == player) ExitVent(player);
        else if (CanInteract(player)) EnterVent(player);
    }

    // 핵심 플로우

    private void EnterVent(GameObject player)
    {
        if (_isOccupied) return;

        _isOccupied = true;
        _currentPlayer = player;

        //탑승 애니메이션
        PlayEnterAnimation();

        // 플레이어 고정 & 가시성 조정
        CapturePlayerCaches(player);
        SetPlayerInsideVisual(true);
        SetPlayerInsideMovement(true);

        // 화살표 생성
        SpawnArrows();
        _linksHash = ComputeLinksHash();
    }

    private void ExitVent(GameObject player)
    {
        if (!_isOccupied || _currentPlayer != player) return;

        // 탈출 애니메이션
        PlayExitAnimation();

        // 플레이어를 벤트 출구 위치로 이동
        player.transform.position = _tr.position + (Vector3)exitOffset;

        // 가시성/이동 원복
        SetPlayerInsideMovement(false);
        SetPlayerInsideVisual(false);
        ClearPlayerCaches();

        // 쿨타임 기록
        _lastExitTimeByPlayer[player.GetInstanceID()] = Time.time;

        // 상태 정리
        _isOccupied = false;
        _currentPlayer = null;

        // 화살표 제거
        DespawnArrows();
    }

    public void RequestMoveTo(VentController target)
    {
        if (!_isOccupied || _currentPlayer == null) return;
        if (!target) return;

        // A의 화살표 제거
        DespawnArrows();

        // "플레이어 위치"를 바로 B의 위치로 옮긴다 (여전히 투명/고정 상태 유지)
        _currentPlayer.transform.position = target.transform.position;

        // 플레이어 상태 캐시를 B에게 전달
        TransferPlayerCachesTo(target);

        // 점유 이전
        target._isOccupied = true;
        target._currentPlayer = _currentPlayer;

        // B에서 화살표 재생성
        target.SpawnArrows();
        target._linksHash = target.ComputeLinksHash();

        // A 초기화
        _isOccupied = false;
        _currentPlayer = null;
    }

    // 화살표

    private void SpawnArrows()
    {
        if (arrowPrefab == null) return;

        DespawnArrows();

        int count = Mathf.Min(4, linkedVents?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            var target = linkedVents[i];
            if (!target) continue;

            var go = Instantiate(arrowPrefab, transform.parent); // 같은 레이어에 생성
            var arrow = go.GetComponent<VentArrowController>();

            Vector3 a = _tr.position;
            Vector3 b = target.transform.position;
            Vector3 ab = b - a;
            float d = ab.magnitude;
            if (d < 0.001f) continue;

            Vector3 dir = ab / d;
            Vector3 perp = new(-dir.y, dir.x, 0f);

            // A에서 떨어진 거리, B와의 최소 이격 보정
            float place = Mathf.Clamp(arrowDistanceFromSource, 0.05f, Mathf.Max(0.05f, d - keepAwayFromTarget));

            // 위치 계산
            Vector3 pos = a + dir * place + perp * arrowNormalOffset;
            go.transform.position = pos;

            // 회전 (B 쪽을 향하게)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            arrow.Setup(this, target);
            _spawnedArrows.Add(go);
        }
    }

    private void DespawnArrows()
    {
        for (int i = 0; i < _spawnedArrows.Count; i++)
            if (_spawnedArrows[i]) Destroy(_spawnedArrows[i]);
        _spawnedArrows.Clear();
    }

    // 플레이어 가시성/고정

    private void CapturePlayerCaches(GameObject player)
    {
        _playerRenderers.Clear();
        _rendererPrevEnabled.Clear();
        _rendererPrevColor.Clear();

        player.GetComponentsInChildren<SpriteRenderer>(true, _playerRenderers);

        for (int i = 0; i < _playerRenderers.Count; i++)
        {
            var r = _playerRenderers[i];
            _rendererPrevEnabled.Add(r.enabled);
            _rendererPrevColor.Add(r.color);
        }

        _playerRb = player.GetComponent<Rigidbody2D>();
        if (_playerRb) _rbPrevSimulated = _playerRb.simulated;
    }

    private void TransferPlayerCachesTo(VentController target)
    {
        if (target == null) return;

        // 렌더러/색/활성화 상태
        target._playerRenderers.Clear();
        target._rendererPrevEnabled.Clear();
        target._rendererPrevColor.Clear();

        // 같은 리스트를 복사(참조만 옮겨도 되지만 안전하게 복제)
        target._playerRenderers.AddRange(_playerRenderers);
        target._rendererPrevEnabled.AddRange(_rendererPrevEnabled);
        target._rendererPrevColor.AddRange(_rendererPrevColor);

        // 물리 캐시
        target._playerRb = _playerRb;
        target._rbPrevSimulated = _rbPrevSimulated;
    }

    private void ClearPlayerCaches()
    {
        _playerRenderers.Clear();
        _rendererPrevEnabled.Clear();
        _rendererPrevColor.Clear();
        _playerRb = null;
    }

    private void SetPlayerInsideVisual(bool inside)
    {
        for (int i = 0; i < _playerRenderers.Count; i++)
        {
            var r = _playerRenderers[i];
            if (!r) continue;

            if (inside)
            {
                if (hideByDisableRenderer) r.enabled = false;
                else
                {
                    var c = r.color;
                    c.a = insideAlpha;
                    r.color = c;
                }
            }
            else
            {
                if (hideByDisableRenderer) r.enabled = _rendererPrevEnabled.Count > i ? _rendererPrevEnabled[i] : true;
                else
                {
                    r.color = _rendererPrevColor.Count > i ? _rendererPrevColor[i] : new Color(1, 1, 1, 1);
                }
            }
        }
    }

    private void SetPlayerInsideMovement(bool inside)
    {
        // 이동/입력 스크립트 비활성화
        for (int i = 0; i < disableWhileInside.Count; i++)
        {
            var b = disableWhileInside[i];
            if (!b) continue;
            b.enabled = !inside;
        }

        // 물리 중지
        if (_playerRb && freezeRigidbody2D)
            _playerRb.simulated = !inside;
    }

    // 링크 변경 감지 해시

    private int ComputeLinksHash()
    {
        int h = 17;
        if (linkedVents != null)
        {
            for (int i = 0; i < linkedVents.Count; i++)
            {
                var v = linkedVents[i];
                h = h * 31 + (v ? v.GetInstanceID() : 0);
            }
        }
        return h;
    }

    void OnValidate()
    {
        if (linkedVents.Count > 4) linkedVents.RemoveRange(4, linkedVents.Count - 4);

        if (Application.isPlaying && _isOccupied)
        {
            DespawnArrows();
            SpawnArrows();
            _linksHash = ComputeLinksHash();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"Vent_{ventId}");
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        foreach (var v in linkedVents)
        {
            if (!v) continue;
            Gizmos.DrawLine(transform.position, v.transform.position);
        }
#endif
    }
}
