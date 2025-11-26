using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MiniMapUI : MonoBehaviour
{
    [SerializeField]
    private Transform left;
    [SerializeField] 
    private Transform right;
    [SerializeField]
    private Transform top;
    [SerializeField]
    private Transform bottom;

    [SerializeField]
    private Image minimapImage;
    [SerializeField]
    private Image minimapPlayerImage;

    // private CharacterMover targetPlayer;
    [SerializeField] 
    private Transform targetPlayer;

    [SerializeField] 
    private string[] playerTags = { "Player", "PlayerGhost" };

    private Transform FindOwnerByTags()
    {
        foreach (var tag in playerTags)
        {
            var candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (var go in candidates)
            {
                var no = go.GetComponent<Unity.Netcode.NetworkObject>();
                if (no != null && no.IsOwner && go.activeInHierarchy)
                    return go.transform;
            }
        }
        return null;
    }

    private void Start()
    {
        var inst = Instantiate(minimapImage.material);
        minimapImage.material = inst;

        var localPlayerObj = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayerObj != null)
        {
            targetPlayer = localPlayerObj.transform;   // 로컬 플레이어의 Transform
        }

        if (targetPlayer == null)
        {
            targetPlayer = FindOwnerByTags();
        }

        // targetPlayer = AmongUsRoomPlayer.MyRoomPlayer.myCharacter;

    }

    private void Update()
    {
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            var localPlayerObj = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
            targetPlayer = (localPlayerObj != null) ? localPlayerObj.transform : FindOwnerByTags();
        }

        if (targetPlayer != null)
        {
            Vector2 mapArea = new Vector2(
                Vector3.Distance(left.position, right.position),
                Vector3.Distance(bottom.position, top.position));

            Vector2 charPos = new Vector2(
                Vector3.Distance(left.position, new Vector3(targetPlayer.transform.position.x, 0f, 0f)),
                Vector3.Distance(bottom.position, new Vector3(0f, targetPlayer.transform.position.y, 0f)));

            Vector2 normalPos = new Vector2(charPos.x / mapArea.x, charPos.y / mapArea.y);

            minimapPlayerImage.rectTransform.anchoredPosition =
                new Vector2(minimapImage.rectTransform.sizeDelta.x * normalPos.x,
                            minimapImage.rectTransform.sizeDelta.y * normalPos.y);
        }

    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

}