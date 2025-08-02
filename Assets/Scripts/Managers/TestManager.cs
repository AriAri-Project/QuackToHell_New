using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance;
    /// <summary>
    /// 싱글턴 인스턴스를 초기화하고, 중복 인스턴스가 생성될 경우 현재 게임 오브젝트를 파괴합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }


}
