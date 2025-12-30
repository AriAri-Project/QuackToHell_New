using Unity.Netcode;
using Unity.Collections;

public enum EWinType : byte
{
    Citizens,
    Mafia,
    RebelSolo
}

public struct ResultPlayerInfo : INetworkSerializable
{
    public FixedString64Bytes Name;
    public FixedString64Bytes Job;

    public ResultPlayerInfo(string name, string job)
    {
        Name = new FixedString64Bytes(name);
        Job = new FixedString64Bytes(job);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Job);
    }
}

/// <summary>
/// "간단 UI" 목표라서 Winner/Loser를 고정 슬롯으로 보냄.
/// (플레이어 최대 8명이면 0~7까지 늘리면 됨)
/// </summary>
public struct GameResultPayload : INetworkSerializable
{
    public EWinType WinType;
    public FixedString128Bytes WinReason;

    public bool HasWinner0; public ResultPlayerInfo Winner0;
    public bool HasWinner1; public ResultPlayerInfo Winner1;
    public bool HasWinner2; public ResultPlayerInfo Winner2;
    public bool HasWinner3; public ResultPlayerInfo Winner3;

    public bool HasLoser0; public ResultPlayerInfo Loser0;
    public bool HasLoser1; public ResultPlayerInfo Loser1;
    public bool HasLoser2; public ResultPlayerInfo Loser2;
    public bool HasLoser3; public ResultPlayerInfo Loser3;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref WinType);
        serializer.SerializeValue(ref WinReason);

        serializer.SerializeValue(ref HasWinner0); if (HasWinner0) serializer.SerializeValue(ref Winner0);
        serializer.SerializeValue(ref HasWinner1); if (HasWinner1) serializer.SerializeValue(ref Winner1);
        serializer.SerializeValue(ref HasWinner2); if (HasWinner2) serializer.SerializeValue(ref Winner2);
        serializer.SerializeValue(ref HasWinner3); if (HasWinner3) serializer.SerializeValue(ref Winner3);

        serializer.SerializeValue(ref HasLoser0); if (HasLoser0) serializer.SerializeValue(ref Loser0);
        serializer.SerializeValue(ref HasLoser1); if (HasLoser1) serializer.SerializeValue(ref Loser1);
        serializer.SerializeValue(ref HasLoser2); if (HasLoser2) serializer.SerializeValue(ref Loser2);
        serializer.SerializeValue(ref HasLoser3); if (HasLoser3) serializer.SerializeValue(ref Loser3);
    }
}
