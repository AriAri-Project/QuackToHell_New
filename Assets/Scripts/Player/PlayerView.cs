using System;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    public EventHandler OnMovementInput;

    public class OnMovementInputEventArgs: EventArgs{
        
        private int xDirection;
        private int yDirection;
        
        public int XDirection
        {
            get { return xDirection; }
        }
        public int YDirection
        {
            get { return yDirection; }
        }

        public OnMovementInputEventArgs(int inputXDirection, int inputYDirection)
        {
            xDirection = inputXDirection;
            yDirection = inputYDirection;
        }
    }



    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        int inputXDirection = 0;
        int inputYDirection = 0;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            inputYDirection = 1;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            inputYDirection = -1;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            inputXDirection = -1;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            inputXDirection = 1;
        }

        if (inputXDirection != 0f || inputYDirection != 0f)
        {
            OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(inputXDirection, inputYDirection));
        }
        else
        {
            OnMovementInput?.Invoke(this, new OnMovementInputEventArgs(0, 0));
        }
    }
}
