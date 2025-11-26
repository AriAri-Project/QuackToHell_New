using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ClickOncePointsGame : MonoBehaviour
{
    [Header("UI Wiring")]
    [SerializeField] private Button closeButton;     // 우상단 닫기 버튼
    [SerializeField] private Transform contentArea;  // 스팟이 들어갈 부모

    [Header("Cursor")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = new Vector2(16, 16);
    [SerializeField] private bool changeCursorOnOpen = true;

    [Header("Game Rule")]
    [Tooltip("기본: 순서 상관없이 모든 스팟 클릭 → 클리어")]
    [SerializeField] private bool requireOrder = false; // true면 순서대로만 유효
    [Tooltip("Auto-Collect: ContentArea 하위의 ClickSpotUI를 자동 수집")]
    [SerializeField] private bool autoCollectSpots = true;

    [Tooltip("비워두면 자동 수집, 지정하면 이 리스트 기준으로 순서/체크")]
    [SerializeField] private List<ClickSpotUI> spots = new List<ClickSpotUI>();

    [Header("Events")]
    public UnityEvent OnCleared;       // 클리어 시
    public UnityEvent OnCanceled;      // 닫기 등으로 취소 시
    public UnityEvent OnWrongOrder;    // 순서 틀렸을 때(선택적 연출용)

    private int _nextOrderIndex;       // requireOrder=true일 때 다음 클릭해야 할 order
    private int _clickedCount;
    private bool _running;
    private Texture2D _prevCursor;
    private Vector2 _prevHotspot;
    private CursorMode _prevMode;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(CloseUI);
    }

    void OnEnable()
    {
        // 초기화
        SetupSpots();
        ResetGame();

        // 커서 교체
        if (changeCursorOnOpen && cursorTexture != null)
        {
            CacheCurrentCursor();
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }

        _running = true;
    }

    void OnDisable()
    {
        // 커서 복원
        if (changeCursorOnOpen && cursorTexture != null)
        {
            RestoreCursor();
        }
        _running = false;
    }

    private void SetupSpots()
    {
        if (autoCollectSpots && contentArea != null)
        {
            spots.Clear();
            contentArea.GetComponentsInChildren(true, spots);
        }

        // 각 스팟에게 매니저 주입
        foreach (var s in spots)
        {
            if (s == null) continue;
            s.Init(this);
        }
    }

    private void ResetGame()
    {
        _clickedCount = 0;
        _nextOrderIndex = 0;

        foreach (var s in spots)
        {
            if (s == null) continue;
            s.ResetState();
        }
    }

    internal void NotifySpotClicked(ClickSpotUI spot)
    {
        if (!_running) return;
        if (spot == null || spot.WasClicked) return;

        // 순서 체크
        if (requireOrder)
        {
            if (spot.OrderIndex != _nextOrderIndex)
            {
                // 틀린 순서 → 무효(원하면 실패 처리/효과음 등)
                OnWrongOrder?.Invoke();
                spot.PlayWrongOrderFeedback();
                return;
            }
        }

        // 유효 클릭 처리
        spot.MarkClicked();
        _clickedCount++;
        _nextOrderIndex++;

        // 모든 스팟 클릭 완료 → 클리어
        if (_clickedCount >= spots.Count)
        {
            Finish(success: true);
        }
    }

    public void CloseUI() // 닫기 버튼에서 호출
    {
        Finish(success: false, canceled: true);
    }

    private void Finish(bool success, bool canceled = false)
    {
        if (!_running) return;
        _running = false;

        if (changeCursorOnOpen && cursorTexture != null)
            RestoreCursor();

        if (success) OnCleared?.Invoke();
        else if (canceled) OnCanceled?.Invoke();

        // UI 닫기(Panel 비활성화를 추천)
        gameObject.SetActive(false);
    }

    private void CacheCurrentCursor()
    {
        // Unity는 현재 커서 Texture를 직접 읽는 API가 없어 캐싱은 의미적으로만 보관
        _prevCursor = null;
        _prevHotspot = Vector2.zero;
        _prevMode = CursorMode.Auto;
        // 필요하면 프로젝트 규칙에 맞춰 '기본 커서 텍스처'를 스크립터블 오브젝트 등으로 보관
    }

    private void RestoreCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, _prevMode);
    }

    // 편의 API
    public void SetRequireOrder(bool on) => requireOrder = on;
    public void SetCursor(Texture2D tex, Vector2 hotspot)
    {
        cursorTexture = tex;
        cursorHotspot = hotspot;
        if (_running && changeCursorOnOpen && tex != null)
            Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
    }
}
