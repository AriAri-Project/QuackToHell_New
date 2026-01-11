using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace Court
{
    public class VoteModel : NetworkBehaviour
    {
        public static VoteModel Instance => SingletonHelper<VoteModel>.Instance;

        // Presenter가 구독할 수 있도록 공개 (읽기 전용)
        public NetworkList<VoteData> VoteDataList => _voteDataList;
        
        private NetworkList<VoteData> _voteDataList;

        private void Awake()
        {
            SingletonHelper<VoteModel>.InitializeSingleton(this, false);
            _voteDataList = new NetworkList<VoteData>(
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Server
            );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeVoteList();
            }
        }

        private void InitializeVoteList()
        {
            _voteDataList.Clear();
            // 전체 플레이어 수만큼 슬롯 생성 (초기값 1)
            int playerSize = PlayerHelperManager.Instance.GetAllPlayers<NetworkBehaviour>().Length;
            for (int i = 0; i < playerSize; i++)
            {
                _voteDataList.Add(new VoteData { count = 1 });
            }
        }

        // --- 서버 로직 ---

        /// <summary>
        /// (서버) 특정 플레이어(ClientId)에게 투표수 추가
        /// </summary>
        public void AddVote(ulong targetClientId, int amount)
        {
            if (!IsServer) return;

            int index = GetPlayerIndex(targetClientId);
            if (index != -1)
            {
                // NetworkList는 구조체 값 수정 시, 다시 대입해야 변경 감지됨
                VoteData data = _voteDataList[index];
                data.count += amount;
                _voteDataList[index] = data; 
                
                Debug.Log($"[VoteModel] {targetClientId}번(Index:{index}) 투표수 증가: +{amount} (총 {data.count})");
            }
        }

        /// <summary>
        /// ClientId로 리스트 내 인덱스를 찾는 헬퍼 함수
        /// </summary>
        public int GetPlayerIndex(ulong clientId)
        {
            var players = PlayerHelperManager.Instance.GetAllPlayers<NetworkBehaviour>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].OwnerClientId == clientId) return i;
            }
            return -1;
        }

        public int GetVoteCount(int index)
        {
            if (index >= 0 && index < _voteDataList.Count) return _voteDataList[index].count;
            return 0;
        }
    }
}

[System.Serializable]
public struct VoteData : INetworkSerializable, IEquatable<VoteData>
{
    public int count;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter => serializer.SerializeValue(ref count);
    public bool Equals(VoteData other) => count == other.count;
}