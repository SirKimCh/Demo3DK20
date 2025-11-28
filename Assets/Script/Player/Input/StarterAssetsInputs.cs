using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool steal;

		[Header("Movement Settings")]
		public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;
	public bool cursorInputForLook = true;
	
	private static bool isGameActive = false; // Game only active after login
	private Login cachedLoginScript; // Cache Login reference

	void Start()
	{
		// Start with cursor unlocked for login
		if (!isGameActive)
		{
			UnlockCursor();
		}
		
		// Cache Login script reference
		cachedLoginScript = Object.FindObjectOfType<Login>();
	}

#if ENABLE_INPUT_SYSTEM
	public void OnMove(InputValue value)
	{
		if (isGameActive)
		{
			MoveInput(value.Get<Vector2>());
		}
	}

	public void OnLook(InputValue value)
	{
		if(cursorInputForLook && isGameActive)
		{
			LookInput(value.Get<Vector2>());
		}
	}

	public void OnJump(InputValue value)
	{
		if (isGameActive)
		{
			JumpInput(value.isPressed);
		}
	}

	public void OnSprint(InputValue value)
	{
		if (isGameActive)
		{
			SprintInput(value.isPressed);
		}
	}

	public void OnSteal(InputValue value)
	{
		if (isGameActive)
		{
			StealInput(value.isPressed);
		}
	}

	public void OnMenu(InputValue value)
	{
		// Only trigger on key press (not release) and when game is active
		if (value.isPressed && isGameActive)
		{
			// Use cached reference or find if not cached
			Login loginScript = cachedLoginScript;
			if (loginScript == null)
			{
				loginScript = Object.FindObjectOfType<Login>();
				cachedLoginScript = loginScript;
			}
			
			if (loginScript != null)
			{
				loginScript.ShowPauseMenu(); // Show pause menu instead of logout
			}
		}
	}
#endif


	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	} 

	public void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	public void JumpInput(bool newJumpState)
	{
		jump = newJumpState;
	}

	public void SprintInput(bool newSprintState)
	{
		sprint = newSprintState;
	}

	public void StealInput(bool newStealState)
	{
		steal = newStealState;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (isGameActive)
		{
			SetCursorState(cursorLocked);
		}
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !newState;
	}
	
	// Public methods to control cursor and game state
	public static void UnlockCursor()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		isGameActive = false;
	}
	
	public static void LockCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		isGameActive = true;
	}
	
	public static void SetGameActive(bool active)
	{
		isGameActive = active;
		if (active)
		{
			LockCursor();
		}
		else
		{
			UnlockCursor();
		}
	}
	
	public static bool IsGameActive()
	{
		return isGameActive;
	}
	}
	
}

