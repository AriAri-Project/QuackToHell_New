using UnityEngine;

public class ActiveController : MonoBehaviour
{
    public void DeactivateGameObject()
    {
        gameObject.SetActive(false);
    }

    public void ActivateGameObject()
    {
        gameObject.SetActive(true);
    }
}
