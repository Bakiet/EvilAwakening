//RemoveBody.cs by Azuline Studios© All Rights Reserved
//Removes NPC ragdolls after bodyStayTime.
using UnityEngine;
using System.Collections;

public class RemoveBody : MonoBehaviour {

	private float startTime = 0;
	[HideInInspector]
	public float bodyStayTime = 15.0f;
	public GameObject GunPickup;//weapon item pickup that should be spawned after NPC dies
	
	void Start (){
		startTime = Time.time;
	}
	
	void FixedUpdate (){
		if(startTime + bodyStayTime < Time.time){
			if(GunPickup){
				GunPickup.transform.parent = null;//unparent weapon pickup object so it won't be deleted
			}
			Destroy(gameObject);
		}
	}

}