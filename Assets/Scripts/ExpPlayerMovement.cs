using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Mirror;

public class ExpPlayerMovement : NetworkBehaviour
{
	//#region Singleton_implementation
	//private static PlayerMovement _instance;
	//public static PlayerMovement Instance
	//{

	//	get
	//	{

	//		if (_instance != null)
	//			return _instance;
	//		return null;

	//	}

	//}
	//#endregion Singlton_implementation


	public int playerID;
	private Player player;

	private CharacterController charController;
	private PlayerWeapon pWeapon;

	public bool FPAvatarActive;

	public bool isAutoSprinting { get; private set; }
	public bool isSprinting { get; private set; }

	[Header ("First-person and third-person avatars")]
	[SerializeField]
	private AvatarInfo FPPrefab;
	[SerializeField]
	private AvatarInfo TPPrefab;

	[Header("Character arms and character animator references")]
	[SerializeField]
	private GameObject CharArms;
	[SerializeField]
	private Animator CharAnimator;

	[Header("Character walking and running speed")]
	[SerializeField]
	private float walkSpeed;
	[SerializeField]
	private float runSpeed;

	[Header("Character camera rotation speed")]
	[SerializeField]
	private float rotateVerSpeed;
	[SerializeField]
	private float rotateHorSpeed;

	private void Awake()
	{
		pWeapon = this.GetComponent<PlayerWeapon>();
		InputManager.Instance.ToogleUI(true);
	}

	void Start()
	{
		if (isLocalPlayer)
			InputManager.Instance.SetPlayerMovementComponent(this);

		if (isLocalPlayer)
		{

			CharArms = FPPrefab.charArms;
			CharAnimator = FPPrefab.charAnimator;

		}
		else
		{

			CharArms = TPPrefab.charArms;
			CharAnimator = TPPrefab.charAnimator;

		}
		charController = this.GetComponent<CharacterController>();

	}


	[Command]
	public void CmdGravity()
	{

			this.charController.Move(new Vector3(0, -1, 0));
		
	}

	/// <summary>
	/// Эта функция подключает нужный префаб оружия на стороне клиента
	/// </summary>
	/// <param name="inf"></param>
	public void SetFPCharArms (AvatarInfo inf)
	{
		inf.charArms.transform.rotation = CharArms.transform.rotation;
		CharArms = inf.charArms;
		CharAnimator = inf.charAnimator;

	}

	/// <summary>
	/// Эта функция запускает анимацию движения на стороне клиента////TODO
	/// </summary>
	/// <param name="vec"></param>
	public void ClientMove(Vector2 vec)
	{


		if (vec.x > 1 || vec.y > 1)
			vec = new Vector2(1, 1);

		if (vec.x < -1 || vec.y < -1)
			vec = new Vector2(-1, -1);



		if (vec.y >= 0.9)
		{

			if (!CharAnimator.GetBool("Run"))
			{
				if (CharAnimator.GetBool("Walk"))
				{
					CharAnimator.SetBool("Walk", false);
					CharAnimator.SetFloat("WalkSpeed", 0);
					CharAnimator.SetBool("Run", true);
				
				}
				else
				{
					CharAnimator.SetBool("Run", true);
				}
			}

			isSprinting = true;

			charController.Move(Vector3.ClampMagnitude(transform.TransformDirection(new Vector3(vec.x * walkSpeed * Time.deltaTime, 0, vec.y * runSpeed * Time.deltaTime)), walkSpeed));

		}
		else
		{

			if (!CharAnimator.GetBool("Walk"))
			{
				if (CharAnimator.GetBool("Run"))
				{
					CharAnimator.SetBool("Run", false);
					isSprinting = false;

					if (Mathf.Abs(vec.x) >= Mathf.Abs(vec.y))
					{

						CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.x));

					}
					else
					{

						CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.y));

					}

					CharAnimator.SetBool("Walk", true);

				}
				else
				{
					if (Mathf.Abs(vec.x) >= Mathf.Abs(vec.y))
					{

						CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.x));

					}
					else
					{

						CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.y));

					}

					CharAnimator.SetBool("Walk", true);

				}
			}
			else
			{

				if (Mathf.Abs(vec.x) >= Mathf.Abs(vec.y))
				{

					CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.x));

				}
				else
				{

					CharAnimator.SetFloat("WalkSpeed", Mathf.Abs(vec.y));

				}

			}

			charController.Move(Vector3.ClampMagnitude(transform.TransformDirection(new Vector3(vec.x * walkSpeed * Time.deltaTime, 0, vec.y * walkSpeed * Time.deltaTime)), walkSpeed));
		}
	}

	/// <summary>
	/// Эта функция двигает игрока на сервере.
	/// </summary>
	/// <param name="vec"></param>
	[Command]
	public void CmdMove(Vector2 vec)
	{
		

			if (vec.x > 1 || vec.y > 1)
				vec = new Vector2(1, 1);

			if (vec.x < -1 || vec.y < -1)
				vec = new Vector2(-1, -1);




		if (vec.y >= 0.9)
		{
			CharAnimator.SetFloat("Horizontal", vec.x);
			CharAnimator.SetFloat("Vertical", vec.y);

			isSprinting = true;

			charController.Move(Vector3.ClampMagnitude(transform.TransformDirection(new Vector3(vec.x * walkSpeed * Time.deltaTime, 0, vec.y * runSpeed * Time.deltaTime)), walkSpeed));
		}
		else
		{
			CharAnimator.SetFloat("Horizontal", vec.x);
			CharAnimator.SetFloat("Vertical", vec.y);

			isSprinting = false;

			charController.Move(Vector3.ClampMagnitude(transform.TransformDirection(new Vector3(vec.x * walkSpeed * Time.deltaTime, 0, vec.y * walkSpeed * Time.deltaTime)), walkSpeed));

		}
		
	}

	/// <summary>
	/// Эта фнукция включает флаг автобега на стороне клиента
	/// </summary>
	public void ClientSetAutoSprint (bool val)
	{

		isAutoSprinting = val;

	}

	/// <summary>
	/// Эта фнукция включает флаг автобега на стороне сервера
	/// </summary>
	[Command]
	public void CmdSetAutoSprint(bool val)
	{

		isAutoSprinting = val;

	}

	/// <summary>
	/// Эта функция выключает анимацию движения на стороне клиента///TODO
	/// </summary>
	public void ClientStopMovingAnim()
	{

		if (CharAnimator.GetBool("Walk"))
		{

			CharAnimator.SetBool("Walk", false);
			isSprinting = false;
			isAutoSprinting = false;
			CharAnimator.SetFloat("WalkSpeed", 0);

		}

		if (CharAnimator.GetBool("Run"))
		{

			CharAnimator.SetBool("Run", false);
			isSprinting = false;
			isAutoSprinting = false;
			CharAnimator.SetFloat("WalkSpeed", 0);

		}


	}

	/// <summary>
	/// Эта функция выключает анимации ходьбы и бега на стороне сервера после того как пользователь отпускает стик движения
	/// </summary>
	[Command]
	public void CmdStopMovingAnim()
	{

		CharAnimator.SetFloat("Horizontal", 0);
		CharAnimator.SetFloat("Vertical", 0);
		isSprinting = false;
		isAutoSprinting = false;


	}


	/// <summary>
	/// Эта функция управляет камерой игрока на сервере.
	/// </summary>
	/// <param name="vec"></param>
	[Command]
	public void CmdRotateCamera(Vector2 vec)
	{
		

			Vector3 rotVecCam = new Vector3(CharArms.transform.localEulerAngles.x + Mathf.Clamp((-vec.y * rotateVerSpeed * Time.deltaTime), -45, 45),
											CharArms.transform.localEulerAngles.y,
											CharArms.transform.localEulerAngles.z);

			CharArms.transform.localEulerAngles = (rotVecCam);


			Vector3 rotVecChar = new Vector3(transform.localEulerAngles.x,
											 transform.localEulerAngles.y + (vec.x * rotateHorSpeed * Time.deltaTime),
											 transform.localEulerAngles.z);

			transform.localEulerAngles = (rotVecChar);
		
	}
}
