using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Rewired;

public class AutoSprintElement : MonoBehaviour, IEndDragHandler, IPointerClickHandler
{


	private ExpPlayerMovement pMove;


	/// <summary>
	/// Проверяем, где игрок отпустил палец после того, как начал спринт. Если отпусти палец над объектом, к которому прикреплен компонент - включается
	/// автобег.
	/// </summary>
	/// <param name="eventData"></param>
	public void OnEndDrag(PointerEventData eventData)
	{
		RaycastResult rRes = eventData.pointerCurrentRaycast;

		if (pMove == null)
			pMove = InputManager.Instance.GetPlayerMovementComponent();

		if (rRes.gameObject != null)
		{


			if (rRes.gameObject.CompareTag("SprintIndicator"))
			{

				if (!pMove.isAutoSprinting&&pMove.isSprinting)
				{
					pMove.ClientSetAutoSprint(true);
					pMove.CmdSetAutoSprint(true);
	
				}
				else
				{
					InputManager.Instance.MoveTouchEnded();

				}


			}
			else
			{
				if (pMove.isAutoSprinting)
				{
					pMove.ClientSetAutoSprint(false);
					pMove.CmdSetAutoSprint(false);
				}

				InputManager.Instance.MoveTouchEnded();

			}
		}
		else
		{
			if (pMove.isAutoSprinting)
			{
				pMove.ClientSetAutoSprint(false);
				pMove.CmdSetAutoSprint(false);
			}

			InputManager.Instance.MoveTouchEnded();

		}
	}

	/// <summary>
	/// Если мы тапаем по джойстику (в любом его месте) во время автобега, автобег прерывается
	/// </summary>
	/// <param name="eventData"></param>
	public void OnPointerClick(PointerEventData eventData)
	{
		if (pMove == null)
			pMove = InputManager.Instance.GetPlayerMovementComponent();

		Debug.Log("Click");
		RaycastResult rRes = eventData.pointerCurrentRaycast;

		if (rRes.gameObject.CompareTag("MoveJoystick") && pMove.isAutoSprinting)
		{
			Debug.Log("StopAutoSprint");
			pMove.ClientSetAutoSprint(false);
			pMove.CmdSetAutoSprint(false);
			InputManager.Instance.MoveTouchEnded();

		}
	}
}
