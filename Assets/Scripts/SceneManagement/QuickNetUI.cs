using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickNetUI : MonoBehaviour
{
    [SerializeField] string firstScene = "Village"; // Host가 세션 시작 후 로드할 씬
    [SerializeField] string address = "127.0.0.1";  // 로컬 테스트용
    [SerializeField] ushort port = 7777;

    void ApplyTransport()
    {
        var utp = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (utp != null) utp.SetConnectionData(address, port);
    }

    public void StartHost()
    {
        ApplyTransport();
        NetworkManager.Singleton.StartHost();
        // 서버(호스트)만 네트워크 씬 로드 → 모두 동기 전환
        NetworkManager.Singleton.SceneManager.LoadScene(firstScene, LoadSceneMode.Single);
    }

    public void StartClient()
    {
        ApplyTransport();
        NetworkManager.Singleton.StartClient();
    }

    public void Shutdown() => NetworkManager.Singleton.Shutdown();
}
