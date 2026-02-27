using UnityEngine;
using Unity.Netcode;
using System;

namespace Court
{
    public class PlayerTrialState : NetworkBehaviour
    {
        // 네트워크 변수로 상태 동기화 (모든 클라이언트가 알 수 있음)
        public NetworkVariable<bool> HasEndedSpeech = new NetworkVariable<bool>(false);

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[PlayerTrialState] OnNetworkSpawn - OwnerClientId:{OwnerClientId}, IsOwner:{IsOwner}");

            // 내가 로컬 플레이어라면, 매니저에 나를 등록
            if (IsOwner)
            {
                if (TrialManager.Instance != null)
                    TrialManager.Instance.SetLocalPlayer(this);
            }
            
            // 서버/클라이언트 모두 매니저의 전체 리스트에 등록
            TrialManager.Instance.RegisterPlayer(this);
        }
        
        public override void OnNetworkDespawn()
        {
            Debug.Log($"[PlayerTrialState] OnNetworkDespawn - OwnerClientId:{OwnerClientId}, IsOwner:{IsOwner}");
            TrialManager.Instance.UnregisterPlayer(this);
        }

        /// <summary>
        /// 발언 마치기 카드 사용 시 호출 (클라이언트 -> 서버)
        /// </summary>
        public void EndSpeech()
        {
            Debug.Log($"[PlayerTrialState] EndSpeech 호출 - OwnerClientId:{OwnerClientId}, IsOwner:{IsOwner}, HasEndedSpeech:{HasEndedSpeech.Value}");

            if (IsOwner && !HasEndedSpeech.Value)
            {
                EndSpeechServerRpc();
            }
        }

        [ServerRpc]
        private void EndSpeechServerRpc()
        {
            HasEndedSpeech.Value = true;
            Debug.Log($"[Server] 플레이어 {OwnerClientId} 발언 종료. HasEndedSpeech:{HasEndedSpeech.Value}");
            
            // 전체 종료 체크
            TrialManager.Instance.CheckAllPlayersEnded();
        }
    }
}