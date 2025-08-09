using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{


    //싱글톤 코드
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //persistent씬에서 시작해서 바로 홈씬으로 전환
        SceneManager.LoadScene("HomeScene");
    }

    public void OnJoinAsClientButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        
        // 클라이언트로 세션에 참여
        NetworkManager.Singleton.StartClient();
    }

    public void OnJoinAsHostButton()
    {
        // 네트워크 연결 완료 이벤트 구독
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        
        // 호스트(서버+클라이언트)로 세션 생성 및 참여
        NetworkManager.Singleton.StartHost();
    }

    private void OnClientConnected(ulong clientId)
    {
        // 자신의 클라이언트가 연결되었을 때만 플레이어 스폰
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 이벤트 구독 해제 (한 번만 실행되도록)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            
            PlayerSpawn();
        }
    }

    private void PlayerSpawn()
    {

        PlayerFactory playerFactory = FindObjectOfType<PlayerFactory>();
        if (playerFactory != null)
        {
                playerFactory.SpawnPlayerServerRpc();
        }
        else
        {
            Debug.LogError("PlayerFactory not found in the scene.");
        }
    }

}
