using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 특정 오브젝트가 그림자에 가려질경우 안보이게 처리하는 클래스
/// </summary>
///
/// 구현아이디어
/// 광원(Light)에서 플레이어에게 레이저(Ray)를 쐈을 때, 벽(ShadowCaster)에 먼저 막히면 플레이어는 그림자 속에 있는 것
public class ShadowHider : NetworkBehaviour
{
    private PlayerView playerView;
    private PlayerModel playerModel;
    private FarmerStrategy playerFarmerStrategy;
    private LayerMask detectionLayer;
    private float innerEyesight;
    private float outerEyesight;
    private Light2D light2D;
    
    List<GameObject> detectionObjects;
    
    private void Start()
    {
        //내 플레이어 뷰 가져오기
        playerView = GetComponentInParent<PlayerView>();
        //플레이어모델
        playerModel = playerView.GetComponent<PlayerModel>();
        //레이캐스트로 감지할 레이어: 그림자 벽
        detectionLayer.value = GameLayers.GetLayerMask(GameLayers.ShadowWall);
        //로비세팅된대로 시야범위 세팅
        light2D = GetComponent<Light2D>();
        if (light2D != null)
        {
            light2D.pointLightInnerRadius =  LobbyManager.Instance.LobbyData.innerEyesightValue;
            light2D.pointLightOuterRadius = LobbyManager.Instance.LobbyData.outerEyesightValue;
        }
    }

    private void Update()
    {
        //오너만 체크해야함
        if (!IsOwner)
        {
            return;
        }
        //죽은애는 다 보이게
        if (playerModel.GetPlayerCurrentJob() == PlayerJob.Ghost)
        {
            return;
        }
        
        //시야범위세팅
        if (SceneManager.GetActiveScene().name == GameScenes.Lobby)
        {
            light2D.pointLightInnerRadius =  LobbyManager.Instance.LobbyData.innerEyesightValue;
            light2D.pointLightOuterRadius = LobbyManager.Instance.LobbyData.outerEyesightValue;
        }

        
        foreach (var target in playerView.OverlappingPlayers)
        {
            if (target == null) continue;
            
            // 시체인지 확인
            PlayerCorpse corpse = target.GetComponent<PlayerCorpse>();
            if (corpse != null)
            {
                // 시체 처리 로직
                float _distance = Vector2.Distance(transform.position, target.transform.position);
                Vector2 _direction = (target.transform.position - transform.position).normalized;
                RaycastHit2D _hit = Physics2D.Raycast(transform.position, _direction, _distance, detectionLayer);
        
                corpse.SetVisibility(_hit.collider == null); // 벽에 안 막히면 보이게
                continue;
            }
            
            // 유령(Ghost) 플레이어는 ShadowHider에서 처리하지 않음
            PlayerModel targetModel = target.GetComponent<PlayerModel>();
            if (targetModel != null && targetModel.GetPlayerAliveState() == PlayerLivingState.Dead)
            {
                continue; // 유령은 스킵
            }
            
            //광원과 상대 플레이어간의 벡터 계산
            float distance = Vector2.Distance(transform.position, target.transform.position);
            Vector2 direction = (target.transform.position - transform.position).normalized;
            
            //광원에서 플레이어쪽으로 레이 발사
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, detectionLayer);
            
            //레이에 벽이 부딪혔는지 확인
            if (hit.collider != null)//벽에부딪힘 = 타겟 플레이어를 가려야 함
            {
                target?.GetComponent<PlayerView>()?.SetPlayerVisibility(false);
            }
            else//벽에 안 부딪힘 = 타겟플레이어가 보여져야 함
            {
                playerFarmerStrategy = target.GetComponent<FarmerStrategy>();
                //(타겟이 farmer라면)벤트에 있는지도 확인
                if (playerFarmerStrategy.enabled)
                {
                    if (playerFarmerStrategy.IsVentEntered==false)
                    {
                        target?.GetComponent<PlayerView>()?.SetPlayerVisibility(true);        
                    }
                    else
                    {
                        target?.GetComponent<PlayerView>()?.SetPlayerVisibility(false); 
                    }
                }
                //타겟이 animal라면
                else
                {
                    target?.GetComponent<PlayerView>()?.SetPlayerVisibility(true);
                }
            }
        }
    }
    
    public bool IsTargetHiddenByShadow(GameObject target)
    {
        if (target == null) return false;
    
        float distance = Vector2.Distance(transform.position, target.transform.position);
        Vector2 direction = (target.transform.position - transform.position).normalized;
    
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, detectionLayer);
        return hit.collider != null;
    }
}
