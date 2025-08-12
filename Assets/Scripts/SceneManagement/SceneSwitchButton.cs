using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneSwitchButton : MonoBehaviour
{
    [SerializeField] string sceneName;
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
        // NetSceneController 준비 전엔 못 누르게
        button.interactable = false;
    }

    void Update()
    {
        // NetSceneController가 Spawn되면 누를 수 있게
        if (!button.interactable && NetSceneController.Instance != null)
            button.interactable = true;
    }

    void OnClick()
    {
        if (NetSceneController.Instance != null)
            NetSceneController.Instance.SwitchSceneFromUI(sceneName);
        else
            Debug.LogWarning("[SceneSwitchButton] NetSceneController.Instance is null");
    }
}
