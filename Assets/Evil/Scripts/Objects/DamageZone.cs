//DamageZone.cs by Azuline Studios© All Rights Reserved
//Damages player by damage var amount when they enter a trigger with this script attached
using UnityEngine;
using System.Collections;

public class DamageZone : MonoBehaviour {
	public float damage = 1.0f;//amount of damage to apply to player while in damage trigger
	public float delay = 1.75f;//delay before player is damaged again 
	private float damageTime;
	private FPSPlayer FPSPlayerComponent;
	
	void Start () {
		FPSPlayerComponent =  Camera.main.GetComponent<CameraKick>().playerObj.GetComponent<FPSPlayer>();
	}
	
	void OnTriggerStay ( Collider col  ){
		if(col.gameObject.tag == "Player"){
			if(damageTime < Time.time){
				FPSPlayerComponent.ApplyDamage(damage);
				damageTime = Time.time + delay;
			}
		}
		if(col.gameObject.layer == 13){//also damage NPCs
			CharacterDamage NPC = col.GetComponent<CharacterDamage>();
			if(damageTime < Time.time){
				NPC.ApplyDamage(damage, Vector3.zero, transform.position);
				damageTime = Time.time + delay;
			}
		}
	}

}




