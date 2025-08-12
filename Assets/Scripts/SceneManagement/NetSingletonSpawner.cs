using Unity.Netcode;
using UnityEngine;

public class NetSingletonSpawner : MonoBehaviour
{
    [SerializeField] NetSceneController controllerPrefab;

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    void OnServerStarted()
    {
        // 서버(호스트 포함)에서만 한 번 스폰
        if (!NetworkManager.Singleton.IsServer) return;

        // 이미 존재하면 중복 스폰 방지
        if (NetSceneController.Instance != null) return;

        var inst = Instantiate(controllerPrefab);
        // NetworkObject가 반드시 있어야 함!
        var no = inst.GetComponent<NetworkObject>();
        no.Spawn(); // 네트워크에 브로드캐스트(클라에도 자동 생성)

        // 이제 모든 참가자에게 NetSceneController가 생김 → RPC 호출 가능
    }
}
