using UnityEngine;
using UnityEngine.SceneManagement;

public class CourtSceneMove : MonoBehaviour
{

    public void CourtScenesCtrl()
    {
        SceneManager.LoadScene("Court");
        Debug.Log("Court Scenes Moved");
    }
}
