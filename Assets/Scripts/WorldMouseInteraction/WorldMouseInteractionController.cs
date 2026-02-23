using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using WorldMouseInteraction;
using UnityEngine.SceneManagement;

public class WorldMouseInteractionController : MonoBehaviour
{
    private Camera mainCamera;
    private LayerMask interactableLayer;
    private void Awake()
    {
        //TODO: 카메라에게 이 컴포넌트(WorldMouse...)를 부착
        mainCamera = GetComponent<Camera>();
        interactableLayer = GameLayers.GetLayerMask(GameLayers.ClickableWorldObj);
    }
    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name == "ResultScene")
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //UI클릭 시 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            //월드 상호작용
            ProcessWorldClick();
        }
    }

    private void ProcessWorldClick()
    {
        //화면좌표->월드좌표 변환
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        //레이캐스트 발사
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, interactableLayer);
        
        if(hit.collider != null)
        {
            Debug.Log(hit.collider.gameObject.name);
            IClickableWorldObj clickableObj = hit.collider.GetComponent<IClickableWorldObj>();
            
            if(clickableObj!=null)
            {
                clickableObj.OnClick();
            }
        }
    }
}
