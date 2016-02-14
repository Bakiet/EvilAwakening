//PlayerWeapons.cs by Azuline StudiosÂ© All Rights Reserved
//Switches and drops weapons and sets weapon parent object position. 
using UnityEngine;
using System.Collections;

public class PlayerWeapons : MonoBehaviour {
	//set up external script references
	private InputControl InputComponent;
	private FPSRigidBodyWalker FPSWalkerComponent;
	private FPSPlayer FPSPlayerComponent;
	[HideInInspector]
	public WeaponBehavior CurrentWeaponBehaviorComponent;
	private Ironsights IronsightsComponent;
	private CameraKick CameraKickComponent;
	public int firstWeapon = 0;//the weaponOrder index of the first weapon that will be selected when the map loads
	//backupWeapon is the weaponOrder index number of a weapon like fists, a knife, or sidearm that player will select when all other weapons are dropped
	//this weapon should also have its "droppable" and "addsToTotalWeaps" values in its WeaponBehavior.cs component set to false
	public int backupWeapon = 1;
	public int maxWeapons = 10;
	[HideInInspector]
	public int totalWeapons;
	[HideInInspector]
	public int currentWeapon;//index of weaponOrder array that corresponds to current weapon 
	//Define array for storing order of weapons. This array is created in the inspector by dragging and dropping 
	//weapons from under the FPSWeapons branch in the FPS Prefab. Weapon 0 should always be the unarmed/null weapon.
	public GameObject[] weaponOrder;
	
	//objects accessed by this script
	[HideInInspector]
	public GameObject playerObj;
	[HideInInspector]
	public GameObject cameraObj;
	[HideInInspector]
	public GameObject weaponObj;
	[HideInInspector]
	public GameObject ammoGuiObj;//this GUI object will be instantiated on level load to display player ammo
	[HideInInspector]
	public GameObject ammoGuiObjInstance;
	private Transform myTransform;
	private Transform mainCamTransform;
	[HideInInspector]
	public Color waterMuzzleFlashColor;
	
	//weapon switching
	[HideInInspector]
	public float switchTime = 0.0f;//time that weapon switch started
	[HideInInspector]
	public float sprintSwitchTime = 0.0f;//time that weapon sprinting animation started, set in WeaponBehavior script
	[HideInInspector]
	public bool switching = false;//true when switching weapons
	[HideInInspector]
	public bool sprintSwitching = false;//true when weapon sprinting animation is playing
	private bool proneMove;

	private bool dropWeapon;
	private bool deadDropped;
	
	//sound effects
	public AudioClip changesnd;
	private bool audioPaused;// used to pause and resume reloading sound based on timescale/game pausing state
	
	private AudioSource []aSources;

	void Start (){

		myTransform = transform;//define transforms for efficiency
		mainCamTransform = Camera.main.transform;
		
		//set up external script references
		InputComponent = playerObj.GetComponent<InputControl>();
		FPSWalkerComponent = playerObj.GetComponent<FPSRigidBodyWalker>();
		FPSPlayerComponent = playerObj.GetComponent<FPSPlayer>();
		IronsightsComponent = playerObj.GetComponent<Ironsights>();
		CameraKickComponent = mainCamTransform.GetComponent<CameraKick>();
		CurrentWeaponBehaviorComponent = weaponOrder[firstWeapon].GetComponent<WeaponBehavior>();
		
		WeaponBehavior BackupWeaponBehaviorComponent = weaponOrder[backupWeapon].GetComponent<WeaponBehavior>();

		aSources = transform.GetComponents<AudioSource>();

		//Create instance of GUIText to display ammo amount on hud. This will be accessed and updated by WeaponBehavior script.
		ammoGuiObjInstance = Instantiate(ammoGuiObj,Vector3.zero,myTransform.rotation) as GameObject;
		
		//set the weapon order number in the WeaponBehavior scripts
		for(int i = 0; i < weaponOrder.Length; i++)	{
			weaponOrder[i].GetComponent<WeaponBehavior>().weaponNumber = i;
		}
		
		//Select first weapon, if firstWeapon is not in inventory, player will spawn unarmed.
		if(weaponOrder[firstWeapon].GetComponent<WeaponBehavior>().haveWeapon){
			StartCoroutine(SelectWeapon(firstWeapon));
		}else{
			StartCoroutine(SelectWeapon(0));	
		}
		
		//set droppable value for backup weapon to false here if it was set to true in inspector 
		//to prevent multiple instances of backup weapon from being dropped and not selecting next weapon
		BackupWeaponBehaviorComponent.droppable = false;
		//set addsToTotalWeaps value for backup weapon to false here if it was set to true in inspector
		//to prevent picking up a backup weapon from swapping current weapon
		BackupWeaponBehaviorComponent.addsToTotalWeaps = false;
		
		UpdateTotalWeapons();

	}
	
	void Update (){
		
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		//Switch Weapons
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			
		if(Time.timeSinceLevelLoad > 2.0f//don't allow weapon switching when level is still loading/fading out
		&& !(!FPSWalkerComponent.grounded && FPSWalkerComponent.sprintActive)//don't allow switching if player is sprinting and airborn
		&& !switching//only allow one weapon switch at once
		&& (!CurrentWeaponBehaviorComponent.shooting || FPSPlayerComponent.hitPoints < 1.0f)//don't switch weapons if shooting
		&& !FPSWalkerComponent.holdingObject//don't allow switching if player is holding an object
		&& !sprintSwitching){//don't allow weapon switching while sprint anim is active/transitioning
			
			//drop weapons
			if((InputComponent.dropPress || InputComponent.xboxDpadDownPress)
			&& currentWeapon != 0
			&& !FPSWalkerComponent.sprintActive
			&& CurrentWeaponBehaviorComponent.droppable
			&& !CurrentWeaponBehaviorComponent.dropWillDupe){//if drop button is pressed and weapon isn't holstered (null weap 0 selected)
				DropWeapon(currentWeapon);		
			}
			
			//drop current weapon if player dies
			if(FPSPlayerComponent.hitPoints < 1.0f && !deadDropped){
				CurrentWeaponBehaviorComponent.droppable = true;
				if(CurrentWeaponBehaviorComponent.muzzleFlash){
					CurrentWeaponBehaviorComponent.muzzleFlash.GetComponent<Renderer>().enabled = false;
				}
				deadDropped = true;
				DropWeapon(currentWeapon);	
			}
		  	
		  	//Cycle weapons using the mousewheel (cycle through FPS Weapon children) and skip weapons that are not in player inventory.
			//weaponOrder.Length - 1 is the last weapon because the built in array starts counting at zero and weaponOrder.Length starts counting at one (index 0 of weaponOrder[] is null/unarmed weapon). 
			if (InputComponent.mouseWheel < 0 || InputComponent.selectPrevPress || InputComponent.xboxDpadLeftPress){//mouse wheel down or previous weapon button pressed
				if(currentWeapon != 0){//not starting at zero
					for(int i = currentWeapon; i > -1; i--){
						if(weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon && i != currentWeapon){//check that player has weapon and it is not currently selected weapon
							StartCoroutine(SelectWeapon(i));//run the SelectWeapon function with the next weapon index that was found
							break;
						}else if(i == 0){//reached zero, count backwards from end of list to find next weapon
							for(int n = weaponOrder.Length - 1; n > -1; n--){
								if(weaponOrder[n].GetComponent<WeaponBehavior>().haveWeapon && n != currentWeapon){
									StartCoroutine(SelectWeapon(n));
									break;
								}
							}
						}
					}
				}else{//starting at 0
					for (int i = weaponOrder.Length - 1; i > -1; i--){
						if(weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon && i != currentWeapon){
							StartCoroutine(SelectWeapon(i));
							break;
						}
					}
				}
			}else if(InputComponent.mouseWheel > 0 //mouse wheel up
			|| InputComponent.selectNextPress//select next weapon button pressed
			|| InputComponent.xboxDpadRightPress
			|| (dropWeapon && totalWeapons != 0)){//drop weapon button pressed and player has weapons in their inventory
				if(currentWeapon < weaponOrder.Length -1){//not starting at last weapon
					for(int i = currentWeapon; i < weaponOrder.Length; i++){
						//cycle weapon selection manually
						if((weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon 
						&& i != currentWeapon && !dropWeapon) 
						//do not select backupWeapon if dropping a weapon and automatically selecting the next weapon
						//but allow backupWeapon to be selected when cycling weapon selection manually
						|| (weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon 
						&& i != currentWeapon && i != backupWeapon && dropWeapon)){
							StartCoroutine(SelectWeapon(i));
							break;
						}else if(i == weaponOrder.Length - 1){//reached end of list, count forwards from zero to find next weapon
							for(int n = 0; n < weaponOrder.Length - 1; n++){
								//cycle weapon selection manually
								if((weaponOrder[n].GetComponent<WeaponBehavior>().haveWeapon 
								&& n != currentWeapon && !dropWeapon) 
								//do not select backupWeapon if dropping a weapon and automatically selecting the next weapon
								//but allow backupWeapon to be selected when cycling weapon selection manually
								|| (weaponOrder[n].GetComponent<WeaponBehavior>().haveWeapon 
								&& n != currentWeapon && n != backupWeapon && dropWeapon)){
									StartCoroutine(SelectWeapon(n));
									break;
								}
							}
						}
					}
				}else{//starting at last weapon
					for(int i = 0; i < weaponOrder.Length - 1; i++){
						//cycle weapon selection manually
						if((weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon 
						&& i != currentWeapon && !dropWeapon) 
						//do not select backupWeapon if dropping a weapon and automatically selecting the next weapon
						//but allow backupWeapon to be selected when cycling weapon selection manually
						|| (weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon 
						&& i != currentWeapon && i != backupWeapon && dropWeapon)){
							StartCoroutine(SelectWeapon(i));
							break;
						}
					}
				}	
			}
			
			//select weapons with number keys
			if (InputComponent.holsterPress) {
				if(currentWeapon != 0){StartCoroutine(SelectWeapon(0));}
			}else if (InputComponent.selectWeap1Press && weaponOrder.Length - 1 > 0) {
				if(currentWeapon != 1){StartCoroutine(SelectWeapon(1));}
			}else if (InputComponent.selectWeap2Press && weaponOrder.Length - 1 > 1) {
				if(currentWeapon != 2){StartCoroutine(SelectWeapon(2));}
			}else if (InputComponent.selectWeap3Press && weaponOrder.Length - 1 > 2) {
				if(currentWeapon != 3){StartCoroutine(SelectWeapon(3));}
			}else if (InputComponent.selectWeap4Press && weaponOrder.Length - 1 > 3) {
				if(currentWeapon != 4){StartCoroutine(SelectWeapon(4));}
			}else if (InputComponent.selectWeap5Press && weaponOrder.Length - 1 > 4) {
				if(currentWeapon != 5){StartCoroutine(SelectWeapon(5));}
			}else if (InputComponent.selectWeap6Press && weaponOrder.Length - 1 > 5) {
				if(currentWeapon != 6){StartCoroutine(SelectWeapon(6));}
			}else if (InputComponent.selectWeap7Press && weaponOrder.Length - 1 > 6) {
				if(currentWeapon != 7){StartCoroutine(SelectWeapon(7));}
			}else if (InputComponent.selectWeap8Press && weaponOrder.Length - 1 > 7) {
				if(currentWeapon != 8){StartCoroutine(SelectWeapon(8));}
			}else if (InputComponent.selectWeap9Press && weaponOrder.Length - 1 > 8) {
				if(currentWeapon != 9){StartCoroutine(SelectWeapon(9));}
			}
			
		}
		
		//check timer for switch to prevent shooting
		//this var checked in "WeaponBehavior" script in the Fire() function 
		if(switchTime + 0.87f > Time.time){
			switching = true;
		}else{
			switching = false;
		}
		
		//define time that sprinting anim is active/transitioning to disable weapon switching
		if(sprintSwitchTime + 0.44f > Time.time){
			sprintSwitching = true;
		}else{
			sprintSwitching = false;
		}
		
		//pause and resume reloading sound based on timescale/game pausing state
		if(Time.timeScale > 0){
			if(audioPaused){
				aSources[1].Play();	
				audioPaused = false;
			}
		}else{
			if(!audioPaused && aSources[1].isPlaying){
				aSources[1].Pause();
				audioPaused = true;
			}
		}
		
	}
	
	//check if the player has a weapon in their inventory (can be used in other scripts)
//	private bool CheckWeapon(string weaponName){
//		//find the PlayerWeapons script in the FPS Prefab to access weaponOrder array
//		PlayerWeapons PlayerWeaponsComponent = Camera.main.transform.GetComponent<CameraKick>().weaponObj.GetComponent<PlayerWeapons>();
//		//iterate through the children of the FPS Weapons object (PlayerWeapon's weaponOrder array) 
//		//and check if player has a certain weapon by gameObject string name like "AK47" or "MP5"
//		for (int i = 0; i < PlayerWeaponsComponent.weaponOrder.Length; i++)	{
//			if(PlayerWeaponsComponent.weaponOrder[i].name == weaponName && PlayerWeaponsComponent.weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon){
//				return true;//player has the weapon with the string name "weaponName", return true
//			}
//		}
//		return false;//player does not have the weapon with the string name "weaponName", return false
//	}
	
	//set weapon parent position in LateUpdate to sync with CameraKick.cs LateUpdate actions
	void LateUpdate (){
		//align weapon parent origin with player camera origin
		Vector3 tempGunPosition = new Vector3(mainCamTransform.position.x, mainCamTransform.position.y,mainCamTransform.position.z);
		myTransform.position = tempGunPosition;
	}
	
	public void DropWeapon ( int weapon ){
		
		float dropVel;//var to allow velocity to be added to weapon if dropped while moving
		
		//set haveWeapon value to false for this weapon to remove it from player's inventory
		weaponOrder[weapon].GetComponent<WeaponBehavior>().haveWeapon = false;
		
		aSources[1].Stop();//prevent reload sound from playing after dropping weapon
		
		//modify drop velocity based on player speed so the weapon doesn't fly far away 
		//if player is moving backwards or drop behind player when moving
		if(!deadDropped){
			if(FPSWalkerComponent.inputY > 0){
				dropVel = 8.0f;	
			}else if(FPSWalkerComponent.inputY < 0){
				dropVel = 2.0f;	
			}else{
				dropVel = 4.0f;
			}
		}else{
			if(FPSWalkerComponent.inputY > 0){
				dropVel = 4.0f;
			}else{
				dropVel = 2.0f;
			}
		}
		
		if((currentWeapon != backupWeapon || deadDropped) && currentWeapon != 0){//only drop backup weapon if player dies
			//set dropWeapon value to true for weapon switch code to check below
			dropWeapon = true;
		}
		
		UpdateTotalWeapons();
		
		//instantiate weaponDropObj from WeaponBehavios.cs at camera position
		if(weaponOrder[weapon].GetComponent<WeaponBehavior>().weaponDropObj){
			GameObject weaponObjDrop = Instantiate(weaponOrder[weapon].GetComponent<WeaponBehavior>().weaponDropObj, mainCamTransform.position + playerObj.transform.forward * 0.25f + Vector3.up * -0.25f, mainCamTransform.rotation) as GameObject;
			//add forward velocity to dropped weapon in the direction that the player currently faces 
			weaponObjDrop.GetComponent<Rigidbody>().AddForce(playerObj.transform.forward * dropVel, ForceMode.Impulse);
			//add random rotation to the dropped weapon
			float rotateAmt;
			if(Random.value > 0.5f){rotateAmt = 7.0f;}else{rotateAmt = -7.0f;}
			weaponObjDrop.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * rotateAmt,ForceMode.Impulse);
			if(Random.value > 0.5f){rotateAmt = 7.0f;}else{rotateAmt = -7.0f;}
			weaponObjDrop.GetComponent<Rigidbody>().AddRelativeTorque(Vector3.right * rotateAmt,ForceMode.Impulse);
		}

		//if player dropped their last weapon, just select the null/holstered weapon 
		if(!deadDropped){
			if(totalWeapons == 0 && currentWeapon != 0 && currentWeapon != backupWeapon){
				if(weaponOrder[backupWeapon].GetComponent<WeaponBehavior>().haveWeapon && FPSPlayerComponent.hitPoints > 1.0f){
					StartCoroutine(SelectWeapon(backupWeapon));
				}else{
					StartCoroutine(SelectWeapon(0));	
				}
			}
		}else{
			StartCoroutine(SelectWeapon(0));//don't select another weapon if player died
		}
		
	}
	
	public void UpdateTotalWeapons (){
		totalWeapons = 0;//initialize totalWeapons value at zero because a weapon could have been picked up since we checked last
		for(int i = 1; i < weaponOrder.Length; i++){//iterate through weaponOrder array and count total weapons in player's inventory
			if(weaponOrder[i].GetComponent<WeaponBehavior>().haveWeapon && weaponOrder[i].GetComponent<WeaponBehavior>().addsToTotalWeaps){
				totalWeapons ++;//increment totalWeapons by one if player has this weapon	
			}
		}
	}
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//Select Weapons
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	public IEnumerator SelectWeapon ( int index  ){
		
		//we are not dropping a weapon anymore if one has been selected
		dropWeapon = false;
		
		//do not proceed with selecting weapon if player doesn't have it in their inventory
		//but make an exception for the null/unarmed weapon for when the player presses the holster button
		//also dont allow weapon switch if player is climbing, swimming, or holding object and their weapon is lowered
		if((!weaponOrder[index].GetComponent<WeaponBehavior>().haveWeapon && index != 0) 
		|| FPSWalkerComponent.hideWeapon){
			yield break;
		}
		
		if(index != 0){//if a weapon is selected, prevent unarmed/null weapon from being selected in selection cycle 
			weaponOrder[0].GetComponent<WeaponBehavior>().haveWeapon = false;
		}
		
		//cancel zooming when switching
		FPSPlayerComponent.zoomed = false;
		
		//reset non-magazine reload if interrupted by weapon switch
		if(CurrentWeaponBehaviorComponent.bulletsToReload != CurrentWeaponBehaviorComponent.bulletsPerClip 
		&& CurrentWeaponBehaviorComponent.weaponMesh.GetComponent<Animation>()["Neutral"]
		&& IronsightsComponent.reloading){
			//play neutral animation when putting weapon away to prevent neutral anim glitch at start of next reload
			CurrentWeaponBehaviorComponent.weaponMesh.GetComponent<Animation>()["Neutral"].speed = 1.5f;
			CurrentWeaponBehaviorComponent.weaponMesh.GetComponent<Animation>().Play("Neutral", PlayMode.StopSameLayer);
			//reset bulletsReloaded to prevent delay of reloading the next time we reload this weapon
			CurrentWeaponBehaviorComponent.bulletsReloaded = 0;
		}
		
		//cancel reloading when switching
		IronsightsComponent.reloading = false;//set IronSights Reloading var to false
		CurrentWeaponBehaviorComponent.StopCoroutine("Reload");//stop the Reload function if it is running
		
		//make timer active during switch to prevent shooting 
		switchTime = Time.time - Time.deltaTime;
		
		if(Time.timeSinceLevelLoad > 2){
			//play weapon switch sound if not the first call to this function after level load
			AudioSource.PlayClipAtPoint(changesnd, mainCamTransform.position);
		
			//play camera weapon switching animation
			Camera.main.GetComponent<Animation>().Rewind("CameraSwitch");
			Camera.main.GetComponent<Animation>().CrossFade("CameraSwitch", 0.35f,PlayMode.StopSameLayer);
		}
		
		//if weapon uses rifle sprinting animation, set speed and play animation
		if(!CurrentWeaponBehaviorComponent.PistolSprintAnim){
			//animate previous weapon down
			if(!FPSWalkerComponent.canRun && !FPSWalkerComponent.proneMove){
				weaponOrder[currentWeapon].GetComponent<Animation>()["RifleSprinting"].normalizedTime = 0;
				weaponOrder[currentWeapon].GetComponent<Animation>()["RifleSprinting"].speed = 1.5f;
				weaponOrder[currentWeapon].GetComponent<Animation>().CrossFade("RifleSprinting", 0.00025f,PlayMode.StopSameLayer);
			}else{
				//if player is sprinting, keep weapon in sprinting position during weapon switch
				weaponOrder[currentWeapon].GetComponent<Animation>()["RifleSprinting"].normalizedTime = 1;
			}
		}else{//weapon uses pistol sprinting animation
			//animate previous weapon down
			if(!FPSWalkerComponent.canRun && !FPSWalkerComponent.proneMove){
				weaponOrder[currentWeapon].GetComponent<Animation>()["PistolSprinting"].normalizedTime = 0;
				weaponOrder[currentWeapon].GetComponent<Animation>()["PistolSprinting"].speed = 1.5f;
				weaponOrder[currentWeapon].GetComponent<Animation>().CrossFade("PistolSprinting", 0.00025f,PlayMode.StopSameLayer);
			}else{
				//if player is sprinting, keep weapon in sprinting position during weapon switch
				weaponOrder[currentWeapon].GetComponent<Animation>()["PistolSprinting"].normalizedTime = 1;
			}
		}
			
		if(Time.timeSinceLevelLoad > 2){
			if(CurrentWeaponBehaviorComponent.meleeSwingDelay == 0){
				//move weapon down while switching
				IronsightsComponent.switchMove = -0.4f;
			}else{
				//move melee weapons down further while switching because they take more vertical screen space than guns
				IronsightsComponent.switchMove = -1.2f;
			}
			
			//wait for weapon down animation to play before switching weapons and animating weapon up
			yield return new WaitForSeconds(0.2f);
			
		}
		
		//immediately switch weapons (activate called weaponOrder index and deactivate all others)
		for (int i = 0; i < weaponOrder.Length; i++){
			if (i == index){
			
				
				#if UNITY_3_5
					// Activate the selected weapon
					weaponOrder[i].SetActiveRecursively(true);
				#else
					// Activate the selected weapon
					weaponOrder[i].SetActive(true);
				#endif
				
				//get current weapon value from index
				currentWeapon = index;
			
				//synchronize current and previous weapon's y pos for correct offscreen switching, use localPosition not position for correct transforms
				weaponOrder[i].transform.localPosition = weaponOrder[i].transform.localPosition + new Vector3(0, weaponOrder[i].transform.localPosition.y - 0.3f, 0);
				
				if(Time.timeSinceLevelLoad > 2){
					//move weapon up when switch finishes
					IronsightsComponent.switchMove = 0;
				}
				
				//if weapon uses rifle sprinting animation set speed and animate 
				if(!weaponOrder[i].GetComponent<WeaponBehavior>().PistolSprintAnim){
					//animate selected weapon up by setting time of animation to it's end and playing in reverse
					if(!FPSWalkerComponent.canRun && !FPSWalkerComponent.proneMove){
						weaponOrder[i].GetComponent<Animation>()["RifleSprinting"].normalizedTime = 1.0f;	
						weaponOrder[i].GetComponent<Animation>()["RifleSprinting"].speed = -1.5f;
						weaponOrder[i].GetComponent<Animation>().CrossFade("RifleSprinting", 0.00025f,PlayMode.StopSameLayer);
					}else{
						//if player is sprinting, keep weapon in sprinting position during weapon switch
						weaponOrder[i].GetComponent<Animation>()["RifleSprinting"].normalizedTime = 1.0f;
						weaponOrder[i].GetComponent<Animation>()["RifleSprinting"].speed = 1.5f;
						weaponOrder[i].GetComponent<Animation>().CrossFade("RifleSprinting", 0.00025f,PlayMode.StopSameLayer);	
					}
				}else{//weapon uses pistol sprinting animation
					//animate selected weapon up by setting time of animation to it's end and playing in reverse
					if(!FPSWalkerComponent.canRun && !FPSWalkerComponent.proneMove){
						weaponOrder[i].GetComponent<Animation>()["PistolSprinting"].normalizedTime = 1.0f;	
						weaponOrder[i].GetComponent<Animation>()["PistolSprinting"].speed = -1.5f;
						weaponOrder[i].GetComponent<Animation>().CrossFade("PistolSprinting", 0.00025f,PlayMode.StopSameLayer);
					}else{
						//if player is sprinting, keep weapon in sprinting position during weapon switch
						weaponOrder[i].GetComponent<Animation>()["PistolSprinting"].normalizedTime = 1.0f;
						weaponOrder[i].GetComponent<Animation>()["PistolSprinting"].speed = 1.5f;
						weaponOrder[i].GetComponent<Animation>().CrossFade("PistolSprinting", 0.00025f,PlayMode.StopSameLayer);	

					}	
				}
			
				//update transform reference of active weapon object in other scipts
				IronsightsComponent.gun = weaponOrder[i].transform;
				//update active weapon object reference in other scipts
				IronsightsComponent.gunObj = weaponOrder[i];
				IronsightsComponent.WeaponBehaviorComponent = weaponOrder[i].GetComponent<WeaponBehavior>();
				FPSPlayerComponent.WeaponBehaviorComponent = weaponOrder[i].GetComponent<WeaponBehavior>();
				CameraKickComponent.gun = weaponOrder[i];
				CurrentWeaponBehaviorComponent = weaponOrder[i].GetComponent<WeaponBehavior>();
	
			}else{
				
				//reset transform of deactivated gun to make it in neutral position when selected again
				//use weapon parent transform.position instead of Camera.main.transform.position
				//or Camera.main.transform.localPosition to avoid positioning bugs due to camera pos changing with walking bob and kick 
				weaponOrder[i].transform.position = myTransform.position;
				
				if(!weaponOrder[i].GetComponent<WeaponBehavior>().PistolSprintAnim){//weapon uses rifle sprinting animation
					//reset animation
					weaponOrder[i].GetComponent<Animation>()["RifleSprinting"].normalizedTime = 1.0f;
				}else{//weapon uses pistol sprinting animation
					//reset animation
					weaponOrder[i].GetComponent<Animation>()["PistolSprinting"].normalizedTime = 1.0f;
				}
				//synchronize sprintState var in WeaponBehavior script
				weaponOrder[i].GetComponent<WeaponBehavior>().sprintState = true;
				
				#if UNITY_3_5
					// Activate the selected weapon
					weaponOrder[i].SetActiveRecursively(false);
				#else
					// Activate the selected weapon
					weaponOrder[i].SetActive(false);
				#endif
			}	
		}	
	}
}