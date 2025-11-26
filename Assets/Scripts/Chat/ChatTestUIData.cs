using UnityEngine;

/// <summary>
/// 프레젠터에서 뷰로 전달하는 채팅 UI 데이터
/// </summary>
[System.Serializable]
public struct ChatTestUIData
{
    public string playerName;           // 플레이어 이름
    public string message;              // 메시지 내용
    public Sprite playerIcon;           // 플레이어 아이콘 (null 허용)
    public Color nameColor;             // 이름 색상
    public Color messageColor;          // 메시지 색상
    public bool showTimestamp;          // 타임스탬프 표시 여부
    public string timestamp;            // 타임스탬프 텍스트
    public bool isOwnMessage;           // 본인 메시지 여부

    public ChatTestUIData(string playerName, string message, bool isOwnMessage = false)
    {
        this.playerName = playerName;
        this.message = message;
        this.isOwnMessage = isOwnMessage;
        this.playerIcon = null;
        this.nameColor = Color.white;
        this.messageColor = Color.white;
        this.showTimestamp = false;
        this.timestamp = "";
    }

    public ChatTestUIData(string playerName, string message, Sprite icon, Color nameColor, 
                     Color messageColor, bool showTimestamp, string timestamp, bool isOwnMessage)
    {
        this.playerName = playerName;
        this.message = message;
        this.playerIcon = icon;
        this.nameColor = nameColor;
        this.messageColor = messageColor;
        this.showTimestamp = showTimestamp;
        this.timestamp = timestamp;
        this.isOwnMessage = isOwnMessage;
    }
}

