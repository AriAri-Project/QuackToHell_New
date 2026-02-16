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
                // ★ [수정 1] 접속 시 초기 점수 1점 부여
                VoteDataList.Add(new VoteData { clientId = clientId, count = 1 });
            }
        }

        private void InitializeVoters()
        {
            VoteDataList.Clear();
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                // ★ [수정 2] 초기화 시 점수 1점 부여
                VoteDataList.Add(new VoteData { clientId = client.ClientId, count = 1 });
            }
        }

        public void AddVote(ulong targetClientId, int delta)
        {
            if (!IsServer) return;

            int index = GetPlayerIndex(targetClientId);
            if (index != -1)
            {
                VoteData data = VoteDataList[index];
                int oldScore = data.count;
                int newScore = oldScore + delta;
                
                // 0점 미만 방지
                if (newScore < 0) newScore = 0;

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

        public bool TryGetTopVoted(out ulong executedClientId, out int topCount, out bool isTie)
        {
            executedClientId = 0UL;
            topCount = int.MinValue;
            isTie = false;

            if (VoteDataList == null || VoteDataList.Count == 0)
                return false;

            // 최고 득표수 찾기
            for (int i = 0; i < VoteDataList.Count; i++)
            {
                var v = VoteDataList[i];
                if (v.count > topCount)
                {
                    topCount = v.count;
                    executedClientId = v.clientId;
                    isTie = false;
                }
                else if (v.count == topCount)
                {
                    // 동점 발생
                    isTie = true;

                }
            }

            return true;
        }

        public void ResetVotes(int initialCount = 1)
        {
            if (!IsServer) return;

            for (int i = 0; i < VoteDataList.Count; i++)
            {
                var data = VoteDataList[i];
                data.count = initialCount;
                VoteDataList[i] = data;
            }

            Debug.Log($"[VoteModel] ResetVotes 완료. initialCount={initialCount}");
        }

    }
}