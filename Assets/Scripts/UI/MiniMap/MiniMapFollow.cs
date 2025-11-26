using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public static MinimapFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    // 2D니까 Z만 뒤로 빼기
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private void Awake()
    {
        Instance = this;   // 씬에 하나만 있다고 가정
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }
}

