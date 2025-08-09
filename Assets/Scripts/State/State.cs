using UnityEngine;

/// <summary>
/// 상태 추상클래스
/// 각 상태들은 State를 상속받아야 함 & 상속받은 구체적 클래스를 컴포넌트로 부착한 프리팹이 존재해야 함
/// </summary>
public abstract class State : MonoBehaviour
{
    public abstract void OnStateEnter();
    public abstract void OnStateUpdate();
    public abstract void OnStateExit();
}
