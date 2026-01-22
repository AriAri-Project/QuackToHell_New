using UnityEngine;
using TMPro;

public class RoleAssignUIReferences : MonoBehaviour
{
    [Header("UI References")]
    
    public GameObject Intro;
    public GameObject ShowRole;
    public TextMeshProUGUI ShowRoleText;
    public Transform spawnParent;
    [Header("Arc Settings")] 
    public int maxPlayerNum=16;
    public int minPlayerNum=5;
    
    // 최소 인원(5명)일 때의 반지름 (예: 500 - 더 둥글게 모임)
    public float minArcRadius = 500f; 

    // 최대 인원(16명)일 때의 반지름 (예: 1500 - 더 완만하게 퍼짐)
    public float maxArcRadius = 1500f; 

    public float arcAngleGap = 10f;
}
