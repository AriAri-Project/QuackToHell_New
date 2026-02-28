using UnityEngine;
using Unity.Netcode;
using System;

namespace Court
{
    public struct VoteData : INetworkSerializable, IEquatable<VoteData>
    {
        public ulong clientId;
        public int count;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref count);
        }

        public bool Equals(VoteData other) => clientId == other.clientId && count == other.count;
    }

    public class VoteModel : NetworkBehaviour
    {
        public static VoteModel Instance;

        public NetworkList<VoteData> VoteDataList;

        private void Awake()
        {
            Instance = this;
            VoteDataList = new NetworkList<VoteData>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeVoters();
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (GetPlayerIndex(clientId) == -1)
            {
                VoteDataList.Add(new VoteData { clientId = clientId, count = 1 });
            }
        }

        private void InitializeVoters()
        {
            VoteDataList.Clear();
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                VoteDataList.Add(new VoteData { clientId = client.ClientId, count = 1 });
            }
        }

        public void AddVote(ulong targetClientId, int delta, bool allowZero = false)
        {
            if (!IsServer) return;

            int index = GetPlayerIndex(targetClientId);
            if (index != -1)
            {
                VoteData data = VoteDataList[index];
                int oldScore = data.count;
                int newScore = oldScore + delta;
                
                // x0 조합이 아닐 때 최저 득표수는 1
                int minScore = allowZero ? 0 : 1;
                if (newScore < minScore) newScore = minScore;

                data.count = newScore;
                VoteDataList[index] = data;

                Debug.Log($"[VoteModel] 득표수 갱신! 타겟:{targetClientId}, 변동:{delta}, 최종:{newScore}");
            }
            else
            {
                Debug.LogWarning($"[VoteModel] 타겟({targetClientId})을 리스트에서 찾을 수 없습니다.");
            }
        }

        public int GetPlayerIndex(ulong clientId)
        {
            for (int i = 0; i < VoteDataList.Count; i++)
            {
                if (VoteDataList[i].clientId == clientId) return i;
            }
            return -1;
        }

        public int GetVoteCount(int index)
        {
            if (index >= 0 && index < VoteDataList.Count)
            {
                return VoteDataList[index].count;
            }
            return 0;
        }
        //초기화 함수
        public void Initialize()
        {
            if (!IsServer) return;
            VoteDataList.Clear();
        }
    }
}