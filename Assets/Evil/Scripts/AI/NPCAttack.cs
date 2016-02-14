//NPCAttack.cs by Azuline StudiosÂ© All Rights Reserved
//Manages timers for enemy attacks, damage application to other objects, and sound effects.
using UnityEngine;
using System.Collections;

public class NPCAttack : MonoBehaviour {
	private AI AIComponent;
	
	private Transform myTransform;
	public float range = 100.0f;
	public float inaccuracy = 0.5f;//random range in units around target that enemy's attack will hit
	public float fireRate = 0.097f;
	public int burstShots = 0;
	public int randomShots = 0;
	public float force = 20.0f;
	public float damage = 10.0f;
	private float damageAmt;
	public int bulletsPerClip = 50;
	public int ammo = 150;
	public float reloadTime = 1.75f;
	
	private bool doneShooting = true;
	private int shotsFired;
	private bool randBurstState;
	private int randShotsAmt;
	private bool  shooting = false;
	private bool  reloading = false;
	private bool  mFlashState = false;
	//private bool  noAmmoState = false;
	
	public ParticleEmitter hitParticles;
	public Renderer muzzleFlash;
	
	public AudioClip firesnd;
	public float fireSndRandPitch = 0.86f;
	public AudioClip reloadsnd;
	public AudioClip noammosnd;
	
	private int bulletsLeft = 0;
	
	private float shootStartTime = 0.0f;
	private float shootElapsedTime = 0.0f;
	
	void Start (){
		
		myTransform = transform;
		AIComponent = myTransform.GetComponent<AI>();
		
		hitParticles = GetComponentInChildren<ParticleEmitter>();
	
		// We don't want to emit particles all the time, only when we hit something.
		if (hitParticles){
			hitParticles.emit = false;
		}
			
		bulletsLeft = bulletsPerClip;
		shootStartTime = -fireRate * 2;
	}
	
	void Update (){
	
		//run shot timer
		shootElapsedTime = Time.time - shootStartTime;
	
		if(shootElapsedTime >= fireRate){ 
			shooting = false;
		}
		
		//fire more shots per attack if burstShots or randomShots is greater than zero
		if(!doneShooting && shotsFired < (burstShots + randShotsAmt)){
			Fire();	
			if(!randBurstState){//get random number of shots to add to this burst for variation
				randShotsAmt = Random.Range(0,randomShots);
				randBurstState = true;
			}
		}else{//reset burst shooting vars
			doneShooting = true;
			shotsFired = 0;
			randBurstState = false;
		}
		
	}
	
	void LateUpdate (){
	
		//enable muzzle flash
		if (muzzleFlash){
			if(shootElapsedTime < fireRate / 3){ 
				if(mFlashState){
					muzzleFlash.enabled = true;
					mFlashState = false;
				}
			}else{
				if(!mFlashState){
					// We didn't, disable the muzzle flash
					muzzleFlash.enabled = false;
				}
			}
		}
	
	}
	
	void Fire (){
	
		if (bulletsLeft == 0){
			return;
		}
		
		//fire weapon
		if(!reloading){
			if(!shooting){
				FireOneShot();
				shootStartTime = Time.time;
				shooting = true;
				doneShooting = false;
			}else{
				if(shootElapsedTime >= fireRate){ 
					shooting = false;
				}
			}
		}else{
			shooting = false;
		}
	
	}
	
	void FireOneShot (){
	
		//Transform target = AIComponent.target;
		RaycastHit hit;
		
		Vector3 targetPos = new Vector3(AIComponent.lastVisiblePlayerPosition.x + Random.Range(-inaccuracy, inaccuracy), 
										AIComponent.lastVisiblePlayerPosition.y - (AIComponent.eyeHeight / 2) + Random.Range(-inaccuracy, inaccuracy), 
										AIComponent.lastVisiblePlayerPosition.z + Random.Range(-inaccuracy, inaccuracy));
		Vector3 targetDir = targetPos - myTransform.position;
		Vector3 rayOrigin = new Vector3(myTransform.position.x, myTransform.position.y + AIComponent.eyeHeight, myTransform.position.z);
		// Did we hit anything?
		if (Physics.Raycast(rayOrigin, targetDir, out hit, range)) {
			// Apply a force to the rigidbody we hit
			if (hit.rigidbody){
				hit.rigidbody.AddForceAtPosition(force * targetDir / (Time.fixedDeltaTime * 100.0f), hit.point);
			}
			// Place the particle system for spawing out of place where we hit the surface!
			// And spawn a couple of particles
			if (hitParticles 
			&& hit.collider.gameObject.layer != 11 
			&& hit.collider.gameObject.layer != 20
			&& hit.collider.gameObject.layer != 13) {
				hitParticles.transform.position = hit.point;
				hitParticles.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
				hitParticles.Emit();
			}
			
			//calculate damage amount
			damageAmt = Random.Range(damage, damage + damage);	
			
			//call the ApplyDamage() function in the script of the object hit
			switch(hit.collider.gameObject.layer){
				case 13://hit object is an NPC
					if(hit.collider.gameObject.GetComponent<CharacterDamage>()){
						hit.collider.gameObject.GetComponent<CharacterDamage>().ApplyDamage(damageAmt, Vector3.zero, myTransform.position);
					}
					break;
				case 9://hit object is an apple
					if(hit.collider.gameObject.GetComponent<AppleFall>()){
						hit.collider.gameObject.GetComponent<AppleFall>().ApplyDamage(damageAmt);
					}	
					break;
				case 19://hit object is a breakable or explosive object
					if(hit.collider.gameObject.GetComponent<BreakableObject>()){
						hit.collider.gameObject.GetComponent<BreakableObject>().ApplyDamage(damageAmt);
					}else if(hit.collider.gameObject.GetComponent<ExplosiveObject>()){
						hit.collider.gameObject.GetComponent<ExplosiveObject>().ApplyDamage(damageAmt);
					}else if(hit.collider.gameObject.GetComponent<MineExplosion>()){
						hit.collider.gameObject.GetComponent<MineExplosion>().ApplyDamage(damageAmt);
					}
					break;
				case 11://hit object is player
					if(hit.collider.gameObject.GetComponent<FPSPlayer>()){
						hit.collider.gameObject.GetComponent<FPSPlayer>().ApplyDamage(damageAmt);
					}	
					if(hit.collider.gameObject.GetComponent<LeanColliderDamage>()){
						hit.collider.gameObject.GetComponent<LeanColliderDamage>().ApplyDamage(damageAmt);
					}	
					break;
				case 20://hit object is player lean collider
					if(hit.collider.gameObject.GetComponent<FPSPlayer>()){
						hit.collider.gameObject.GetComponent<FPSPlayer>().ApplyDamage(damageAmt);
					}	
					if(hit.collider.gameObject.GetComponent<LeanColliderDamage>()){
						hit.collider.gameObject.GetComponent<LeanColliderDamage>().ApplyDamage(damageAmt);
					}	
					break;
				default:
					break;	
			}
			
		}

		GetComponent<AudioSource>().clip = firesnd;
		GetComponent<AudioSource>().pitch = Random.Range(fireSndRandPitch, 1);
		GetComponent<AudioSource>().PlayOneShot(GetComponent<AudioSource>().clip, 0.9f / GetComponent<AudioSource>().volume);
		
		//track ammo and fired shots amount
		//bulletsLeft--;
		shotsFired++;

		mFlashState=true;
		enabled = true;
		
		// Reload gun in reload Time		
//		if (bulletsLeft == 0){
//			Reload();	
//		}
		
	}
	
//	IEnumerator Reload (){
//		
//		if(ammo > 0){
//			audio.volume = 1.0f;
//			audio.pitch = 1.0f;
//			audio.PlayOneShot(reloadsnd, 1.0f / audio.volume);
//			
//			reloading = true;
//			// Wait for reload time first, then proceed
//			yield return new WaitForSeconds(reloadTime);
//			//set reloading var in ironsights script to true after reloading delay
//			reloading = false;
//	
//			// We have ammo left to reload
//			if(ammo >= bulletsPerClip){
//				ammo -= bulletsPerClip - bulletsLeft;
//				bulletsLeft = bulletsPerClip;
//			}else{
//				bulletsLeft += ammo;
//				ammo = 0;
//			}
//		}
//		
//	}
	
	private int GetBulletsLeft(){
		return bulletsLeft;
	}
}