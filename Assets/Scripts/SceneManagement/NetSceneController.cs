using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetSceneController : NetworkBehaviour
{
    public static NetSceneController Instance;

    public override void OnNetworkSpawn()
    {
        // 네트워크로 Spawn된 뒤에 싱글톤 세팅
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 넘어가도 유지
        base.OnNetworkSpawn();
    }

    public void SwitchSceneFromUI(string sceneName)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            RequestSwitchServerRpc(sceneName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestSwitchServerRpc(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
