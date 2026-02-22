using PinePie.SimpleJoystick;
using Unity.Netcode;
using UnityEngine;

public class MobileJoystickUI : UIHUD
{
    private JoystickController joystickController;
    private PlayerView playerView;
    public enum JoystickControllers
    {
        StaticFreemoving,
    }

    private void Start()
    {
        base.Init();
        Bind<JoystickController>(typeof(JoystickControllers));
        joystickController= Get<JoystickController>((int)JoystickControllers.StaticFreemoving);
        
        //바인드
        joystickController.OnTouchRemoved += OnTouchRemoved;
        joystickController.OnDirectionChanged += OnMove;
        
        //컴포넌트 가져오기
        playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(NetworkManager.Singleton.LocalClientId);
    }
    
    
    

    private void OnTouchRemoved()
    {
        if(playerView==null){
            playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(NetworkManager.Singleton.LocalClientId);
        }
        playerView.ProcessMoveInput(Vector2.zero);
    }

    private void OnMove()
    {
		if(playerView==null){
	        playerView = PlayerHelperManager.Instance.GetPlayerViewlByClientId(NetworkManager.Singleton.LocalClientId);
		}
        Vector2 inputDirection = joystickController.InputDirection;
        playerView.ProcessMoveInput(inputDirection);
    }
}
