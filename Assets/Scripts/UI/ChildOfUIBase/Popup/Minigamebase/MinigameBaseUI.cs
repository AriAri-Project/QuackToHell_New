using System;
using UnityEngine;
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
