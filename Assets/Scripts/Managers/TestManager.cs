using UnityEngine;

public class TestManager : MonoBehaviour
{
    private static TestManager _instance;
    public static TestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TestManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TestManager");
                    _instance = obj.AddComponent<TestManager>();
                }
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}
