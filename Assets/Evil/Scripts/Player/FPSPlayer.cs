//FPSPlayer.cs by Azuline StudiosÂ© All Rights Reserved
//Controls main player behaviors such as hitpoints and damage, HUD GUIText/Texture element instantiation and update,
//directs player button mappings to other scripts, handles item detection and pickup, and plays basic player sound effects.
using UnityEngine;
using System.Collections;

public class FPSPlayer : MonoBehaviour {
	[HideInInspector]
	public Ironsights IronsightsComponent;
	[HideInInspector]
	public InputControl InputComponent;
	[HideInInspector]
	public FPSRigidBodyWalker FPSWalkerComponent;
	[HideInInspector]
	public PlayerWeapons PlayerWeaponsComponent;
	[HideInInspector]
	public WorldRecenter WorldRecenterComponent;
	[HideInInspector]
	public WeaponBehavior WeaponBehaviorComponent;
	[HideInInspector]
	public	SmoothMouseLook MouseLookComponent;
	[HideInInspector]
	public	WeaponEffects WeaponEffectsComponent;
 	//other objects accessed by this script
	[HideInInspector]
	public GameObject[] children;//behaviors of these objects are deactivated when restarting the scene
	[HideInInspector]
	public GameObject weaponCameraObj;
	[HideInInspector]
	public GameObject weaponObj;
	[HideInInspector]
	public GameObject painFadeObj;
	[HideInInspector]
	public GameObject levelLoadFadeObj;
	[HideInInspector]
	public GameObject healthGuiObj;//this object is instantiated for heath display on hud
	[HideInInspector]
	public GameObject healthGuiObjInstance;
	[HideInInspector]
	public GameObject hungerGuiObj;//this object is instantiated for hunger display on hud
	[HideInInspector]
	public GameObject hungerGuiObjInstance;
	[HideInInspector]
	public GameObject thirstGuiObj;//this object is instantiated for thirst display on hud
	[HideInInspector]
	public GameObject thirstGuiObjInstance;
	[HideInInspector]
	public GameObject helpGuiObj;//this object is instantiated for help text display
	[HideInInspector]
	public GameObject helpGuiObjInstance;
	[HideInInspector]
	public GameObject PickUpGuiObj;//this object is instantiated for hand pick up crosshair on hud
	[HideInInspector]
	public GameObject PickUpGuiObjGuiObjInstance;
	[HideInInspector]
	public GameObject CrosshairGuiObj;//this object is instantiated for aiming reticle on hud
	[HideInInspector]
	public GameObject CrosshairGuiObjInstance;
	[HideInInspector]
	//public Projector shadow;//to access the player shadow projector 
	private AudioSource[]aSources;//access the audio sources attatched to this object as an array for playing player sound effects
	private Transform mainCamTransform;
	
	//player hit points
	public float hitPoints = 100.0f;
	public float maximumHitPoints = 200.0f;
	public bool regenerateHealth = false;//should the player regenerate their health?
	public float maxRegenHealth = 100.0f;//the maximum amount of hitpoints that should be regenerated
	public float healthRegenDelay = 7.0f;//delay after being damaged that the player should start to regenerate health
	public float healthRegenRate = 25.0f;//rate at which the player should regenerate health
	private float timeLastDamaged;//time that the player was last damaged
	
	//player hunger
	public bool usePlayerHunger;//true if player should have a hunger attribute that increases over time
	private float maxHungerPoints = 100.0f;//maximum amount that hunger will increase to before players starts to starve
	public float hungerInterval = 7.0f;//seconds it takes for player to accumulate 1 hunger point
	[HideInInspector]
	public float hungerPoints = 0.0f;//total hunger points 
	private float lastHungerTime;//time that last hunger point was applied
	private float lastStarveTime;//time that last starve damage was applied
	public float starveInterval = 3.0f;//seconds to wait before starve damaging again (should be less than healthRegenDelay to prevent healing of starvation damage)
	public float starveAmt = -5.0f;//amount to damage player per starve interval 
	
	//player thirst
	public bool usePlayerThirst;//true if player should have a thirst attribute that increases over time
	private float maxThirstPoints = 100.0f;//maximum amount that thirst will increase to before players starts to take thirst damage
	public float thirstInterval = 7.0f;//seconds it takes for player to accumulate 1 thirst point
	[HideInInspector]
	public float thirstPoints = 0.0f;//total thirst points 
	private float lastThirstTime;//time that last thirst point was applied
	private float lastThirstDamTime;//time that last thirst damage was applied
	public float thirstDamInterval = 3.0f;//seconds to wait before thirst damaging again (should be less than healthRegenDelay to prevent healing of thirst damage)
	public float thirstDamAmt = -5.0f;//amount to damage player per thirst damage interval 

	public bool allowBulletTime = true;
	public bool showHelpText = true;
	
	//Damage feedback
	private float gotHitTimer = -1.0f;
	private Color PainColor = new Color(0.221f, 0f, 0f, 0.44f);//color of pain screen flash can be selected in editor
	public Texture2D painTexture;
	private Color painFadeColor;//used to modify opacity of pain fade object
	public float painScreenKickAmt = 0.016f;//magnitude of the screen kicks when player takes damage
	
	//Bullet Time and Pausing
	public float bulletTimeSpeed = 0.35f;//decrease time to this speed when in bullet time
	private float pausedTime;//time.timescale value to return to after pausing
	[HideInInspector]
	public bool bulletTimeActive;
	
	//zooming
	private bool zoomBtnState = true;
	private float zoomStopTime = 0.0f;//track time that zoom stopped to delay making aim reticle visible again
	[HideInInspector]
	public bool zoomed = false;
	private float zoomStart = -2.0f;
	private bool zoomStartState = false;
	private float zoomEnd = 0.0f;
	private bool zoomEndState = false;
	private float zoomDelay = 0.4f;
	
	//crosshair 
	public bool crosshairEnabled = true;//enable or disable the aiming reticle
	private bool crosshairVisibleState = true;
	private bool crosshairTextureState = false;
	public bool useSwapReticle = true;//set to true to display swap reticle when item under reticle will replace current weapon
	public Texture2D aimingReticle;//the texture used for the aiming crosshair
	public Texture2D pickupReticle;//the texture used for the pick up crosshair
	public Texture2D swapReticle;//the texture used for when the weapon under reticle will replace current weapon
	public Texture2D noPickupReticle;//the texture used for showing that weapon under reticle cannot be picked up
	private Texture2D pickupTex;//the texture used for the pick up crosshair

	private Color pickupReticleColor = Color.white; 
	private Color reticleColor = Color.white; 
	//[HideInInspector]
	public LayerMask rayMask;//only layers to include for crosshair raycast in hit detection (for efficiency)
	
	//button and behavior states
	private bool pickUpBtnState = true;
	[HideInInspector]
	public bool restarting = false;//to notify other scripts that level is restarting
	
	//sound effects
	public AudioClip painLittle;
	public AudioClip painBig;
	public AudioClip painDrown;
	public AudioClip gasp;
	public AudioClip catchBreath;
	public AudioClip die;
	public AudioClip dieDrown;
	public AudioClip jumpfx;
	public AudioClip enterBulletTimeFx;
	public AudioClip exitBulletTimeFx;

	void Start (){	
		mainCamTransform = Camera.main.transform;
		//set up external script references
		IronsightsComponent = GetComponent<Ironsights>();
		InputComponent = GetComponent<InputControl>();
		FPSWalkerComponent = GetComponent<FPSRigidBodyWalker>();
		PlayerWeaponsComponent = weaponObj.GetComponent<PlayerWeapons>();
		WorldRecenterComponent = GetComponent<WorldRecenter>();
		MouseLookComponent = mainCamTransform.parent.transform.GetComponent<SmoothMouseLook>();
		WeaponEffectsComponent = weaponObj.GetComponent<WeaponEffects>();
		
		//Set time settings
		Time.timeScale = 1.0f;
		
		//Physics Layer Management Setup
		//these are the layer numbers and their corresponding uses/names accessed by the FPS prefab
		//	Weapon = 8;
		//	Ragdoll = 9;
		//	WorldCollision = 10;
		//	Player = 11;
		//	Objects = 12;
		//	NPCs = 13;
		//	GUICameraLayer = 14;
		//	WorldGeometry = 15;
		//	BulletMarks = 16;
		
		//player object collisions
		Physics.IgnoreLayerCollision(11, 12);//no collisions between player object and misc objects like bullet casings
		Physics.IgnoreLayerCollision (12, 12);//no collisions between bullet shells
		Physics.IgnoreLayerCollision(11, 9);//no collisions between player and ragdolls
		Physics.IgnoreLayerCollision(9, 13);//no collisions between ragdolls and NPCs
		
		//weapon object collisions
		Physics.IgnoreLayerCollision(8, 2);//
		Physics.IgnoreLayerCollision(8, 13);//no collisions between weapon and NPCs
		Physics.IgnoreLayerCollision(8, 12);//no collisions between weapon and Objects
		Physics.IgnoreLayerCollision(8, 11);//no collisions between weapon and Player
		Physics.IgnoreLayerCollision(8, 10);//no collisions between weapon and world collision
		Physics.IgnoreLayerCollision(8, 9);//no collisions between weapon and ragdolls

		//Call FadeAndLoadLevel fucntion with fadeIn argument set to true to tell the function to fade in (not fade out and (re)load level)
		GameObject llf = Instantiate(levelLoadFadeObj) as GameObject;
		llf.GetComponent<LevelLoadFade>().FadeAndLoadLevel(Color.black, 2.0f, true);
		
		//create instance of GUIText to display health amount on hud
		healthGuiObjInstance = Instantiate(healthGuiObj,Vector3.zero,transform.rotation) as GameObject;
		if(showHelpText){
			//create instance of GUIText to display help text
			helpGuiObjInstance = Instantiate(helpGuiObj,Vector3.zero,transform.rotation) as GameObject;
		}
		//create instance of GUITexture to display crosshair on hud
		CrosshairGuiObjInstance = Instantiate(CrosshairGuiObj,new Vector3(0.5f,0.5f,0.0f),transform.rotation) as GameObject;
		CrosshairGuiObjInstance.GetComponent<GUITexture>().texture = aimingReticle;
		//set alpha of hand pickup crosshair
		pickupReticleColor.a = 0.5f;
		//set alpha of aiming reticule and make it 100% transparent if crosshair is disabled
		if(crosshairEnabled){
			reticleColor.a = 0.25f;
		}else{
			//make alpha of aiming reticle zero/transparent
			reticleColor.a = 0.0f;
			//set alpha of aiming reticle at start to prevent it from showing, but allow item pickup hand reticle 
			CrosshairGuiObjInstance.GetComponent<GUITexture>().color = reticleColor;
		}
		
		//set reference for main color element of heath GUIText
		HealthText HealthText = healthGuiObjInstance.GetComponent<HealthText>();
		//set reference for shadow background color element of health GUIText
		//this object is a child of the main health GUIText object, so access it as an array
		HealthText[] HealthText2 = healthGuiObjInstance.GetComponentsInChildren<HealthText>();
		
		//initialize health amounts on GUIText objects
		HealthText.healthGui = hitPoints;
		HealthText2[1].healthGui = hitPoints;	
		
		if(usePlayerHunger){
			//create instance of GUIText to display hunger amount on hud
			hungerGuiObjInstance = Instantiate(hungerGuiObj,Vector3.zero,transform.rotation) as GameObject;
			//set reference for main color element of hunger GUIText
			HungerText HungerText = hungerGuiObjInstance.GetComponent<HungerText>();
			//set reference for shadow background color element of hunger GUIText
			//this object is a child of the main hunger GUIText object, so access it as an array
			HungerText[] HungerText2 = hungerGuiObjInstance.GetComponentsInChildren<HungerText>();
			
			//initialize hunger amounts on GUIText objects
			HungerText.hungerGui = hungerPoints;
			HungerText2[1].hungerGui = hungerPoints;	
		}
		
		if(usePlayerThirst){
			//create instance of GUIText to display thirst amount on hud
			thirstGuiObjInstance = Instantiate(thirstGuiObj,Vector3.zero,transform.rotation) as GameObject;
			//set reference for main color element of thirst GUIText
			ThirstText ThirstText = thirstGuiObjInstance.GetComponent<ThirstText>();
			//set reference for shadow background color element of thirst GUIText
			//this object is a child of the main thirst GUIText object, so access it as an array
			ThirstText[] ThirstText2 = thirstGuiObjInstance.GetComponentsInChildren<ThirstText>();
			
			//initialize thirst amounts on GUIText objects
			ThirstText.thirstGui = thirstPoints;
			ThirstText2[1].thirstGui = thirstPoints;	
		}
		
	}
	
	void Update (){

		AudioSource []aSources = GetComponents<AudioSource>();//Initialize audio source
		AudioSource otherfx = aSources[0] as AudioSource;

		if(InputComponent.pausePress){//Pause game when pause button is pressed
			if(Time.timeScale > 0){
				pausedTime = Time.timeScale;
				Time.timeScale = 0;
			}else{
				Time.timeScale = pausedTime;	
			}
		}
			
		if(allowBulletTime){//make bullet time an optional feature
			if(InputComponent.bulletTimePress){//set bulletTimeActive to true or false based on button input
				if(!bulletTimeActive){
					otherfx.clip = enterBulletTimeFx;
					otherfx.PlayOneShot(otherfx.clip, 1.0f);//play enter bullet time sound effect
					bulletTimeActive = true;
				}else{
					otherfx.clip = exitBulletTimeFx;
					otherfx.PlayOneShot(otherfx.clip, 1.0f);//play exit bullet time sound effect
					bulletTimeActive = false;
				}
			}	
					
			otherfx.pitch = Time.timeScale;//sync pitch of bullet time sound effects with Time.timescale
			
			if(Time.timeScale > 0){//decrease or increase Time.timescale when bulletTimeActive is true
				if(bulletTimeActive){
					Time.timeScale = Mathf.MoveTowards(Time.timeScale, bulletTimeSpeed, Time.deltaTime * 3.0f);
				}else{
					if(1.0f - Mathf.Abs(Time.timeScale) > 0.05f){//make sure that timescale returns to exactly 1.0f 
						Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1.0f, Time.deltaTime * 3.0f);
					}else{
						Time.timeScale = 1.0f;
					}
				}
			}
		}
		
		//set zoom mode to toggle, hold, or both, based on inspector setting
		switch (IronsightsComponent.zoomMode){
			case Ironsights.zoomType.both:
				zoomDelay = 0.4f;
			break;
			case Ironsights.zoomType.hold:
				zoomDelay = 0.0f;
			break;
			case Ironsights.zoomType.toggle:
				zoomDelay = 999.0f;
			break;
		}
		
		//regenerate player health if regenerateHealth var is true
		if(regenerateHealth){
			if(hitPoints < maxRegenHealth && timeLastDamaged + healthRegenDelay < Time.time){
				HealPlayer(healthRegenRate * Time.deltaTime);	
			}
		}
		
		//apply player hunger if usePlayerHunger var is true
		if(usePlayerHunger){
			//increase player hunger 
			if(lastHungerTime + hungerInterval < Time.time){
				UpdateHunger(1.0f);
			}
			//calculate and apply starvation damage to player
			if(hungerPoints == maxHungerPoints 
			&& lastStarveTime + starveInterval < Time.time
			&& hitPoints > 0.0f){
				//use a negative heal amount to prevent unneeded damage effects of ApplyDamage function
				HealPlayer(starveAmt);
				//fade screen red when taking starvation damage
				GameObject pf = Instantiate(painFadeObj) as GameObject;//Create instance of painFadeObj
				pf.GetComponent<PainFade>().FadeIn(PainColor, painTexture, 0.75f);//Call FadeIn function in painFadeObj to fade screen red when damage taken
				//Call Die function if player's hitpoints have been depleted
				if (hitPoints < 1.0f){
					SendMessage("Die");//use SendMessage() to allow other script components on this object to detect player death
				}
				//update starvation timers
				timeLastDamaged = Time.time;
				lastStarveTime = Time.time;
			}
			
		}
		
		//apply player thirst if usePlayerThirst var is true
		if(usePlayerThirst){
			//increase player hunger 
			if(lastThirstTime + thirstInterval < Time.time){
				UpdateThirst(1.0f);
			}
			//calculate and apply starvation damage to player
			if(thirstPoints == maxThirstPoints 
			&& lastThirstDamTime + thirstDamInterval < Time.time
			&& hitPoints > 0.0f){
				//use a negative heal amount to prevent unneeded damage effects of ApplyDamage function
				HealPlayer(thirstDamAmt);
				//fade screen red when taking starvation damage
				GameObject pf = Instantiate(painFadeObj) as GameObject;//Create instance of painFadeObj
				pf.GetComponent<PainFade>().FadeIn(PainColor, painTexture, 0.75f);//Call FadeIn function in painFadeObj to fade screen red when damage taken
				//Call Die function if player's hitpoints have been depleted
				if (hitPoints < 1.0f){
					Die();
				}
				//update starvation timers
				timeLastDamaged = Time.time;
				lastThirstDamTime = Time.time;
			}
			
		}
		
	}
	
	void FixedUpdate (){
		//set up external script references
		WeaponBehavior WeaponBehaviorComponent = PlayerWeaponsComponent.CurrentWeaponBehaviorComponent;
		
		//Exit application if escape is pressed
		if (InputComponent.exitHold){
			Application.Quit();
		}
		
		//Restart level if v is pressed
		if (InputComponent.restartHold){
			Time.timeScale = 1.0f;//set timescale to 1.0f so fadeout wont take longer if bullet time is active
			GameObject llf = Instantiate(levelLoadFadeObj) as GameObject;//Create instance of levelLoadFadeObj
			//call FadeAndLoadLevel function with fadein argument set to false 
			//in levelLoadFadeObj to restart level and fade screen out from black on level load
			llf.GetComponent<LevelLoadFade>().FadeAndLoadLevel(Color.black, 2.0f, false);
			//set restarting var to true to be accessed by FPSRigidBodyWalker script to stop rigidbody movement
			restarting = true;
			// Disable all scripts to deactivate player control upon player death
			foreach(var c in children) {
				Component[] coms = c.GetComponentsInChildren<MonoBehaviour>();
				foreach(var b in coms) {
					MonoBehaviour p = b as MonoBehaviour;
					if (p){
						p.enabled = false;
					}
				}
			}

		}
		
		//toggle or hold zooming state by determining if zoom button is pressed or held
		if(InputComponent.zoomHold
		&& WeaponBehaviorComponent.canZoom 
		&& !IronsightsComponent.reloading
		&& !FPSWalkerComponent.proneMove//no zooming while crawling
		&& !FPSWalkerComponent.hideWeapon){
			if(!zoomStartState){
				zoomStart = Time.time;//track time that zoom button was pressed
				zoomStartState = true;//perform these actions only once
				zoomEndState = false;
				if(zoomEnd - zoomStart < zoomDelay * Time.timeScale){//if button is tapped, toggle zoom state
					if(!zoomed){
						zoomed = true;
					}else{
						zoomed = false;	
					}
				}
			}
		}else{
			if(!zoomEndState){
				zoomEnd = Time.time;//track time that zoom button was released
				zoomEndState = true;
				zoomStartState = false;
				if(zoomEnd - zoomStart > zoomDelay * Time.timeScale){//if releasing zoom button after holding it down, stop zooming
					zoomed = false;	
				}
			}
		}
		
		//cancel zooming while crawling
		if(FPSWalkerComponent.proneMove){
			zoomEndState = true;
			zoomStartState = false;
			zoomed = false;	
		}
		
		//track when player stopped zooming to allow for delay of reticle becoming visible again
		if (zoomed){
			zoomBtnState = false;//only perform this action once per button press
		}else{
			if(!zoomBtnState){
				zoomStopTime = Time.time;
				zoomBtnState = true;
			}
		}
		
		//enable and disable crosshair based on various states like reloading and zooming
		if(IronsightsComponent.reloading || zoomed){
			//don't disable reticle if player is using a melee weapon or if player is unarmed
			if(WeaponBehaviorComponent.meleeSwingDelay == 0 && !WeaponBehaviorComponent.unarmed){
				if(crosshairVisibleState){
					//disable the GUITexture element of the instantiated crosshair object
					//and set state so this action will only happen once.
					CrosshairGuiObjInstance.GetComponent<GUITexture>().enabled = false;
					crosshairVisibleState = false;
				}
			}
		}else{
			//Because of the method that is used for non magazine reloads, an additional check is needed here
			//to make the reticle appear after the last bullet reload time has elapsed. Proceed with no check
			//for magazine reloads.
			if((WeaponBehaviorComponent.bulletsPerClip != WeaponBehaviorComponent.bulletsToReload 
				&& WeaponBehaviorComponent.reloadLastStartTime + WeaponBehaviorComponent.reloadLastTime < Time.time)
			|| WeaponBehaviorComponent.bulletsPerClip == WeaponBehaviorComponent.bulletsToReload){
				//allow a delay before enabling crosshair again to let the gun return to neutral position
				//by checking the zoomStopTime value
				if(!crosshairVisibleState && (zoomStopTime + 0.2f < Time.time)){
					CrosshairGuiObjInstance.GetComponent<GUITexture>().enabled = true;
					crosshairVisibleState = true;
				}
			}
		}
		
		if(crosshairEnabled){
			if(WeaponBehaviorComponent.showAimingCrosshair){
				reticleColor.a = 0.25f;
				CrosshairGuiObjInstance.GetComponent<GUITexture>().color = reticleColor;
			}else{
				//make alpha of aiming reticle zero/transparent
				reticleColor.a = 0.0f;
				//set alpha of aiming reticle at start to prevent it from showing, but allow item pickup hand reticle 
				CrosshairGuiObjInstance.GetComponent<GUITexture>().color = reticleColor;
			}
		}else{
			reticleColor.a = 0.0f;
			CrosshairGuiObjInstance.GetComponent<GUITexture>().color = reticleColor;
		}
				
		//Pick up items		
		RaycastHit hit;
		if(!IronsightsComponent.reloading//no item pickup when reloading
		&& !WeaponBehaviorComponent.lastReload//no item pickup when when reloading last round in non magazine reload
		&& !PlayerWeaponsComponent.switching//no item pickup when switching weapons
		&& !FPSWalkerComponent.holdingObject//don't pick up objects if player is dragging them
		&& (!FPSWalkerComponent.canRun || FPSWalkerComponent.inputY == 0)//no item pickup when sprinting
			//there is a small delay between the end of canRun and the start of sprintSwitching (in PlayerWeapons script),
			//so track actual time that sprinting stopped to avoid the small time gap where the pickup hand shows briefly
		&& ((FPSWalkerComponent.sprintStopTime + 0.4f) < Time.time)){
			//raycast a line from the main camera's origin using a point extended forward from camera position/origin as a target to get the direction of the raycast
			//and scale the distance of the raycast based on the playerHeightMod value in the FPSRigidbodyWalker script 
			if (Physics.Raycast(mainCamTransform.position, ((mainCamTransform.position + mainCamTransform.forward * (5.0f + (FPSWalkerComponent.playerHeightMod * 0.25f))) - mainCamTransform.position).normalized, out hit, 2.1f + FPSWalkerComponent.playerHeightMod, rayMask)) {
				if(hit.collider.gameObject.tag == "Usable"){//if the object hit by the raycast is a pickup item and has the "Usable" tag
					
					if (pickUpBtnState && InputComponent.useHold){
						//run the PickUpItem function in the pickup object's script
						hit.collider.SendMessageUpwards("PickUpItem", SendMessageOptions.DontRequireReceiver);
						//run the ActivateObject function of this object's script if it has the "Usable" tag
						hit.collider.SendMessageUpwards("ActivateObject", SendMessageOptions.DontRequireReceiver);
						pickUpBtnState = false;
						FPSWalkerComponent.cancelSprint = true;
					}
					
					//determine if pickup item is using a custom pickup reticle and if so set pickupTex to custom reticle
					if(pickUpBtnState){//check pickUpBtnState to prevent reticle from briefly showing custom/general pickup icon briefly when picking up last weapon before maxWeapons are obtained
						
						//determine if item under reticle is a weapon pickup
						if(hit.collider.gameObject.GetComponent<WeaponPickup>()){
							//set up external script references
							WeaponBehavior PickupWeaponBehaviorComponent = PlayerWeaponsComponent.weaponOrder[hit.collider.gameObject.GetComponent<WeaponPickup>().weaponNumber].GetComponent<WeaponBehavior>();
							WeaponPickup WeaponPickupComponent = hit.collider.gameObject.GetComponent<WeaponPickup>();
							
							if(PlayerWeaponsComponent.totalWeapons == PlayerWeaponsComponent.maxWeapons//player has maximum weapons
							&& PickupWeaponBehaviorComponent.addsToTotalWeaps){//weapon adds to total inventory
								
								//player does not have weapon under reticle
								if(!PickupWeaponBehaviorComponent.haveWeapon
								//and weapon under reticle hasn't been picked up from an item with removeOnUse set to false
								&& !PickupWeaponBehaviorComponent.dropWillDupe){	
									
									if(!useSwapReticle){//if useSwapReticle is true, display swap reticle when item under reticle will replace current weapon
										if(WeaponPickupComponent.weaponPickupReticle){
											//display custom weapon pickup reticle if the weapon item has one defined
											pickupTex = WeaponPickupComponent.weaponPickupReticle;	
										}else{
											//weapon has no custom pickup reticle, just show general pickup reticle 
											pickupTex = pickupReticle;
										}
									}else{
										//display weapon swap reticle if player has max weapons and can swap held weapon for pickup under reticle
										pickupTex = swapReticle;
									}
									
								}else{
									
									//weapon under reticle is not removed on use and is in player's inventory, so show cantPickup reticle
									if(!WeaponPickupComponent.removeOnUse){
										
										pickupTex = noPickupReticle;
										
									}else{//weapon is removed on use, so show standard or custom pickup reticle
										
										if(WeaponPickupComponent.weaponPickupReticle){
											//display custom weapon pickup reticle if the weapon item has one defined
											pickupTex = WeaponPickupComponent.weaponPickupReticle;	
										}else{
											//weapon has no custom pickup reticle, just show general pickup reticle 
											pickupTex = pickupReticle;
										}
										
									}
									
								}
							}else{//total weapons not at maximum and weapon under reticle does not add to inventory
								
								if(!PickupWeaponBehaviorComponent.haveWeapon
								&& !PickupWeaponBehaviorComponent.dropWillDupe
								|| WeaponPickupComponent.removeOnUse){
									
									if(WeaponPickupComponent.weaponPickupReticle){
										//display custom weapon pickup reticle if the weapon item has one defined
										pickupTex = WeaponPickupComponent.weaponPickupReticle;	
									}else{
										//weapon has no custom pickup reticle, just show general pickup reticle 
										pickupTex = pickupReticle;
									}
									
								}else{
									pickupTex = noPickupReticle;
								}
								
							}
						//determine if item under reticle is a health pickup	
						}else if(hit.collider.gameObject.GetComponent<HealthPickup>()){
							//set up external script references
							HealthPickup HealthPickupComponent = hit.collider.gameObject.GetComponent<HealthPickup>();
							
							if(HealthPickupComponent.healthPickupReticle){
								pickupTex = HealthPickupComponent.healthPickupReticle;	
							}else{
								pickupTex = pickupReticle;
							}
						//determine if item under reticle is an ammo pickup
						}else if(hit.collider.gameObject.GetComponent<AmmoPickup>()){
							//set up external script references
							AmmoPickup AmmoPickupComponent = hit.collider.gameObject.GetComponent<AmmoPickup>();
							
							if(AmmoPickupComponent.ammoPickupReticle){
								pickupTex = AmmoPickupComponent.ammoPickupReticle;	
							}else{
								pickupTex = pickupReticle;
							}
						}else{
							pickupTex = pickupReticle;
						}
					}
					
					UpdateReticle(false);//show pickupReticle if raycast hits a pickup item

				}else{
					if(crosshairTextureState){
						UpdateReticle(true);//show aiming reticle crosshair if item is not a pickup item
					}
				}
			}else{
				if(crosshairTextureState){
					UpdateReticle(true);//show aiming reticle crosshair if raycast hits nothing
				}
			}
		}else{
			if(crosshairTextureState){
				UpdateReticle(true);//show aiming reticle crosshair if reloading, switching weapons, or sprinting
			}
		}
		
		//only register one press of E key to make player have to press button again to pickup items instead of holding E
		if (InputComponent.useHold){
			pickUpBtnState = false;
		}else{
			pickUpBtnState = true;	
		}
	
	}
	
	//set reticle type based on the boolean value passed to this function
	void UpdateReticle( bool reticleType ){
		if(!reticleType){
			CrosshairGuiObjInstance.GetComponent<GUITexture>().texture = pickupTex;
			CrosshairGuiObjInstance.GetComponent<GUITexture>().color = pickupReticleColor;
			crosshairTextureState = true;
		}else{
			CrosshairGuiObjInstance.GetComponent<GUITexture>().texture = aimingReticle;
			CrosshairGuiObjInstance.GetComponent<GUITexture>().color = reticleColor;
			crosshairTextureState = false;	
		}
	}
	
	//add hitpoints to player health
	public void HealPlayer( float healAmt ){
			
		if (hitPoints < 1.0f){//Don't add health if player is dead
			return;
		}
		
		//Update health GUIText 
		HealthText HealthText = healthGuiObjInstance.GetComponent<HealthText>();
		HealthText[] HealthText2 = healthGuiObjInstance.GetComponentsInChildren<HealthText>();
		
		//Apply healing
		if(hitPoints + healAmt > maximumHitPoints){ 
			hitPoints = maximumHitPoints;
		}else{
			hitPoints += healAmt;
		}
			
		//set health hud value to hitpoints remaining
		HealthText.healthGui = Mathf.Round(hitPoints);
		HealthText2[1].healthGui = Mathf.Round(hitPoints);
			
		//change color of hud health element based on hitpoints remaining
		if (hitPoints <= 25.0f){
			HealthText.GetComponent<GUIText>().material.color = Color.red;
		}else if (hitPoints <= 40.0f){
				HealthText.GetComponent<GUIText>().material.color = Color.yellow;	
		}else{
			HealthText.GetComponent<GUIText>().material.color = HealthText.textColor;	
		}

	}
	
	//update the hunger amount for the player
	public void UpdateHunger( float hungerAmt ){
		
		if (hitPoints < 1.0f){//Don't add hunger if player is dead
			return;
		}
		
		//Update hunger GUIText 
		HungerText HungerText = hungerGuiObjInstance.GetComponent<HungerText>();
		HungerText[] HungerText2 = hungerGuiObjInstance.GetComponentsInChildren<HungerText>();
		
		//Apply hungerAmt
		if(hungerPoints + hungerAmt > maxHungerPoints){ 
			hungerPoints = maxHungerPoints;
		}else{
			hungerPoints += hungerAmt;
		}
			
		//set hunger hud value to hunger points remaining
		HungerText.hungerGui = Mathf.Round(hungerPoints);
		HungerText2[1].hungerGui = Mathf.Round(hungerPoints);
			
		//change color of hud hunger element based on hunger points
		if (hungerPoints <= 65.0f){
			HungerText.GetComponent<GUIText>().material.color = HungerText.textColor;
		}else if (hungerPoints <= 85.0f){
				HungerText.GetComponent<GUIText>().material.color = Color.yellow;	
		}else{
			HungerText.GetComponent<GUIText>().material.color = Color.red;	
		}
		
		lastHungerTime = Time.time;	
	}
	
	//update the thirst amount for the player
	public void UpdateThirst( float thirstAmt ){
		
		if (hitPoints < 1.0f){//Don't add thirst if player is dead
			return;
		}
		
		//Update thirst GUIText 
		ThirstText ThirstText = thirstGuiObjInstance.GetComponent<ThirstText>();
		ThirstText[] ThirstText2 = thirstGuiObjInstance.GetComponentsInChildren<ThirstText>();
		
		//Apply thirstAmt
		if(thirstPoints + thirstAmt > maxThirstPoints){ 
			thirstPoints = maxThirstPoints;
		}else{
			thirstPoints += thirstAmt;
		}
			
		//set thirst hud value to thirst points remaining
		ThirstText.thirstGui = Mathf.Round(thirstPoints);
		ThirstText2[1].thirstGui = Mathf.Round(thirstPoints);
			
		//change color of hud thirst element based on thirst points
		if (thirstPoints <= 65.0f){
			ThirstText.GetComponent<GUIText>().material.color = ThirstText.textColor;
		}else if (thirstPoints <= 85.0f){
				ThirstText.GetComponent<GUIText>().material.color = Color.yellow;	
		}else{
			ThirstText.GetComponent<GUIText>().material.color = Color.red;	
		}
		
		lastThirstTime = Time.time;
	}
	
	//remove hitpoints from player health
	public void ApplyDamage ( float damage ){

		float appliedPainKickAmt;
			
		if (hitPoints < 1.0f){//Don't apply damage if player is dead
			return;
		}
		
		timeLastDamaged = Time.time;
		
		//Update health GUIText 
		HealthText HealthText = healthGuiObjInstance.GetComponent<HealthText>();
		HealthText[] HealthText2 = healthGuiObjInstance.GetComponentsInChildren<HealthText>();

	    Quaternion painKickRotation;//Set up rotation for pain view kicks
	    int painKickUpAmt = 0;
	    int painKickSideAmt = 0;

		hitPoints -= damage;//Apply damage
			
		//set health hud value to hitpoints remaining
		HealthText.healthGui = Mathf.Round(hitPoints);
		HealthText2[1].healthGui = Mathf.Round(hitPoints);
			
		//change color of hud health element based on hitpoints remaining
		if (hitPoints <= 25.0f){
			HealthText.GetComponent<GUIText>().material.color = Color.red;
		}else if (hitPoints <= 40.0f){
				HealthText.GetComponent<GUIText>().material.color = Color.yellow;	
		}else{
			HealthText.GetComponent<GUIText>().material.color = HealthText.textColor;	
		}
		
		GameObject pf = Instantiate(painFadeObj) as GameObject;//Create instance of painFadeObj
		painFadeColor = PainColor;
		painFadeColor.a = (damage / 5.0f);//fade pain overlay based on damage amount
		pf.GetComponent<PainFade>().FadeIn(painFadeColor, painTexture, 0.75f);//Call FadeIn function in painFadeObj to fade screen red when damage taken
			
		if(!FPSWalkerComponent.holdingBreath){
			//Play pain sound when getting hit
			if (Time.time > gotHitTimer && painBig && painLittle) {
				// Play a big pain sound
				if (hitPoints < 40.0f || damage > 30.0f) {
					AudioSource.PlayClipAtPoint(painBig, mainCamTransform.position);
					gotHitTimer = Time.time + Random.Range(.5f, .75f);
				} else {
					//Play a small pain sound
					AudioSource.PlayClipAtPoint(painLittle, mainCamTransform.position);
					gotHitTimer = Time.time + Random.Range(.5f, .75f);
				}
			}
		}else{
			if (Time.time > gotHitTimer && painDrown) {
				//Play a small pain sound
				AudioSource.PlayClipAtPoint(painDrown, mainCamTransform.position);
				gotHitTimer = Time.time + Random.Range(.5f, .75f);
			}	
		}
		
		painKickUpAmt = Random.Range(100, -100);//Choose a random view kick up amount
		if(painKickUpAmt < 50 && painKickUpAmt > 0){painKickUpAmt = 50;}//Maintain some randomness of the values, but don't make it too small
		if(painKickUpAmt < 0 && painKickUpAmt > -50){painKickUpAmt = -50;}
		
		painKickSideAmt = Random.Range(100, -100);//Choose a random view kick side amount
		if(painKickSideAmt < 50 && painKickSideAmt > 0){painKickSideAmt = 50;}
		if(painKickSideAmt < 0 && painKickSideAmt > -50){painKickSideAmt = -50;}
		
		//create a rotation quaternion with random pain kick values
		painKickRotation = Quaternion.Euler(mainCamTransform.localRotation.eulerAngles - new Vector3(painKickUpAmt, painKickSideAmt, 0));
		
		//make screen kick amount based on the damage amount recieved
		if(zoomed){
			appliedPainKickAmt = (damage / (painScreenKickAmt * 10)) / 3;	
		}else{
			appliedPainKickAmt = (damage / (painScreenKickAmt * 10));			
		}
		
		//make sure screen kick is not so large that view rotates past arm models 
		appliedPainKickAmt = Mathf.Clamp(appliedPainKickAmt, 0.0f, 0.15f); 
		
		//smooth current camera angles to pain kick angles using Slerp
		mainCamTransform.localRotation = Quaternion.Slerp(mainCamTransform.localRotation, painKickRotation, appliedPainKickAmt );
	
		//Call Die function if player's hitpoints have been depleted
		if (hitPoints < 1.0f){
			SendMessage("Die");//use SendMessage() to allow other script components on this object to detect player death
		}
	}
	
	void Die (){
		
		bulletTimeActive = false;//set bulletTimeActive to false so fadeout wont take longer if bullet time is active
		
		if(!FPSWalkerComponent.drowning){
			AudioSource.PlayClipAtPoint(die, mainCamTransform.position);//play normal player death sound effect if the player is on land 
		}else{
			AudioSource.PlayClipAtPoint(dieDrown, mainCamTransform.position);//play drowning sound effect if the player is underwater 	
		}
		
		//disable player control and sprinting on death
		FPSWalkerComponent.inputX = 0;
		FPSWalkerComponent.inputY = 0;
		FPSWalkerComponent.cancelSprint = true;
			
		GameObject llf = Instantiate(levelLoadFadeObj) as GameObject;//Create instance of levelLoadFadeObj
		//call FadeAndLoadLevel function with fadein argument set to false 
		//in levelLoadFadeObj to restart level and fade screen out from black on level load
		llf.GetComponent<LevelLoadFade>().FadeAndLoadLevel(Color.black, 2.0f, false);
		
	}

}