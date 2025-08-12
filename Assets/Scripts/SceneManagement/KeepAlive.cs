using UnityEngine;

public class KeepAlive : MonoBehaviour
{
    private static bool created = false;

    void Awake()
    {
        if (created) { Destroy(gameObject); return; }
        created = true;
        DontDestroyOnLoad(gameObject);
    }
}