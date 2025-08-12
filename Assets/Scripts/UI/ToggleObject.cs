using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    public GameObject targetObject; // 토글할 대상

    public void ToggleActive()
    {
        if (!targetObject) return;
        targetObject.SetActive(!targetObject.activeSelf);
    }
}