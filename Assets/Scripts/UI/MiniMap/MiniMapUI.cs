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

    [SerializeField] private Vector2 offset;
    [SerializeField] private float scaleMultiplier = 1f;

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
        if (targetPlayer == null)
        {
            var localPlayerObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayerObj != null)
            {
                targetPlayer = localPlayerObj.transform;
            }
            else
            {
                targetPlayer = FindOwnerByTags();
            }

            if (targetPlayer == null)
                return;
        }

        UpdateMiniMapPosition();
    }

    private void UpdateMiniMapPosition()
    {
        float mapWidth = right.position.x - left.position.x;
        float mapHeight = top.position.y - bottom.position.y;

        float normalizedX = (targetPlayer.position.x - left.position.x) / mapWidth;
        float normalizedY = (targetPlayer.position.y - bottom.position.y) / mapHeight;

        Vector2 mapSize = minimapImage.rectTransform.sizeDelta;

        float posX = (normalizedX - 0.5f) * mapSize.x;
        float posY = (normalizedY - 0.5f) * mapSize.y;

        Vector2 finalPos = new Vector2(posX, posY) * scaleMultiplier + offset;

        minimapPlayerImage.rectTransform.anchoredPosition = finalPos;
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