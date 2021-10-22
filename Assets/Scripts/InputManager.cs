using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Rewired;
using Mirror;

public class InputManager : Monosingleton<InputManager>

{

	[SerializeField]
	private GameObject MainCanvas;

	[SerializeField]
	private GameObject MovementJoystick;


	[SerializeField]
	private GameObject sprintIndicator;

	[SerializeField]
	private bool isMoving;
	[SerializeField]
	private bool isRotating;

	[SerializeField]
	private ExpPlayerMovement pMove;

	[SerializeField]
	private PlayerWeapon pWeapon;

	private Vector2 moveVec;
	private Vector2 rotateVec;

	/// <summary>
	/// Функция включает/выключает игровой UI.
	/// </summary>
	/// <param name="val"></param>
	public void ToogleUI(bool val)
	{


		MainCanvas.SetActive(val);


	}

	/// <summary>
	/// Функция, вызываемая префабом игрока и присылающая компонент, ответственный за управление персонажем.
	/// </summary>
	/// <param name="pM"></param>
	public void SetPlayerMovementComponent (ExpPlayerMovement pM)
	{
		
		pMove = pM;
		
	}

	/// <summary>
	/// Функция для получения компонента, ответственного за управление персонажем.
	/// </summary>
	/// <param name="pM"></param>
	public ExpPlayerMovement GetPlayerMovementComponent()
	{

		return pMove;

	}

	/// <summary>
	/// Функция, вызываемая префабом игрока и присылающая компонент, ответственный за управление оружием игрока
	/// </summary>
	/// <param name="pW"></param>
	public void SetPlayerWeaponComponent (PlayerWeapon pW)
	{

		pWeapon = pW;

	}

	/// <summary>
	/// Функция для получения компонента, ответственного за управление оружием игрока.
	/// </summary>
	/// <param name="pW"></param>
	public PlayerWeapon GetPlayerWeaponComponent()
	{

		return pWeapon;

	}

	/// <summary>
	/// В апдейте отслеживаеются тачи и определяется, в какой "зоне" (передвижения или поворота камеры) они были сделаны. В зависимости от этого
	/// вызывается либо команда релокации джойстика передвижения, либо команда поворота камеры.
	/// </summary>
	private void Update()
	{
		///Включение/выключение индикатора спринта в интерфейсе.
		if (pMove != null)
		{
			if (pMove.isSprinting || pMove.isAutoSprinting)
			{

				sprintIndicator.SetActive(true);

			}
			else
			{

				sprintIndicator.SetActive(false);

			}

		}

		///Отслеживание и обработка тапов по экрану в тех местах, где нет элементов интерфейса.
		if(Input.touchCount > 0)
		{
			foreach (Touch touch in Input.touches)
			{

				if (touch.position.x < Screen.width / 2 && touch.position.y < (Screen.height/5) * 4)
				{
					if (touch.phase == TouchPhase.Began)
					{
						RectTransform rTrans = MovementJoystick.GetComponent<RectTransform>();

						rTrans.position = new Vector3(touch.position.x, touch.position.y, 0);
					}

				}
				else if (touch.position.x > Screen.width / 2)
				{

					if (touch.phase == TouchPhase.Moved)
					{


						RotateCommand(touch.deltaPosition);


					}

				}
			}


		}

		///Применяем "гравитацию" к контроллеру, проверяем, находится ли на данный момент контроллер в движении, если да - продолжаем его двигать.
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{
			if (pMove != null)
				pMove.CmdGravity();

			if (isMoving)
			{

				MoveCommand(moveVec);

			}
		}


	}
#region Move_functions
	public void MoveInfo (Vector2 vec)
	{
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{

			if (pMove.isAutoSprinting)
			{

				pMove.ClientSetAutoSprint(false);
				pMove.CmdSetAutoSprint(false);

			}

			if (vec.x > 0 || vec.y > 0 || vec.x < 0 || vec.y < 0)
			{
				moveVec = vec;

				if (!isMoving)
					isMoving = true;
			}
		}
	}

	public void MoveCommand (Vector2 vec)
	{
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{
			pMove.CmdMove(vec);
			pMove.ClientMove(vec);
		}
		
	}

	public void MoveTouchEnded()
	{
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer && !pMove.isAutoSprinting && pMove.isSprinting)
		{

				isMoving = false;
				pMove.CmdStopMovingAnim();
				pMove.ClientStopMovingAnim();


		}
		else if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{

				isMoving = false;
				pMove.CmdStopMovingAnim();
				pMove.ClientStopMovingAnim();


		}
		
	}
#endregion Move_functions

#region Rotate_camera_functions


	public void RotateCommand (Vector2 vec)
	{
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{
			pMove.CmdRotateCamera(vec);
		}
	}



#endregion Rotate_camera_functions

#region Weapon_functions
	public void ChangeWeapon()
	{
		if (pMove != null && pMove.hasAuthority && pMove.isLocalPlayer)
		{

			pWeapon.CmdChangeWeapon();

		}

	}
#endregion Weapon_functions
}
