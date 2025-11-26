using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class AreaClickGame : MonoBehaviour
{
    [Header("SFX")]
    public AudioSource clickSFX;
    
    [Header("UI Wiring")]
    [SerializeField] private Button closeButton;        // 우상단 닫기
    [SerializeField] private Transform contentArea;     // 클릭 영역 부모
    [SerializeField] private Text counterText;          // (선택) 진행 표시

    [Header("Cursor")]
    [SerializeField] private Texture2D cursorTexture;   // 테마 커서 (기획 입력)
    [SerializeField] private Vector2 cursorHotspot = new Vector2(16, 16);
    [SerializeField] private bool changeCursorOnOpen = true;

    [Header("Rule")]
    [Min(1)]
    [SerializeField] private int targetClicks = 10;     // N (기획자가 조정)
    [SerializeField] private float cooldownSeconds = 0.2f; // 0.2s 미집계 구간
    [Tooltip("ContentArea 하위에서 자동으로 AreaClickRegion을 찾음")]
    [SerializeField] private bool autoCollectRegion = true;
    [SerializeField] private AreaClickRegion region;    // 직접 지정도 가능

    [Header("Events")]
    public UnityEvent OnCleared;    // N회 달성
    public UnityEvent OnCanceled;   // 닫기 버튼 등

    private int _count;
    private float _nextAllowedTime;
    private bool _running;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Cancel);
    }

    private void OnEnable()
    {
        Debug.Log($"[Game] start region={(region ? region.name : "NULL")} target={targetClicks}");

        if (autoCollectRegion && contentArea != null && region == null)
            region = contentArea.GetComponentInChildren<AreaClickRegion>(true);

        if (region == null)
        {
            Debug.LogError("[AreaClickGame] ClickRegion(AreaClickRegion)이 필요합니다.", this);
            return;
        }

        // 구독
        region.OnRegionClicked -= HandleRegionClicked;
        region.OnRegionClicked += HandleRegionClicked;

        // 초기화
        _count = 0;
        _nextAllowedTime = 0f;
        _running = true;
        UpdateCounterUI();

        // 커서 교체
        if (changeCursorOnOpen && cursorTexture != null)
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    private void OnDisable()
    {
        if (region != null)
            region.OnRegionClicked -= HandleRegionClicked;

        if (changeCursorOnOpen && cursorTexture != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        _running = false;
    }

    private void HandleRegionClicked(Vector2 screenPos)
    {
        if (!_running) return;

        // 쿨다운: 마지막 유효 클릭 이후 cooldownSeconds 동안 미집계
        if (Time.unscaledTime < _nextAllowedTime)
            return;
        //효과음
        SoundManager.Instance.SFXPlay(clickSFX.name, clickSFX.clip);
        _count++;
        _nextAllowedTime = Time.unscaledTime + cooldownSeconds;
        UpdateCounterUI();

        Debug.Log($"[Game] count={_count}/{targetClicks}");

        if (_count >= targetClicks)
            Finish(true);
    }

    private void UpdateCounterUI()
    {
        if (counterText)
            counterText.text = $"{_count} / {targetClicks}";
    }

    private void Cancel() => Finish(false);

    private void Finish(bool success)
    {
        if (!_running) return;
        _running = false;

        if (changeCursorOnOpen && cursorTexture != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (success) OnCleared?.Invoke();
        else OnCanceled?.Invoke();

        // Panel 루트를 끄면 MinigameController의 CloseUi 이벤트에 의해 정리됨
        gameObject.SetActive(false);
    }

    // 기획 편의용 메서드
    public void SetTargetClicks(int n) => targetClicks = Mathf.Max(1, n);
    public void SetCooldown(float sec) => cooldownSeconds = Mathf.Max(0f, sec);
}
