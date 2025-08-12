using UnityEngine;
using UnityEngine.SceneManagement;
public class VillageSceneMove : MonoBehaviour
{
    public void VillageScenesCtrl()
    {
        SceneManager.LoadScene("Village");
        Debug.Log("Village Scenes Moved");
    }
}
