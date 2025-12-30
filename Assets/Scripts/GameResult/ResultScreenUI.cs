using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ResultScreenUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root; // ResultPanel

    [Header("Texts")]
    [SerializeField] private Text winnersText;
    [SerializeField] private Text losersText;
    [SerializeField] private Text reasonText;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Open(GameResultPayload payload)
    {
        if (root != null)
            root.SetActive(true);

        winnersText.text = BuildWinners(payload);
        losersText.text = BuildLosers(payload);
        reasonText.text = BuildReason(payload);
    }

    private string BuildWinners(GameResultPayload p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("승리자 (이름 / 직업)");

        int count = 0;
        if (p.HasWinner0) { sb.AppendLine($"- {p.Winner0.Name} / {p.Winner0.Job}"); count++; }
        if (p.HasWinner1) { sb.AppendLine($"- {p.Winner1.Name} / {p.Winner1.Job}"); count++; }
        if (p.HasWinner2) { sb.AppendLine($"- {p.Winner2.Name} / {p.Winner2.Job}"); count++; }
        if (p.HasWinner3) { sb.AppendLine($"- {p.Winner3.Name} / {p.Winner3.Job}"); count++; }

        if (count == 0) sb.AppendLine("- (없음)");
        return sb.ToString();
    }

    private string BuildLosers(GameResultPayload p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("패배자 (이름 / 직업)");

        int count = 0;
        if (p.HasLoser0) { sb.AppendLine($"- {p.Loser0.Name} / {p.Loser0.Job}"); count++; }
        if (p.HasLoser1) { sb.AppendLine($"- {p.Loser1.Name} / {p.Loser1.Job}"); count++; }
        if (p.HasLoser2) { sb.AppendLine($"- {p.Loser2.Name} / {p.Loser2.Job}"); count++; }
        if (p.HasLoser3) { sb.AppendLine($"- {p.Loser3.Name} / {p.Loser3.Job}"); count++; }

        if (count == 0) sb.AppendLine("- (없음)");
        return sb.ToString();
    }

    private string BuildReason(GameResultPayload p)
    {
        // WinType도 같이 찍고 싶으면 아래처럼
        // return $"승리 요인 ({p.WinType})\n- {p.WinReason}";
        return $"승리 요인\n- {p.WinReason}";
    }
}
