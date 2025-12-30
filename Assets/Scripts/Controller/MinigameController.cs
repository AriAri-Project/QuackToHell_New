using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class MinigameController : MonoBehaviour
{
    [SerializeField] private GameObject existingUiInstance;

    [Header("Binding (Designer)")]
    [SerializeField] private GameObject clientUiPrefab;
    [SerializeField] private bool attachUiToMainCanvas = true;
    [SerializeField] private Transform uiRootOverride;

    [Header("Interaction")]
    [Min(0.1f)] public float interactionRadius = 1f;

    [Tooltip("권장: false. (오브젝트가 스스로 입력/범위감지 하지 않게 함)")]
    [SerializeField] private bool enableSelfInput = false;

    [Tooltip("legacy self-input key")]
    public KeyCode interactionKey = KeyCode.Space;

    [Header("Mouse Click (legacy self-input)")]
    [SerializeField] private bool allowMouseClick = true;
    [SerializeField] private bool ignoreUIRaycastBlockers = false;

    // 보상
    [SerializeField] private int rewardGold = 100;
    [SerializeField] private GameObject rewardUIRoot;
    [SerializeField] private TextMeshProUGUI rewardUITMP;
    [SerializeField] private float rewardUIDuration = 2f;

    // 쿨다운
    [SerializeField] private float cooldownSeconds = 10f;

    [SerializeField] private GameObject cooldownToastRoot;
    [SerializeField] private TextMeshProUGUI cooldownToastTMP;

    [SerializeField] private float toastFadeIn = 0.15f;
    [SerializeField] private float toastHold = 1.2f;
    [SerializeField] private float toastFadeOut = 0.25f;

    [Tooltip("로컬 플레이어 Transform(비우면 자동 탐색) - self-input일 때만 사용")]
    [SerializeField] private Transform overrideLocalPlayer;

    [Header("Highlight (Sprite Outline Clone)")]
    [SerializeField] private List<Renderer> highlightRenderers = new();
    [SerializeField, Min(1.0f)] private float outlineScale = 1.03f;
    [SerializeField] private int outlineSortingOffset = -1;
    [SerializeField] private float outlineAlpha = 1f;

    [SerializeField] private Renderer[] dimTargets;
    [SerializeField] private float dimAmount = 0.35f;

    [Header("UX")]
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

    private Color[] _originalColors;
    private bool _dimmed;

    private void Awake()
    {
        if (highlightRenderers == null || highlightRenderers.Count == 0)
        {
            Renderer[] found = GetComponentsInChildren<Renderer>(includeInactive: true);
            highlightRenderers = new List<Renderer>(found);
        }
        DisableHighlight();
    }

    // =========================
    // Player가 쓰는 public API
    // =========================
    public bool CanInteract(Transform player)
    {
        if (player == null) return false;
        return Vector3.Distance(player.position, transform.position) <= interactionRadius;
    }

    public bool TryOpenFromPlayer(Transform player)
    {
        if (!CanInteract(player)) return false;

        if (IsLocalOnCooldown(out var remain))
        {
            ShowCooldownPopup(remain);
            return false;
        }

        OpenUi();
        return true;
    }

    public void TryCloseFromPlayer()
    {
        CloseUi();
    }

    // =========================
    // 기존 self input 유지하고 싶을 때만
    // =========================
    private void Update()
    {

        UpdateLocalEligibilityAndHighlight();

        if (!enableSelfInput) return;

        if (_isLocalEligible && Input.GetKeyDown(interactionKey))
        {
            if (IsLocalOnCooldown(out var remain)) ShowCooldownPopup(remain);
            else OpenUi();
        }

        if (allowMouseClick && _isLocalEligible && Input.GetMouseButtonUp(0))
        {
            if (!ignoreUIRaycastBlockers && IsPointerOverUI()) return;

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 wp3 = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 wp = new Vector2(wp3.x, wp3.y);

                var hits = Physics2D.OverlapPointAll(wp);
                for (int i = 0; i < hits.Length; i++)
                {
                    var h = hits[i];
                    if (!h) continue;

                    if (h.transform == transform || h.transform.IsChildOf(transform))
                    {
                        if (IsLocalOnCooldown(out var remain)) ShowCooldownPopup(remain);
                        else OpenUi();
                        break;
                    }
                }
            }
        }

        if (closeWithEscape && _spawnedLocalUi && _spawnedLocalUi.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            CloseUi();
    }

    private void LateUpdate()
    {
        if (_localCooldownUntil > 0f && Time.unscaledTime >= _localCooldownUntil)
        {
            _localCooldownUntil = 0f;
            ApplyDim(false);
        }
    }

    private void OnDestroy()
    {
        DisableHighlight();
        for (int i = 0; i < _outlineClones.Count; i++)
            if (_outlineClones[i]) Destroy(_outlineClones[i]);
        _outlineClones.Clear();

        if (_spawnedLocalUi)
        {
            if (_spawnedFromPrefab) Destroy(_spawnedLocalUi);
            else _spawnedLocalUi.SetActive(false);
        }
    }

    // === Eligibility / Highlight (self-input일 때만 자동 갱신) ===
    private void UpdateLocalEligibilityAndHighlight()
    {
        var player = ResolveLocalPlayer();
        if (player == null) { SetEligible(false); return; }

        float dist = Vector3.Distance(player.position, transform.position);
        SetEligible(dist <= interactionRadius);
    }

    private Transform ResolveLocalPlayer()
    {
        if (overrideLocalPlayer) return overrideLocalPlayer;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            var nm = NetworkManager.Singleton;
            var localPlayerObj = nm.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObj != null) return localPlayerObj.transform;

            foreach (var netObj in FindObjectsOfType<NetworkObject>())
                if (netObj && netObj.CompareTag("Player") && netObj.IsOwner)
                    return netObj.transform;
        }

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

            go.transform.localScale = Vector3.one * 1.09f;

            var sr2 = go.AddComponent<SpriteRenderer>();
            sr2.sprite = sr.sprite;
            sr2.flipX = sr.flipX;
            sr2.flipY = sr.flipY;

            sr2.sortingLayerID = sr.sortingLayerID;
            sr2.sortingOrder = sr.sortingOrder - 1;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                sr2.material = new Material(shader); 
            }

            sr2.color = new Color(1f, 1f, 1f, 0.85f);

            _outlineClones.Add(go);
        }
    }


    private void DisableHighlight()
    {
        for (int i = 0; i < _outlineClones.Count; i++)
            if (_outlineClones[i]) _outlineClones[i].SetActive(false);
    }

    private void HandleMinigameClearedReward()
    {
        if (_rewardGrantedThisRun) return;
        _rewardGrantedThisRun = true;
        GrantGoldServerRpc(rewardGold);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrantGoldServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;

        var model = PlayerHelperManager.Instance.GetPlayerModelByClientId(senderId);
        if (model == null) return;

        int current = PlayerHelperManager.Instance.GetPlayerGoldByClientId(senderId);
        int newGold = current + amount;

        model.SetGoldServerRpc(newGold);

        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderId } }
        };
        ShowRewardPopupClientRpc(amount, target);
    }

    [ClientRpc]
    private void ShowRewardPopupClientRpc(int amount, ClientRpcParams target = default)
    {
        ShowRewardPopup(amount);
    }

    private void ShowRewardPopup(int gold)
    {
        if (rewardUIRoot == null) return;

        if (_rewardUICg == null)
            _rewardUICg = rewardUIRoot.GetComponent<CanvasGroup>() ?? rewardUIRoot.AddComponent<CanvasGroup>();

        _rewardUICg.interactable = false;
        _rewardUICg.blocksRaycasts = false;

        if (rewardUITMP == null)
            rewardUITMP = rewardUIRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        if (rewardUITMP != null) rewardUITMP.text = $"미니게임 클리어! {gold}골드 획득";

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

    private void ApplyDim(bool on)
    {
        if (dimTargets == null || dimTargets.Length == 0)
            dimTargets = GetComponentsInChildren<Renderer>(true);
        if (dimTargets == null || dimTargets.Length == 0) return;

        if (_originalColors == null || _originalColors.Length != dimTargets.Length)
            _originalColors = new Color[dimTargets.Length];

        for (int i = 0; i < dimTargets.Length; i++)
        {
            var r = dimTargets[i];
            if (!r) continue;

            var mat = r.material;
            if (!mat.HasProperty("_Color")) continue;

            if (on)
            {
                if (!_dimmed) _originalColors[i] = mat.color;

                float k = Mathf.Clamp01(1f - dimAmount);
                var c0 = (_dimmed ? _originalColors[i] : mat.color);
                mat.color = new Color(c0.r * k, c0.g * k, c0.b * k, c0.a);
            }
            else
            {
                mat.color = _originalColors[i];
            }
        }

        _dimmed = on;
    }

    private void ShowCooldownPopup(int remainSec)
    {
        if (cooldownToastRoot == null) return;

        if (_toastCg == null) _toastCg = cooldownToastRoot.GetComponent<CanvasGroup>() ?? cooldownToastRoot.AddComponent<CanvasGroup>();
        _toastCg.interactable = false;
        _toastCg.blocksRaycasts = false;

        if (cooldownToastTMP != null)
            cooldownToastTMP.text = $"미니게임을 플레이 할 수 없습니다.\n미니게임 쿨타임이 {remainSec}초 남았습니다!";

        if (_toastCo != null) StopCoroutine(_toastCo);
        _toastCo = StartCoroutine(CoShowCooldownToast());
    }

    private System.Collections.IEnumerator CoShowCooldownToast()
    {
        cooldownToastRoot.SetActive(true);
        _toastCg.alpha = 0f;

        float t = 0f;
        while (t < toastFadeIn)
        {
            t += Time.unscaledDeltaTime;
            _toastCg.alpha = Mathf.SmoothStep(0f, 1f, t / toastFadeIn);
            yield return null;
        }
        _toastCg.alpha = 1f;

        yield return new WaitForSecondsRealtime(toastHold);

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
        if (_spawnedLocalUi && _spawnedLocalUi.activeSelf) return;

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
            if (rt2 != null) rt2.SetAsLastSibling();
        }
        else
        {
            if (!clientUiPrefab) { Debug.LogError("[MG] clientUiPrefab is NULL"); return; }

            Transform parent = uiRootOverride;
            if (parent == null && attachUiToMainCanvas)
            {
                var canvas = FindMainCanvas();
                if (canvas == null) { Debug.LogError("[MG] No Canvas found"); return; }
                parent = canvas.transform;
            }

            _spawnedLocalUi = Instantiate(clientUiPrefab, parent);
            _spawnedFromPrefab = true;

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
    }

    public void CloseUi()
    {
        if (_spawnedLocalUi == null) return;

        if (_spawnedFromPrefab) Destroy(_spawnedLocalUi);
        else _spawnedLocalUi.SetActive(false);

        _spawnedLocalUi = null;
    }

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