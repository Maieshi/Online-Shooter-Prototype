using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rewired;
using Mirror;

public class Monosingleton<T> : MonoBehaviour where T: Monosingleton<T>
{

	private static T _instance;
	public static T Instance
	{

		get
		{

			if (_instance != null)
				return _instance;

			return null;

		}

	}

    void Awake()
    {

		_instance = (T)this;

    }


    void Update()
    {
        
    }
}
