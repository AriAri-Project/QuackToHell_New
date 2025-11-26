using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MinigameController : MonoBehaviour
{

    [SerializeField] private GameObject existingUiInstance;

    [Header("Binding (Designer)")]
    [Tooltip("클라이언트(로컬) 화면에만 뜨는 UI 프리팹")]
    [SerializeField] private GameObject clientUiPrefab;

    [Tooltip("true: 메인 Canvas 아래로 붙임 / false: 지정한 Transform 또는 월드에 생성")]
    [SerializeField] private bool attachUiToMainCanvas = true;

    [Tooltip("UI 부모를 직접 지정하고 싶을 때 사용 (비우면 자동 탐색)")]
    [SerializeField] private Transform uiRootOverride;

    [Header("Interaction")]
    [Tooltip("상호작용 반경(미터). 1m = 1 Unity 유닛")]
    [Min(0.1f)] public float interactionRadius = 1f;

    [Tooltip("상호작용 키(기본: Space)")]
    public KeyCode interactionKey = KeyCode.Space;

    [Header("Mouse Click")]
    [SerializeField] private bool allowMouseClick = true;
    [SerializeField] private bool ignoreUIRaycastBlockers = false;

    // 보상
    [SerializeField] private int rewardGold = 100;
    [SerializeField] private GameObject rewardUIRoot;
    [SerializeField] private TextMeshProUGUI rewardUITMP;
    [SerializeField] private float rewardUIDuration = 2f;          // 요구: 2초

    // 쿨다운
    [SerializeField] private float cooldownSeconds = 10f;

    [SerializeField] private GameObject cooldownToastRoot; // 씬의 CooldownToast (비활성)
    [SerializeField] private TextMeshProUGUI cooldownToastTMP;

    [SerializeField] private float toastFadeIn = 0.15f;
    [SerializeField] private float toastHold = 1.2f;
    [SerializeField] private float toastFadeOut = 0.25f;

    [Tooltip("로컬 플레이어 Transform(비우면 자동 탐색: Netcode Local Player → tag==\"Player\" → Camera.main)")]
    [SerializeField] private Transform overrideLocalPlayer;

    [Header("Highlight (Sprite Outline Clone)")]
    [Tooltip("테두리로 처리할 대상 Renderer들(비우면 자식에서 자동 수집)")]
    [SerializeField] private List<Renderer> highlightRenderers = new();

    [SerializeField, Min(1.0f)] private float outlineScale = 1.08f;   // 복제본 확대 배율
    [SerializeField] private int outlineSortingOffset = -500;            // 정렬순서 오프셋
    [SerializeField] private float outlineAlpha = 1f;                  // 노란색 불투명도(0~1)

    [SerializeField] private Renderer[] dimTargets;
    [SerializeField] private float dimAmount = 0.35f; // 0(그대로)~1(완전 검은색) 사이, 0.35~0.5 추천

    [Header("UX")]
    [Tooltip("UI가 떠 있는 동안 ESC로 닫기 허용")]
    [SerializeField] private bool closeWithEscape = true;

    // state
    private bool _isLocalEligible;
    private GameObject _spawnedLocalUi;
    private readonly List<GameObject> _outlineClones = new();
    private bool _spawnedFromPrefab;
    private float _localCooldownUntil = 0f;
    private Coroutine _toastCo;
    private CanvasGroup _toastCg;
    private Coroutine _rewardUICo;
    private CanvasGroup _rewardUICg;
    private bool _rewardGrantedThisRun;

    // 밝기 복원용 원본 색상 캐시
    private Color[] _originalColors;
    private bool _dimmed;


    // Unity
    private void Awake()
    {
        // 하이라이트 대상 자동 수집(없을 때만)
        if (highlightRenderers == null || highlightRenderers.Count == 0)
        {
            Renderer[] found = GetComponentsInChildren<Renderer>(includeInactive: true);
            highlightRenderers = new List<Renderer>(found);
        }
        DisableHighlight();
    }

    private void Update()
    {
        UpdateLocalEligibilityAndHighlight();

        if (_isLocalEligible && Input.GetKeyDown(interactionKey))
            if (IsLocalOnCooldown(out var remain))
            {
                ShowCooldownPopup(remain);
            }
            else
            {
                OpenUi();
            }

        if (allowMouseClick && _isLocalEligible && Input.GetMouseButtonUp(0))
        {
            // UI가 가리는 경우 막을지 선택
            if (!ignoreUIRaycastBlockers && IsPointerOverUI())
                return;

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 wp3 = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 wp = new Vector2(wp3.x, wp3.y);

                // 이 포인트에 겹치는 2D 콜라이더 전부 확인
                var hits = Physics2D.OverlapPointAll(wp);
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (!h) continue;

                    // 이 미니게임 오브젝트 자신(또는 자식)인지 확인
                    if (h.transform == transform || h.transform.IsChildOf(transform))
                    {
                        if (IsLocalOnCooldown(out var remain))
                            ShowCooldownPopup(remain);
                        else
                            OpenUi();
                        break;
                    }
                }
            }
        }

        if (closeWithEscape && _spawnedLocalUi && _spawnedLocalUi.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseUi();
    }

    // 2D 클릭
    private void OnMouseUpAsButton()
    {
        if (_isLocalEligible && !IsPointerOverUI())
            OpenUi();
    }

    private void OnDestroy()
    {
        DisableHighlight();
        // 생성했던 복제본 정리
        for (int i = 0; i < _outlineClones.Count; i++)
        {
            if (_outlineClones[i]) Destroy(_outlineClones[i]);
        }
        _outlineClones.Clear();

        if (_spawnedLocalUi)
        {
            if (_spawnedFromPrefab) Destroy(_spawnedLocalUi);
            else _spawnedLocalUi.SetActive(false); // 씬 원본은 보존
        }
    }

    // === Eligibility / Highlight ===
    private void UpdateLocalEligibilityAndHighlight()
    {
        var player = ResolveLocalPlayer();
        if (player == null)
        {
            SetEligible(false);
            return;
        }

        float dist = Vector3.Distance(player.position, transform.position);
        SetEligible(dist <= interactionRadius);
    }

    private Transform ResolveLocalPlayer()
    {
        // 인스펙터에서 직접 지정해 둔 경우 그대로 사용
        if (overrideLocalPlayer) return overrideLocalPlayer;

        // Netcode가 켜져 있을 때만 네트워크 경로로 찾기
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            var nm = NetworkManager.Singleton;

            var localPlayerObj = nm.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObj != null)
                return localPlayerObj.transform;

            foreach (var netObj in FindObjectsOfType<NetworkObject>())
            {
                if (netObj != null &&
                    netObj.CompareTag("Player") &&
                    netObj.IsOwner)
                {
                    return netObj.transform;
                }
            }

            Debug.LogWarning("[MinigameController] 로컬 플레이어를 Netcode로 찾지 못했습니다.");
        }

        // 여기까지 왔다는 건 Netcode 안 쓰는 싱글 테스트이거나, 아직 네트워크가 시작 안 된 상태 등
        var tagged = GameObject.FindWithTag("Player");
        if (tagged) return tagged.transform;

        return Camera.main ? Camera.main.transform : null;
    }

    private void SetEligible(bool eligible)
    {
        if (_isLocalEligible == eligible) return;
        _isLocalEligible = eligible;
        if (eligible) EnableHighlight(); else DisableHighlight();
    }

    private void EnableHighlight()
    {
        foreach (var r in highlightRenderers)
        {
            if (!r) continue;
            var sr = r as SpriteRenderer ?? r.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            var existing = sr.transform.Find("OutlineClone");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                continue;
            }

            var go = new GameObject("OutlineClone");
            go.transform.SetParent(sr.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * outlineScale;

            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = sr.sprite;
            sr2.flipX = sr.flipX;
            sr2.flipY = sr.flipY;

            sr2.sortingLayerID = sr.sortingLayerID;
            sr2.sortingOrder = sr.sortingOrder + outlineSortingOffset;

            // 머티리얼 코드로 생성
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.yellow; // 항상 진한 노란색
            sr2.material = mat;

            _outlineClones.Add(go);
        }
    }

    private void DisableHighlight()
    {
        // 복제본만 숨김
        for (int i = 0; i < _outlineClones.Count; i++)
        {
            if (_outlineClones[i]) _outlineClones[i].SetActive(false);
        }

        // 혹시 Emission 머티리얼을 함께 쓰는 경우가 있을 수 있어 원복도 시도(무시 가능)
        foreach (var r in highlightRenderers)
        {
            if (!r) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m) continue;
                if (m.IsKeywordEnabled("_EMISSION"))
                    m.DisableKeyword("_EMISSION");
            }
        }
    }

    // 성공 시 호출
    private void HandleMinigameClearedReward()
    {
        if (_rewardGrantedThisRun) return;
        _rewardGrantedThisRun = true;

        // 서버에 보상(골드) 지급을 요청
        GrantGoldServerRpc(rewardGold);
    }

    // 보상 지급(골드) + 해당 클라에 팝업 알림
    [Unity.Netcode.ServerRpc(RequireOwnership = false)]
    private void GrantGoldServerRpc(int amount, Unity.Netcode.ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;

        // PlayerHelperManager 싱글톤 접근
        var model = PlayerHelperManager.Instance.GetPlayerModelByClientId(senderId);
        if (model == null)
        {
            UnityEngine.Debug.LogWarning($"Player model not found for client {senderId}");
            return;
        }

        // 현재 골드 읽고 누적값 계산
        int current = PlayerHelperManager.Instance.GetPlayerGoldByClientId(senderId);
        int newGold = current + amount;

        model.SetGoldServerRpc(newGold);

        // 해당 클라에만 보상 팝업
        var target = new Unity.Netcode.ClientRpcParams
        {
            Send = new Unity.Netcode.ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { senderId }
            }
        };
        ShowRewardPopupClientRpc(amount, target);
    }

    // 클라이언트에서(해당 클라만) 2초 팝업
    [Unity.Netcode.ClientRpc]
    private void ShowRewardPopupClientRpc(int amount, Unity.Netcode.ClientRpcParams target = default)
    {
        ShowRewardPopup(amount);
    }

    private void ShowRewardPopup(int gold)
    {
        if (rewardUIRoot == null)
        {
            Debug.Log($"[MG] Reward +{gold} (RewardUI not assigned)");
            return;
        }

        if (_rewardUICg == null)
            _rewardUICg = rewardUIRoot.GetComponent<CanvasGroup>() ?? rewardUIRoot.AddComponent<CanvasGroup>();

        _rewardUICg.interactable = false;
        _rewardUICg.blocksRaycasts = false;

        // 텍스트 자동 탐색
        if (rewardUITMP == null)
        {
            rewardUITMP = rewardUIRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        string msg = $"미니게임 클리어! {gold}골드 획득";
        if (rewardUITMP != null) rewardUITMP.text = msg;

        if (_rewardUICo != null) StopCoroutine(_rewardUICo);
        _rewardUICo = StartCoroutine(CoShowRewardUI());
    }

    private System.Collections.IEnumerator CoShowRewardUI()
    {
        rewardUIRoot.SetActive(true);
        _rewardUICg.alpha = 1f;
        yield return new WaitForSecondsRealtime(rewardUIDuration);
        _rewardUICg.alpha = 0f;
        rewardUIRoot.SetActive(false);
        _rewardUICo = null;
    }

    private bool IsLocalOnCooldown(out int remainSec)
    {
        float remain = _localCooldownUntil - Time.unscaledTime;
        if (remain > 0f)
        {
            remainSec = Mathf.CeilToInt(remain);
            return true;
        }
        remainSec = 0;
        return false;
    }

    private void BeginLocalCooldown()
    {
        _localCooldownUntil = Time.unscaledTime + cooldownSeconds;
        ApplyDim(true);
    }

    // 매 프레임 쿨타임 끝났는지 확인해서 자동 복구
    private void LateUpdate()
    {
        if (_localCooldownUntil > 0f && Time.unscaledTime >= _localCooldownUntil)
        {
            _localCooldownUntil = 0f;
            ApplyDim(false);
        }
    }

    // 명도 낮추기 / 복원
    private void ApplyDim(bool on)
    {
        // 대상 없으면 자동 수집(한 번만)
        if (dimTargets == null || dimTargets.Length == 0)
            dimTargets = GetComponentsInChildren<Renderer>(true);

        if (dimTargets == null || dimTargets.Length == 0) return;

        // 원본 색상 캐시는 처음 어둡게 만들 때 확보
        if (_originalColors == null || _originalColors.Length != dimTargets.Length)
            _originalColors = new Color[dimTargets.Length];

        for (int i = 0; i < dimTargets.Length; i++)
        {
            var r = dimTargets[i];
            if (!r) continue;

            // 개별 머티리얼 인스턴스 사용(공유 머티리얼 색 변조 방지)
            var mat = r.material;
            if (!mat.HasProperty("_Color")) continue;

            if (on)
            {
                // 아직 캐시 안 했으면 저장
                if (!_dimmed)
                    _originalColors[i] = mat.color;

                float k = Mathf.Clamp01(1f - dimAmount); // 0.35 → 약 65% 밝기
                var c0 = (_dimmed ? _originalColors[i] : mat.color);
                mat.color = new Color(c0.r * k, c0.g * k, c0.b * k, c0.a);
            }
            else
            {
                // 복원
                if (_originalColors != null)
                    mat.color = _originalColors[i];
            }
        }

        _dimmed = on;
    }

    // 쿨타임 팝업
    private void ShowCooldownPopup(int remainSec)
    {
        if (cooldownToastRoot == null)
        {
            Debug.Log($"[MG] Cooldown: {remainSec}s (toast root not assigned)");
            return;
        }

        // 초기 캐시
        if (_toastCg == null) _toastCg = cooldownToastRoot.GetComponent<CanvasGroup>();
        if (_toastCg == null) _toastCg = cooldownToastRoot.AddComponent<CanvasGroup>();
        _toastCg.interactable = false;
        _toastCg.blocksRaycasts = false;

        // 텍스트 세팅
        string msg = $"미니게임을 플레이 할 수 없습니다.\n미니게임 쿨타임이 {remainSec}초 남았습니다!";
        if (cooldownToastTMP != null) cooldownToastTMP.text = msg;

        // 중복 재생 시 이전 코루틴 중단
        if (_toastCo != null) StopCoroutine(_toastCo);
        _toastCo = StartCoroutine(CoShowCooldownToast());
    }

    private System.Collections.IEnumerator CoShowCooldownToast()
    {
        cooldownToastRoot.SetActive(true);
        _toastCg.alpha = 0f;

        // Fade In
        float t = 0f;
        while (t < toastFadeIn)
        {
            t += Time.unscaledDeltaTime;
            _toastCg.alpha = Mathf.SmoothStep(0f, 1f, t / toastFadeIn);
            yield return null;
        }
        _toastCg.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(toastHold);

        // Fade Out
        t = 0f;
        while (t < toastFadeOut)
        {
            t += Time.unscaledDeltaTime;
            _toastCg.alpha = Mathf.SmoothStep(1f, 0f, t / toastFadeOut);
            yield return null;
        }
        _toastCg.alpha = 0f;
        cooldownToastRoot.SetActive(false);
        _toastCo = null;
    }

    // === UI Open/Close ===
    public void OpenUi()
    {
        // 이미 열려 있으면 무시
        if (_spawnedLocalUi && _spawnedLocalUi.activeSelf) return;

        // 1) 씬에 있는 원본 우선 사용
        if (existingUiInstance != null)
        {
            _spawnedLocalUi = existingUiInstance;
            _spawnedFromPrefab = false;
            _spawnedLocalUi.SetActive(true);

            var p1 = _spawnedLocalUi.GetComponentInChildren<AreaClickGame>(true);
            var p2 = _spawnedLocalUi.GetComponentInChildren<ClickOncePointsGame>(true);
            Component playable2 = (Component)p1 ?? (Component)p2;
            if (playable2 != null) playable2.gameObject.SetActive(true);

            var rt2 = _spawnedLocalUi.GetComponent<RectTransform>();
            if (rt2 != null) rt2.SetAsLastSibling(); // 최상단으로
        }
        else
        {
            // 2) 프리팹 생성
            if (!clientUiPrefab)
            {
                Debug.LogError("[MG] clientUiPrefab is NULL");
                return;
            }

            Transform parent = uiRootOverride;
            if (parent == null && attachUiToMainCanvas)
            {
                var canvas = FindMainCanvas();
                if (canvas == null)
                {
                    Debug.LogError("[MG] No Canvas found while attachUiToMainCanvas=ON");
                    return;
                }
                parent = canvas.transform;
            }

            _spawnedLocalUi = Instantiate(clientUiPrefab, parent);
            _spawnedFromPrefab = true;

            // 캔버스 하위면 전체 스트레치
            var rt = _spawnedLocalUi.GetComponent<RectTransform>();
            if (rt != null && parent != null && parent.GetComponentInParent<Canvas>())
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.anchoredPosition3D = Vector3.zero;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            _spawnedLocalUi.SetActive(true);
        }

        // 3) 자동 닫기 이벤트 연결 (클리어/취소 → CloseUi)
        {
            var g1 = _spawnedLocalUi.GetComponentInChildren<AreaClickGame>(true);
            if (g1 != null)
            {
                g1.OnCleared.RemoveListener(CloseUi);
                g1.OnCanceled.RemoveListener(CloseUi);
                g1.OnCleared.AddListener(CloseUi);
                g1.OnCanceled.AddListener(CloseUi);

                g1.OnCleared.RemoveListener(HandleMinigameClearedReward);
                g1.OnCleared.AddListener(HandleMinigameClearedReward);

                g1.OnCleared.RemoveListener(BeginLocalCooldown);
                g1.OnCleared.AddListener(BeginLocalCooldown);
            }
            var g2 = _spawnedLocalUi.GetComponentInChildren<ClickOncePointsGame>(true);
            if (g2 != null)
            {
                g2.OnCleared.RemoveListener(CloseUi);
                g2.OnCanceled.RemoveListener(CloseUi);
                g2.OnCleared.AddListener(CloseUi);
                g2.OnCanceled.AddListener(CloseUi);

                g2.OnCleared.RemoveListener(HandleMinigameClearedReward);
                g2.OnCleared.AddListener(HandleMinigameClearedReward);

                g2.OnCleared.RemoveListener(BeginLocalCooldown);
                g2.OnCleared.AddListener(BeginLocalCooldown);
            }

            _rewardGrantedThisRun = false;
        }

        Debug.Log("UI Opened");
    }
    public void CloseUi()
    {
        if (_spawnedLocalUi == null) return;

        if (_spawnedFromPrefab)
        {
            Destroy(_spawnedLocalUi);
        }
        else
        {
            _spawnedLocalUi.SetActive(false); // 씬 원본은 파괴 X
        }

        _spawnedLocalUi = null;
    }

    // === Helpers ===
    private Canvas FindMainCanvas()
    {
        var tagged = GameObject.FindWithTag("MainCanvas");
        if (tagged && tagged.TryGetComponent(out Canvas c)) return c;
        return GameObject.FindAnyObjectByType<Canvas>();
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
