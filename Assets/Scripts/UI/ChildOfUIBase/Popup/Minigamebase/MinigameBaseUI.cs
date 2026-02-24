using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public abstract class MinigameBaseUI : UIPopup
{
    public UnityEvent OnCleared;       // 클리어 시
    public UnityEvent OnCanceled;      // 닫기 등으로 취소 시
    
    /// <summary>
    /// 게임 재시작을 위한 초기화 함수
    /// </summary>
    protected abstract void Initialize();
    
    /// <summary>
    /// 게임 클리어 시 실행될 내용 구현
    /// </summary>
    protected abstract void OnGameComplete();

    protected void Awake()
    {
        SetXButton();
    }

    private void SetXButton()
    {
        // 1. 오브젝트 생성 및 부모 설정
        GameObject closeBtnObj = new GameObject("CloseButton");
        // false를 줘야 UI 스케일이 부모에 맞춰지면서 크기가 망가지지 않습니다.
        closeBtnObj.transform.SetParent(this.transform, false);

        // 2. 50x50 크기 생성 및 앵커 우상단 설정
        RectTransform rect = closeBtnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50f, 50f);
        
        // 앵커(Anchor)와 피벗(Pivot)을 모두 우상단(1, 1)으로 세팅
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        // 화면 완전 구석에 딱 붙으면 안 예쁘니까 안쪽으로 살짝 띄워줍니다. (필요 없으면 0, 0)
        rect.anchoredPosition = new Vector2(-20f, -20f);

        // 3. 이미지 로드 및 적용
        Image img = closeBtnObj.AddComponent<Image>();
        Sprite xIcon = Resources.Load<Sprite>("Sprites/XButton/icon_cross");
        if (xIcon != null)
        {
            img.sprite = xIcon;
        }
        else
        {
            Debug.LogError("[MinigameBaseUI] X버튼 이미지를 찾을 수 없습니다! 경로: Sprites/XButton/icon_cross");
        }

        // 4. 버튼 컴포넌트 추가 및 팝업 닫기 이벤트 바인딩
        Button btn = closeBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => 
        {
            gameObject.SetActive(false);
        });
    }

    private void OnEnable()
    {
        Initialize();
    }
    
    private void OnDisable()
    {
        OnCanceled?.Invoke();
    }

    /// <summary>
    /// 하위클래스가 게임 클리어 시 호출해야하는 함수
    /// </summary>
    protected void FinishGame()
    {
        // 1. 이벤트 무조건 발생 
        OnCleared?.Invoke();

        // 2. 하위 클래스의 커스텀 로직 실행
        OnGameComplete();
    }
}
