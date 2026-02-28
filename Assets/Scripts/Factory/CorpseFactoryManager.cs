// CorpseFactory.cs
using Unity.Netcode;
using UnityEngine;

// 시체 생성을 전담하는 팩토리 클래스 (싱글톤)
public class CorpseFactory : MonoBehaviour
{
    #region 싱글톤
    #region 싱글톤 코드
    //싱글톤 코드
    private static CorpseFactory _instance;
    public static CorpseFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<CorpseFactory>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DeckManager");
                    _instance = go.AddComponent<CorpseFactory>();
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
    #endregion

    #endregion

    [SerializeField]
    private GameObject _corpsePrefab;


    [ServerRpc]
    public void CreateCorpseServerRpc(Vector3 position, Quaternion rotation, PlayerAppearanceData victimAppearanceData, ulong clientId)
    {
        
        // 1. 시체 인스턴스화
        GameObject corpseInstance = Instantiate(_corpsePrefab, position, rotation);
        
        // 2. 데이터 초기화
        PlayerCorpse playerCorpse = corpseInstance.GetComponent<PlayerCorpse>();
        playerCorpse.AppearanceData.Value = victimAppearanceData;
        Debug.Log($"[CorpseFactoryManager] 생성되는 시체 cliend id: {clientId}");
        playerCorpse.ClientId = clientId;
        
        // 3. 네트워크에 스폰
        NetworkObject corpseNetworkObject = corpseInstance.GetComponent<NetworkObject>();
        corpseNetworkObject.Spawn(true);
    }
}