using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering;

[System.Serializable]
public class SpawnPointGroup
{
    public string groupName;
    public Transform[] spawnPoints;
}

public class SpawnPointsController : NetworkBehaviour
{
    public static SpawnPointsController Instance { get; private set; }

    [SerializeField]
    private List<SpawnPointGroup> spawnPointGroups = new List<SpawnPointGroup>();

    // 서버에만 저장될, 각 클라이언트에게 할당된 스폰 포인트 딕셔너리
    private readonly Dictionary<ulong, Transform> _assignedSpawnPoints = new Dictionary<ulong, Transform>();
    #region 이벤트 구독
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // 서버만 '씬 로딩 완료' 이벤트를 구독한다.
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoadComplete;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // 오브젝트가 파괴될 때, 서버는 이벤트 구독을 반드시 해제한다 (메모리 누수 방지).
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleSceneLoadComplete;
        }
    }
    #endregion

    /// <summary>
    /// (서버 전용) 모든 클라이언트가 씬 로딩을 완료했을 때 자동으로 호출될 함수.
    /// </summary>
    private void HandleSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if(!IsHost)
        {
            return;
        }

        // 'VillageScene'이 로딩되었는지 확인한다.
        if (sceneName == "VillageScene")
        {
            Debug.Log("모든 클라이언트가 VillageScene 로딩 완료! 스폰 포인트 재배치를 시작합니다.");

            AssignAndRelocatePlayersAfterDelay();
        }
    }


    private void AssignAndRelocatePlayersAfterDelay()
    {
        if (!IsHost)
        {
            return;
        }

        AssignAllPlayerSpawnPoints();
        RelocateAllPlayers();

    }

    /// <summary>
    /// 할당된 위치로 모든 플레이어를 순간이동시킵니다.
    /// </summary>
    private void RelocateAllPlayers()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            var playerObject = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(clientId);
            var spawnPoint = GetSpawnPointForClient(clientId);

            if (playerObject != null && spawnPoint != null)
            {
                // 플레이어의 위치와 회전값을 직접 변경한다. 서버에서 변경하면 모든 클라이언트에 자동 동기화된다.
                playerObject.transform.position = spawnPoint.position;
                playerObject.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    /// <summary>
    /// 모든 클라이언트의 스폰 위치를 규칙에 맞게 할당합니다.
    /// </summary>
    private void AssignAllPlayerSpawnPoints()
    {
       

        _assignedSpawnPoints.Clear();
        //현재 연결된 모든 클라이언트의 ID 목록을 복사하여 할당
        var clientsToAssign = new List<ulong>(NetworkManager.Singleton.ConnectedClients.Keys);
        //리스트 순회 & 모든 스폰포인트 리스트화
        var allAvailablePoints = spawnPointGroups.SelectMany(g => g.spawnPoints).ToList();

        // 플레이어 수보다 스폰 포인트가 부족하면 경고
        if (allAvailablePoints.Count < clientsToAssign.Count)
        {
            Debug.LogError("플레이어 수에 비해 스폰 포인트가 부족합니다!");
            return;
        }

        // 규칙을 만족하는 유효한 배치를 찾을 때까지 반복 (안전장치)
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (TryAssign(clientsToAssign, spawnPointGroups))
            {
                Debug.Log("모든 플레이어의 스폰 포인트 할당 완료.");
                return; // 성공적으로 할당 완료
            }
        }

        Debug.LogError("규칙에 맞는 스폰 포인트 배치를 찾는 데 실패했습니다. 스폰 그룹 구성을 확인하세요.");
    }

    /// <summary>
    /// 규칙에 따라 스폰 포인트를 할당하는 로직
    /// </summary>
    private bool TryAssign(List<ulong> clients, List<SpawnPointGroup> groups)
    {
        _assignedSpawnPoints.Clear();
        var tempClients = new List<ulong>(clients);
        //group을 딕셔너리로 만들고, 키, value에 대해 처리. 키는 그대로 사용, value는 리스트로 변환
        var availablePointsByGroup = groups.ToDictionary(g => g, g => new List<Transform>(g.spawnPoints));

        // 1. 클라이언트와 그룹 목록을 무작위로 섞는다 (다양한 결과 생성)
        Shuffle(tempClients);
        var shuffledGroups = new List<SpawnPointGroup>(groups);
        Shuffle(shuffledGroups);

        // 2. 그룹을 순회하며 최소 조건(2명)을 만족시키도록 먼저 할당
        foreach (var group in shuffledGroups)
        {
            var pointsInGroup = availablePointsByGroup[group];
            Shuffle(pointsInGroup); // 그룹 내 스폰 위치도 랜덤화

            // 이 그룹에 최소 2명을 할당
            for (int i = 0; i < 2; i++)
            {
                if (tempClients.Count > 0 && pointsInGroup.Count > 0)
                {
                    _assignedSpawnPoints[tempClients[0]] = pointsInGroup[0];
                    tempClients.RemoveAt(0);
                    pointsInGroup.RemoveAt(0);
                }
            }
        }

        // 3. 남은 클라이언트들을 남은 아무 자리에나 할당
        // 남은 모든 스폰 포인트를 하나의 리스트로 합침
        var remainingPoints = availablePointsByGroup.Values.SelectMany(p => p).ToList();
        Shuffle(remainingPoints);

        while (tempClients.Count > 0 && remainingPoints.Count > 0)
        {
            _assignedSpawnPoints[tempClients[0]] = remainingPoints[0];
            tempClients.RemoveAt(0);
            remainingPoints.RemoveAt(0);
        }

        // 4. 할당이 모두 완료되었는지, 규칙을 만족하는지 최종 검증
        if (tempClients.Count > 0) return false; // 모든 클라이언트가 할당되지 못함

        
        var groupCounts = _assignedSpawnPoints.Values//트랜스폼 가져오기
            .GroupBy(t => groups.First(g => g.spawnPoints.Contains(t)))//모든 트랜스폼에 대해, 그룹을 찾아 그룹별로 묶기. 
            .ToDictionary(g => g.Key, g => g.Count());//각 그룹별로 몇명이 있는지 세기

        // 사용된 그룹 중 플레이어가 1명만 있는 그룹이 있는지 확인
        foreach (var count in groupCounts.Values)
        {
            if (count == 1) return false; // 규칙 위반!
        }

        return true; // 유효한 배치
    }

    /// <summary>
    /// 특정 클라이언트에게 할당된 스폰 포인트를 반환합니다.
    /// 서버 권한을 가진 코드(예: if (IsServer) 블록 또는 ServerRpc 내부)에서만 호출해야 올바른 결과를 보장받을 수 있습니다.
    /// </summary>
    private Transform GetSpawnPointForClient(ulong clientId)
    {
        if (_assignedSpawnPoints.TryGetValue(clientId, out Transform spawnPoint))
        {
            return spawnPoint;
        }

        // 만약 할당된 포인트가 없다면, 안전하게 첫번째 포인트를 반환
        Debug.LogWarning($"{clientId} 에게 할당된 스폰 포인트가 없어 기본 위치를 사용합니다.");
        return spawnPointGroups.Count > 0 && spawnPointGroups[0].spawnPoints.Length > 0 ? spawnPointGroups[0].spawnPoints[0] : null;
    }

    // 리스트를 무작위로 섞는 유틸리티 함수
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
