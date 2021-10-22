using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerWeapon : NetworkBehaviour
{
	private ExpPlayerMovement pMove;
	private CharacterAvatar pAva;

	private GameObject currentWeapon;
	private GameObject nextWeapon;

	[SerializeField] ///Тут хранятся ссылки на префабы слотов оружия игрока.
	private GameObject[] weaponSlots;

	[SerializeField] ///Тут хранятся ссылки на префабы конкретных видов оружия игрока.
	private GameObject[] weaponInSlots;

	private void Awake()
	{

		pMove = this.GetComponent<ExpPlayerMovement>();
		pAva = this.GetComponent<CharacterAvatar>();

	}

	private void Start()
	{

		if (isLocalPlayer)
			InputManager.Instance.SetPlayerWeaponComponent(this);

		weaponSlots = new GameObject[4];
		weaponInSlots = new GameObject[4];

		weaponSlots[0] = pAva.clientSideAvatar.transform.Find("FirstSlotPrefab").gameObject;
		weaponSlots[1] = pAva.clientSideAvatar.transform.Find("SecondSlotPrefab").gameObject;
		weaponSlots[2] = pAva.clientSideAvatar.transform.Find("ThirdSlotPrefab").gameObject;
		weaponSlots[3] = pAva.clientSideAvatar.transform.Find("EquipmentSlotPrefab").gameObject;

		PlayerClientSetWeaponSlots(WeaponSetInfo.Instance.firstSlotWeapon, 
								   WeaponSetInfo.Instance.secondSlotWeapon,
							       WeaponSetInfo.Instance.thirdSlotWeapon,
							       WeaponSetInfo.Instance.equipmentSlotWeapon);

		CmdPlayerServerSetWeaponSlots(WeaponSetInfo.Instance.firstSlotWeapon,
								      WeaponSetInfo.Instance.secondSlotWeapon,
								      WeaponSetInfo.Instance.thirdSlotWeapon,
								      WeaponSetInfo.Instance.equipmentSlotWeapon);

	}

	/// <summary>
	/// Устанвливаем оружие в слоты игрока на стороне клиента.
	/// </summary>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="third"></param>
	/// <param name="equipment"></param>
	private void PlayerClientSetWeaponSlots (string first, string second, string third, string equipment)
	{
		if (first != "null")
		{
			Transform fTrans = weaponSlots[0].transform.Find(first);
			weaponInSlots[0] = fTrans.gameObject;
			currentWeapon = fTrans.gameObject;
			fTrans.gameObject.SetActive(true);
		}

		if (second != "null")
		{

			Transform sTrans = weaponSlots[1].transform.Find(second);
			weaponInSlots[1] = sTrans.gameObject;
		}

		if (third != "null")
		{

			Transform tTrans = weaponSlots[2].transform.Find(third);
			weaponInSlots[2] = tTrans.gameObject;
		}

		if (equipment != "null")
		{
			Transform eTrans = weaponSlots[3].transform.Find(equipment);
			weaponInSlots[3] = eTrans.gameObject;
		}
	}
	/// <summary>
	/// Устанавливаем оружие в слоты игрока на стороне сервера TODO
	/// </summary>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="third"></param>
	/// <param name="equipment"></param>
	[Command]
	private void CmdPlayerServerSetWeaponSlots(string first, string second, string third, string equipment)
	{



	}
	
	/// <summary>
	/// Устанавливаем новое активное оружие игрока после его смены на стороне сервера. Здесь же вызываются функции для смены оружия на стороне клиента TODO
	/// </summary>
	[Command]
	public void CmdChangeWeapon()
	{

		for (int i = 0; i < weaponInSlots.Length - 1; i++)
		{


			if (currentWeapon == weaponInSlots[i])
			{

				currentWeapon.SetActive(false);

				if ((i + 1) <= weaponInSlots.Length - 2)
				{

					ChangeCurrentWeapon(weaponInSlots[i + 1]);
					currentWeapon.SetActive(true);
					break;
				}
				else
				{

					ChangeCurrentWeapon(weaponInSlots[0]);
					currentWeapon.SetActive(true);
					break;
				}

			}
			else

				continue;

		}



	}


	void ChangeCurrentWeapon (GameObject weapon)
	{
		currentWeapon = weapon;
		AvatarInfo avaInfo = weapon.GetComponent<AvatarInfo>();
		pMove.SetFPCharArms(avaInfo);


	}
}
