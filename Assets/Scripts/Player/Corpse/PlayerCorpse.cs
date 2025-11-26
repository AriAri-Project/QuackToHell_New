using Unity.Netcode;
using UnityEngine;

public class PlayerCorpse : NetworkBehaviour
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    private NetworkVariable<PlayerAppearanceData> _appearanceData = new NetworkVariable<PlayerAppearanceData>();
    public NetworkVariable<PlayerAppearanceData> AppearanceData { get { return _appearanceData; } }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        
        // 초기값 적용
        ApplyAppearance();

        // 값 변경 감지 등록
        if (_appearanceData != null)
        {
            _appearanceData.OnValueChanged += (previousValue, newValue) =>
            {
                ApplyAppearance();
            };
        }
    }
    private void ApplyAppearance()
    {
        //색깔
        /*SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {*/
            /*if (spriteRenderer.gameObject.name.Contains("Body"))
            {*/
                int colorIndex = AppearanceData.Value.ColorIndex;
                // 모든 클라이언트에서 색상 변경 적용
                switch (colorIndex)
                {
                     case 0:
                        _spriteRenderer.color = Color.white;
                        break;
                    case 1:
                        _spriteRenderer.color = Color.red;
                        break;
                    case 2:
                        _spriteRenderer.color = new Color(1f, 0.647f, 0f);
                        break;
                    case 3:
                        _spriteRenderer.color = Color.yellow;
                        break;
                    case 4:
                        _spriteRenderer.color = Color.green;
                        break;
                    case 5:
                        _spriteRenderer.color = Color.blue;
                        break;
                    case 6:
                        _spriteRenderer.color =new Color(0.502f, 0f, 0.502f); 
                        break;
                }

            //}
        //}
    }


}
