using System.Text;
using TMPro;
using UnityEngine;

public sealed class ResultScreenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text winnersText;
    [SerializeField] private TMP_Text losersText;
    [SerializeField] private TMP_Text reasonText;

    private void Start()
    {
        if (ResultBroadcaster.Instance != null &&
            ResultBroadcaster.Instance.HasPayload)
        {
            Render(ResultBroadcaster.Instance.LastPayload);
        }
    }
    public void Open(GameResultPayload payload)
    {
        Render(payload);
    }

    private void Render(GameResultPayload payload)
    {
        // 사유
        if (reasonText != null)
            reasonText.text = payload.WinReason.ToString();

        // 우승자
        if (winnersText != null)
            winnersText.text = BuildText("우승", payload, true);

        // 패배자
        if (losersText != null)
            losersText.text = BuildText("패배", payload, false);
    }

    private string BuildText(string title, GameResultPayload p, bool isWinner)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"{title} : ");

        bool first = true;

        if (isWinner)
        {
            AppendIf(ref sb, ref first, p.HasWinner0, p.Winner0);
            AppendIf(ref sb, ref first, p.HasWinner1, p.Winner1);
            AppendIf(ref sb, ref first, p.HasWinner2, p.Winner2);
            AppendIf(ref sb, ref first, p.HasWinner3, p.Winner3);
        }
        else
        {
            AppendIf(ref sb, ref first, p.HasLoser0, p.Loser0);
            AppendIf(ref sb, ref first, p.HasLoser1, p.Loser1);
            AppendIf(ref sb, ref first, p.HasLoser2, p.Loser2);
            AppendIf(ref sb, ref first, p.HasLoser3, p.Loser3);
        }

        return sb.ToString();
    }

    private void AppendIf(ref StringBuilder sb, ref bool first, bool has, ResultPlayerInfo info)
    {
        if (!has) return;

        string name = info.Name.ToString();

        if (!first)
            sb.Append("\n");   // 줄바꿈

        sb.Append(name);
        first = false;
    }
}