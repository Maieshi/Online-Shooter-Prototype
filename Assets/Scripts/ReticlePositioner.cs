using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReticlePositioner : MonoBehaviour
{

	private void Awake()
	{

		RectTransform rTrans = this.GetComponent<RectTransform>();
		rTrans.position = new Vector3 (Screen.width / 2, Screen.height / 2, 0);

	}



}
