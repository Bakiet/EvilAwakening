//Footsteps.cs by Azuline Studios© All Rights Reserved
//Plays footstep sounds by surface type.
using UnityEngine;
using System.Collections;

public class Footsteps : MonoBehaviour {
	private FPSRigidBodyWalker FPSWalkerComponent;
	[HideInInspector]
	public GameObject playerObj;
	[HideInInspector]
	private Transform cameraTransform;
	public string materialType;//tag of object that player is standing on (Metal, Wood, Dirt, Stone, Water)
	private float volumeAmt = 1.0f;//volume of audio clip to be played
	private AudioClip footStepClip;//audio clip to be played 
	//player movement sounds and volume amounts
	public float dirtStepVol = 1.0f;
	public AudioClip[] dirtSteps;
	public float woodStepVol = 1.0f;
	public AudioClip[] woodSteps;
	public float metalStepVol = 1.0f;
	public AudioClip[] metalSteps;
	public float waterSoundVol = 1.0f;
	public AudioClip[] waterSounds;
	public float climbSoundVol = 1.0f;
	public AudioClip[] climbSounds;
	public float stoneStepVol = 1.0f;
	public AudioClip[] stoneSteps;
	public float proneStepVol = 1.0f;
	public AudioClip[] proneSteps;
		
	public AudioClip dirtLand;
	public AudioClip metalLand;
	public AudioClip woodLand;
	public AudioClip waterLand;
	public AudioClip stoneLand;
	
	void Start () {
		playerObj = transform.gameObject;
		cameraTransform = Camera.main.transform;
		FPSWalkerComponent = playerObj.GetComponent<FPSRigidBodyWalker>();
	}
	
	public void FootstepSfx (){
		//play footstep sound effects
		if(!FPSWalkerComponent.prone){
			if(!FPSWalkerComponent.climbing){
				if(FPSWalkerComponent.inWater){//play swimming/wading footstep effects
					if(waterSounds.Length > 0){
						footStepClip = waterSounds[Random.Range(0, waterSounds.Length)];//select random water step effect from waterSounds array
						volumeAmt = waterSoundVol;//set volume of audio clip to customized amount
						AudioSource.PlayClipAtPoint(footStepClip, cameraTransform.position, volumeAmt);
					}
				}else{
					//Make a short delay before playing footstep sounds to allow landing sound to play
					if (FPSWalkerComponent.grounded && (FPSWalkerComponent.landStartTime + 0.3f) < Time.time){
						switch(materialType){//determine which material the player is standing on and select random footstep effect for surface type
						case "Wood":
							if(woodSteps.Length > 0){
								footStepClip = woodSteps[Random.Range(0, woodSteps.Length)];
								volumeAmt = woodStepVol;
							}
							break;
						case "Metal":
							if(metalSteps.Length > 0){
								footStepClip = metalSteps[Random.Range(0, metalSteps.Length)];
								volumeAmt = metalStepVol;
							}
							break;
						case "Dirt":
							if(dirtSteps.Length > 0){
								footStepClip = dirtSteps[Random.Range(0, dirtSteps.Length)];
								volumeAmt = dirtStepVol;
							}
							break;
						case "Stone":
							if(stoneSteps.Length > 0){
								footStepClip = stoneSteps[Random.Range(0, stoneSteps.Length)];
								volumeAmt = stoneStepVol;
							}
							break;
						default:
							if(dirtSteps.Length > 0){
								footStepClip = dirtSteps[Random.Range(0, dirtSteps.Length)];
								volumeAmt = dirtStepVol;
							}
							break;	
						}
						if(footStepClip){
							//play the sound effect
							AudioSource.PlayClipAtPoint(footStepClip, cameraTransform.position, volumeAmt);
						}
					}
				}
			}else{//play climbing footstep effects
				if(climbSounds.Length > 0){
					footStepClip = climbSounds[Random.Range(0, climbSounds.Length)];
					volumeAmt = climbSoundVol;
					AudioSource.PlayClipAtPoint(footStepClip, cameraTransform.position, volumeAmt);
				}
			}
		}else{
			if(proneSteps.Length > 0){
				footStepClip = proneSteps[Random.Range(0, proneSteps.Length)];
				volumeAmt = proneStepVol;
				AudioSource.PlayClipAtPoint(footStepClip, cameraTransform.position, volumeAmt);
			}
		}
	}
}
