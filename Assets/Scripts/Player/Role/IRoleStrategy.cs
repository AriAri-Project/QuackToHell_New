using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 역할별 행동 전략 인터페이스
/// State 패턴과 유사하게 의존성 주입 방식으로 관리
///
/// 1. 키 입력(view) / 키보드입력(SkillButtonsUI)
//2. 키 입력 시, 
    //0. 외부 인터페이스: Kill/ Report등...
    //1. Can으로 조건검사: ServerRpc. Kill/Report등의 외부인터페이스가 호출하는 함수
    //2. 결과를 전송: ClinetRpc(Can인지 False인지) 
    //3. 그 결과에 따라 Kill/Report등.. 해야할 일을 수행하기: ServerRpc
/// </summary>
public interface IRoleStrategy
{
    /// <summary>
    /// 역할별 초기 설정 (Action Map, UI 등)
    /// </summary>
    void Setup();
    
    
    /// <summary>
    /// 역할별 업데이트 로직
    /// </summary>
    void Update();
    
    /// <summary>
    /// 역할별 정리 작업
    /// </summary>
    void Cleanup();
    
    // Ability 메서드들 (모든 역할이 구현해야 함)
    public void Kill(ulong targetNetworkObjectId); 
    [ServerRpc(RequireOwnership = false)]
    void KillServerRpc(ulong targetNetworkObjectId,ServerRpcParams rpcParams = default);
    [ServerRpc(RequireOwnership = false)]
    void CanKillServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default);  // 서버에서 조건 검사
    [ClientRpc]
    void CanKillResultClientRpc(bool canKill, ulong targetNetworkObjectId, ClientRpcParams rpcParams = default);  // 결과 전송


    public void ReportCorpse(ulong corpseClientId);

    [ServerRpc(RequireOwnership = false)]
    void CanReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default); // 서버에서 조건 검사
    
    [ClientRpc]
    void CanReportResultClientRpc(bool canReport, ulong corpseClientId, ClientRpcParams rpcParams = default);  // 결과 전송
    [ServerRpc(RequireOwnership = false)]
    void ReportServerRpc(ulong corpseClientId, ServerRpcParams rpcParams = default);



    public void Savotage(SabotageType  sabotageType);

    [ServerRpc(RequireOwnership = false)]
    void CanSavotageServerRpc(SabotageType  sabotageType, ServerRpcParams rpcParams = default); // 서버에서 조건 검사
    
    [ClientRpc]
    void CanSavotageResultClientRpc(SabotageType  sabotageType, bool canSabotage, ClientRpcParams rpcParams = default);  // 결과 전송
    [ServerRpc(RequireOwnership = false)]
    void SavotageServerRpc(SabotageType  sabotageType, ServerRpcParams rpcParams = default);


    
    /// <param name="targetNetworkObjectId">0: 없음</param>
    public void Interact(string targetTag,  ulong targetNetworkObjectId = 0);

    /// <param name="targetNetworkObjectId">0: 없음</param>
    [ServerRpc(RequireOwnership = false)]
    void CanInteractServerRpc(string targetTag,  ulong targetNetworkObjectId = 0, ServerRpcParams rpcParams = default); // 서버에서 조건 검사
    
    /// <param name="targetNetworkObjectId">0: 없음</param>
    [ClientRpc]
    void CanInteractResultClientRpc(bool canInteract, string targetTag, ulong targetNetworkObjectId = 0, ClientRpcParams rpcParams = default);  // 결과 전송
    /// <param name="targetNetworkObjectId">0: 없음</param>
    [ServerRpc(RequireOwnership = false)]
    void InteractServerRpc(string targetTag,  ulong targetNetworkObjectId = 0, ServerRpcParams rpcParams = default);


}