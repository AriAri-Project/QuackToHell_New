using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [SerializeField] string nextScene = "Village"; // Village로 이동
    void Start() => StartCoroutine(Go());

    IEnumerator Go()
    {
        // NetworkRoot 같은 싱글톤들이 Awake를 끝내도록 한 프레임 대기
        yield return null;
        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }
}
