using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class ShadowHider : NetworkBehaviour
{
    private PlayerView playerView;
    private PlayerModel playerModel;
    private LayerMask detectionLayer;
    private Light2D light2D;
    
    // 오프셋 설정 변수 (Y -1)
    [SerializeField] private Vector2 rayOffset = new Vector2(0, -1f); 

    private void Start()
    {
        playerView = GetComponentInParent<PlayerView>();
        playerModel = playerView.GetComponent<PlayerModel>();
        detectionLayer.value = GameLayers.GetLayerMask(GameLayers.Wall);
        
        light2D = GetComponent<Light2D>();
        if (light2D != null)
        {
            light2D.pointLightInnerRadius =  LobbyManager.Instance.LobbyData.innerEyesightValue;
            light2D.pointLightOuterRadius = LobbyManager.Instance.LobbyData.outerEyesightValue;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (playerModel.GetPlayerJob() == PlayerJob.Ghost) return;
        
        if (SceneManager.GetActiveScene().name == GameScenes.Lobby)
        {
            light2D.pointLightInnerRadius =  LobbyManager.Instance.LobbyData.innerEyesightValue;
            light2D.pointLightOuterRadius = LobbyManager.Instance.LobbyData.outerEyesightValue;
        }

        // 레이 시작 지점을 계산 (현재 위치 + 오프셋)
        Vector2 rayStartPos = (Vector2)transform.position + rayOffset;

        foreach (var target in playerView.OverlappingPlayers)
        {
            if (target == null) continue;
            
            // 1. 시체 처리
            PlayerCorpse corpse = target.GetComponent<PlayerCorpse>();
            if (corpse != null)
            {
                // transform.position 대신 rayStartPos 사용
                float _distance = Vector2.Distance(rayStartPos, target.transform.position);
                Vector2 _direction = ((Vector2)target.transform.position - rayStartPos).normalized;
                
                RaycastHit2D _hit = Physics2D.Raycast(rayStartPos, _direction, _distance, detectionLayer);
        
                bool isVisible = _hit.collider == null;
                corpse.SetVisibility(isVisible); 

                if (isVisible)
                    Debug.DrawRay(rayStartPos, _direction * _distance, Color.green);
                else
                    Debug.DrawRay(rayStartPos, _direction * _hit.distance, Color.red);
                continue;
            }
            
            // 2. 유령 스킵
            PlayerModel targetModel = target.GetComponent<PlayerModel>();
            if (targetModel != null && targetModel.GetPlayerAliveState() == PlayerLivingState.Dead)
            {
                continue; 
            }
            
            // 3. 플레이어 처리
            // transform.position 대신 rayStartPos 사용
            float distance = Vector2.Distance(rayStartPos, target.transform.position);
            Vector2 direction = ((Vector2)target.transform.position - rayStartPos).normalized;
            
            RaycastHit2D hit = Physics2D.Raycast(rayStartPos, direction, distance, detectionLayer);
            
            if (hit.collider != null)
            {
                target.GetComponent<PlayerView>().SetPlayerVisibility(false);
                Debug.DrawRay(rayStartPos, direction * hit.distance, Color.red);
            }
            else
            {
                target.GetComponent<PlayerView>().SetPlayerVisibility(true);
                Debug.DrawRay(rayStartPos, direction * distance, Color.green);
            }
        }
    }
    
    public bool IsTargetHiddenByShadow(GameObject target)
    {
        if (target == null) return false;
        
        Vector2 rayStartPos = (Vector2)transform.position + rayOffset;

        float distance = Vector2.Distance(rayStartPos, target.transform.position);
        Vector2 direction = ((Vector2)target.transform.position - rayStartPos).normalized;
    
        RaycastHit2D hit = Physics2D.Raycast(rayStartPos, direction, distance, detectionLayer);
        return hit.collider != null;
    }
}