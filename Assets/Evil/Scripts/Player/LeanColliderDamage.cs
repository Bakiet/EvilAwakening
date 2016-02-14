//LeanColliderDamage.cs by Azuline Studios© All Rights Reserved
//Applies damage to player whenthey are leaning.
using UnityEngine;
using System.Collections;

public class LeanColliderDamage : MonoBehaviour {
	
	private FPSPlayer FPSPlayerComponent; 

	void Start () {
		FPSPlayerComponent =  Camera.main.GetComponent<CameraKick>().playerObj.GetComponent<FPSPlayer>();
	}
	
	public void ApplyDamage ( float damage ){
		FPSPlayerComponent.ApplyDamage(damage);
	}	
}
