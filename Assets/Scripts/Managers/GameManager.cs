using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

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

    public void PlayerSpawnTestButton_OnPlayerSpawn()
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
