using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerMinimapAssigner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // 내 로컬 플레이어가 아니면 패스
        if (!IsOwner) return;

        // MinimapFollow가 씬에 뜰 때까지 한 프레임씩 기다렸다가 타겟 세팅
        StartCoroutine(AssignWhenReady());
    }

    private IEnumerator AssignWhenReady()
    {
        // MinimapFollow.Instance 생길 때까지 기다리기
        while (MinimapFollow.Instance == null)
            yield return null;

        MinimapFollow.Instance.SetTarget(transform);
        Debug.Log($"[Minimap] Target set: {gameObject.name}");
    }
}
