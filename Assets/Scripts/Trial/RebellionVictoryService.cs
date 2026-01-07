using UnityEngine;
using Unity.Netcode;

public static class RebellionVictoryService
{
    public static bool TryTriggerRebellion(ulong winnerClientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return false;

        var winnerGO = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(winnerClientId);
        if (winnerGO == null) return false;

        var inventory = winnerGO.GetComponent<CardInventoryModel>();
        if (inventory == null || !inventory.HasAllRevoltCards())
            return false;

        // 반란승 확정
        GameResultPayload payload = BuildPayload(winnerClientId);

        ResultBroadcaster broadcaster = Object.FindFirstObjectByType<ResultBroadcaster>();
        if (broadcaster == null)
        {
            Debug.LogError("[RebellionVictoryService] ResultBroadcaster not found!");
            return false;
        }

        broadcaster.EndGameAndShowResult(payload);
        return true;
    }

    private static GameResultPayload BuildPayload(ulong winnerClientId)
    {
        GameResultPayload p = new GameResultPayload
        {
            WinType = EWinType.RebelSolo,
            WinReason = "반란 카드 3장 수집 후 재판 소집"
        };

        int winnerIndex = 0;
        int loserIndex = 0;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            ulong cid = kv.Key;

            PlayerModel model = PlayerHelperManager.Instance.GetPlayerModelByClientId(cid);
            if (model == null) continue;

            // 이름은 가져오고, 직업은 일단 Unknown
            string name = model.PlayerStatusData.Value.Nickname.ToString();
            var info = new ResultPlayerInfo(name, "Unknown");

            if (cid == winnerClientId)
                AssignWinner(ref p, winnerIndex++, info);
            else
                AssignLoser(ref p, loserIndex++, info);
        }

        return p;
    }

    private static void AssignWinner(ref GameResultPayload p, int idx, ResultPlayerInfo info)
    {
        if (idx == 0) { p.HasWinner0 = true; p.Winner0 = info; }
        else if (idx == 1) { p.HasWinner1 = true; p.Winner1 = info; }
        else if (idx == 2) { p.HasWinner2 = true; p.Winner2 = info; }
        else if (idx == 3) { p.HasWinner3 = true; p.Winner3 = info; }
    }

    private static void AssignLoser(ref GameResultPayload p, int idx, ResultPlayerInfo info)
    {
        if (idx == 0) { p.HasLoser0 = true; p.Loser0 = info; }
        else if (idx == 1) { p.HasLoser1 = true; p.Loser1 = info; }
        else if (idx == 2) { p.HasLoser2 = true; p.Loser2 = info; }
        else if (idx == 3) { p.HasLoser3 = true; p.Loser3 = info; }
    }
}
