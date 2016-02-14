//AI.cs by Azuline StudiosÂ© All Rights Reserved
//Allows NPC to track and attack targets and patrol waypoints.
using UnityEngine;
using System.Collections;

// Make sure there is always a character controller
[RequireComponent(typeof(CharacterController))]

public class AI : MonoBehaviour {
	
	[HideInInspector]
	public GameObject playerObj;
	[HideInInspector]
	public GameObject weaponObj;//currently equipped weapon object of player
	private FPSRigidBodyWalker FPSWalker;
	private PlayerWeapons PlayerWeaponsComponent;
	private WeaponBehavior WeaponBehaviorComponent;
	private CharacterController controller;	
	
	public Transform objectWithAnims;//the object with the Animation component automatically created by the character mesh's import settings
	public float randomSpawnChance = 1.0f;
	
	//NPC movement speeds
	public float runSpeed = 6.0f;//movement speed of the NPC
	public float walkSpeed = 1.0f;
	public float walkAnimSpeed = 1.0f;
	public float runAnimSpeed = 1.0f;
	private float speedAmt = 1.0f;
	public float pushPower = 5.0f;//physics force to apply to rigidbodies blocking NPC path
	public float rotationSpeed = 5.0f;
	
	//targeting and attacking
	public bool targetPlayer = true;//to determine if NPC should ignore player
	public float shootRange = 15.0f;//minimum range to target for attack
	public float attackRange = 30.0f;//range that NPC will start chasing target until they are within shootRange
	public float listenRange = 30.0f;//range that NPC will hear player attacks
	public float shotDuration = 0.0f;//time between shots (longer for burst weapons)
	public float shootAnimSpeed = 1.0f;//time for NPC to hold up weapon when firing (longer for burst weapons)
	[HideInInspector]
	public float attackRangeAmt = 30.0f;//increased by character damage script if NPC is damaged by player
	public float sneakRangeMod = 0.4f;//reduce NPC's attack range by sneakRangeMod amount when player is sneaking
	private float shootAngle = 3.0f;
	public float dontComeCloserRange = 5.0f;
	public float delayShootTime = 0.35f;
	public float eyeHeight = 0.4f;//height of rayCast starting point/origin which detects player (can be raised if NPC origin is at their feet)
	private float pickNextWaypointDistance = 2.0f;
	[HideInInspector]
	public Transform target;
	private float lastSearchTime;//delay between NPC checks for target, for efficiency

	private float targetDistance;
	private bool pursueTarget;
	[HideInInspector]
	public Vector3 lastVisiblePlayerPosition;
	private float timeout = 3.0f;
	private bool heardPlayer;
	
	//waypoints and patrolling
	private Transform myTransform;
	public bool doPatrol = true;
	public bool patrolOnce;
	public bool walkOnPatrol = true;
	public  AutoWayPoint curWayPoint;
	public AutoWayPoint firstWayPoint;//first waypoint that this NPC should patrol, continuing on to other wayponts with same group number 
	private LayerMask searchMask = 0;//only layers to include in target search (for efficiency)
	private bool  countBackwards = false;
	
	void OnEnable (){
		
		myTransform = transform;
		timeout = 3.0f;
		Mathf.Clamp01(randomSpawnChance);
	
		// Activate the npc based on randomSpawnChance
		if(Random.value > randomSpawnChance){
			Destroy(myTransform.gameObject);
		}else{
			
			//set layermask to layers 10 (world collision) and 19 (interactive objects) for target detection 
			searchMask = ~(~(1 << 10) & ~(1 << 19));
		
			//if there is no objectWithAnims defined, use the Animation Component attached to this game object
			if(objectWithAnims == null){objectWithAnims = transform;}
	
			// Set all animations to loop
			objectWithAnims.GetComponent<Animation>().wrapMode = WrapMode.Loop;
			// Except our action animations, Dont loop those
			objectWithAnims.GetComponent<Animation>()["shoot"].wrapMode = WrapMode.Once;
			// Put idle and run in a lower layer. They will only animate if our action animations are not playing
			objectWithAnims.GetComponent<Animation>()["idle"].layer = -1;
			objectWithAnims.GetComponent<Animation>()["walk"].layer = -1;
			objectWithAnims.GetComponent<Animation>()["run"].layer = -1;
			
			objectWithAnims.GetComponent<Animation>()["walk"].speed = walkAnimSpeed;
			objectWithAnims.GetComponent<Animation>()["shoot"].speed = shootAnimSpeed;
			objectWithAnims.GetComponent<Animation>()["run"].speed = runAnimSpeed;
			
			objectWithAnims.GetComponent<Animation>().Stop();
		
			//initialize AI vars
			playerObj = Camera.main.transform.GetComponent<CameraKick>().playerObj;
			PlayerWeaponsComponent = Camera.main.transform.GetComponent<CameraKick>().weaponObj.GetComponentInChildren<PlayerWeapons>();
			FPSWalker = playerObj.GetComponent<FPSRigidBodyWalker>();
			controller = GetComponent<CharacterController>();	
			attackRangeAmt = attackRange;
			objectWithAnims.GetComponent<Animation>().CrossFade("idle", 0.3f);
			// Auto setup player as target
			if(target == null && playerObj && targetPlayer){
				target = playerObj.transform;
			}
			if(doPatrol){
				curWayPoint = firstWayPoint;
				StartCoroutine(Patrol());
			}else{
				StartCoroutine(StandWatch());
			}
			
			if(!targetPlayer){
				//ignore collisions with player if NPC is not targeting player to prevent physics oddities
				myTransform.gameObject.layer = 9;
			}
		}
	}
	
	IEnumerator StandWatch (){
		
		while (true) {
			//play idle animation
			objectWithAnims.GetComponent<Animation>().CrossFade("idle", 0.3f);
			
			//if NPC spawns in the air, move their character controller to the ground
			if(!controller.isGrounded){ 
				Vector3 down = myTransform.TransformDirection(-Vector3.up);
				controller.SimpleMove(down);
			}else{	
				if(lastSearchTime + 0.75f < Time.time){
					lastSearchTime = Time.time;
					if (CanSeeTarget()){
						yield return StartCoroutine(AttackPlayer());
					}
				}
			}
			
			yield return new WaitForFixedUpdate ();
		}
	}
	
	IEnumerator Patrol (){
		if(curWayPoint){//patrol if NPC has a current waypoint, otherwise stand watch
			while (true) {
				Vector3 waypointPosition = curWayPoint.transform.position;
				// Are we close to a waypoint? -> pick the next one!
				if(curWayPoint.waypointNumber == curWayPoint.connected.Count && patrolOnce){
					if(Vector3.Distance(waypointPosition, myTransform.position) < pickNextWaypointDistance){
						StartCoroutine(StandWatch());
						yield break;//cancel patrol if patrolOnce var is true
					}
				}else{	
					if(Vector3.Distance(waypointPosition, myTransform.position) < pickNextWaypointDistance){
						curWayPoint = PickNextWaypoint (curWayPoint);
					}
				}
				
				//if NPC spawns in the air, move their character controller to the ground
				if(!controller.isGrounded){ 
					Vector3 down = myTransform.TransformDirection(-Vector3.up);
					controller.SimpleMove(down);
				}else{	
					//determine if player is within sight of NPC
					if(target && lastSearchTime + 0.75f < Time.time){
						lastSearchTime = Time.time;
						if(CanSeeTarget()){
							yield return StartCoroutine(AttackPlayer());
						}
					}
				}
				//determine if NPC should walk or run on patrol
				if(walkOnPatrol){speedAmt = walkSpeed;}else{speedAmt = runSpeed;}
				// Move towards our target
				MoveTowards(waypointPosition);
				
				yield return new WaitForFixedUpdate();
			}
		}else{
			StartCoroutine(StandWatch());
			yield break;
		}
	}
	
	bool CanSeeTarget(){
		
		if(FPSWalker.crouched){
			attackRangeAmt = attackRange * sneakRangeMod;//reduce NPC's attack range by sneakRangeMod amount when player is crouched
		}else if(FPSWalker.prone){
			attackRangeAmt = attackRange * (sneakRangeMod * 0.75f);//reduce NPC's attack range further when player is prone
		}else{
			attackRangeAmt = attackRange;
		}
		
		targetDistance = Vector3.Distance(myTransform.position, target.position);
		
		//listen for player attacks
		if(!heardPlayer){
			if(targetDistance < listenRange){
				WeaponBehaviorComponent = PlayerWeaponsComponent.CurrentWeaponBehaviorComponent;
				if(WeaponBehaviorComponent.shooting && !WeaponBehaviorComponent.silentShots){
					attackRange = attackRange * 6.0f;
					heardPlayer = true;
				}
			}
		}
			
		if(targetDistance > attackRangeAmt){
			return false;
		}
		
		//perform linecast to determine if NPC can see main player capsule collider or player lean collider 
		RaycastHit hit;
		if(Mathf.Abs(FPSWalker.leanAmt) > 0.1f 
		&& !Physics.Linecast (myTransform.position + myTransform.up * (1.0f + eyeHeight), FPSWalker.leanObj.transform.position, out hit, searchMask)){
				pursueTarget = true;//true when NPC has seen only the player leaning around a corner
				return true;
		}else if(!Physics.Linecast (myTransform.position + myTransform.up * (1.0f + eyeHeight), target.position, out hit, searchMask)){
				pursueTarget = false;
				return true;
		}else{
			return false;
		}
		
	}
	
	IEnumerator Shoot (){
		// Start shoot animation
		objectWithAnims.GetComponent<Animation>().CrossFade("shoot", 0.3f);
		speedAmt = 0.0f;
		SetSpeed(0.0f);
		// Wait until half the animation has played
		yield return new WaitForSeconds(delayShootTime);
		// Fire gun
		BroadcastMessage("Fire");
		// Wait for the rest of the animation to finish
		yield return new WaitForSeconds(objectWithAnims.GetComponent<Animation>()["shoot"].length - delayShootTime + Random.Range(shotDuration, shotDuration + 0.75f));
	}
	
	IEnumerator AttackPlayer (){
		while (true) {
			if(CanSeeTarget()){
				// Target is dead - stop hunting
				if(target == null){
					speedAmt = walkSpeed;
					yield break;
				}
				
				if(pursueTarget){
					lastVisiblePlayerPosition = FPSWalker.leanObj.transform.position;
				}else{
					lastVisiblePlayerPosition = target.position;
				}
				
				lastVisiblePlayerPosition.y = lastVisiblePlayerPosition.y - 0.25f;
				
				// Target is too far away - give up	
				float distance = Vector3.Distance(myTransform.position, target.position);
				if(distance > attackRangeAmt){
					speedAmt = walkSpeed;
					yield break;
				}
				
				speedAmt = runSpeed;
				timeout = 3.0f;
				if(distance > dontComeCloserRange){
					MoveTowards (lastVisiblePlayerPosition);
				}else{
					RotateTowards(lastVisiblePlayerPosition);
				}
				Vector3 forward = myTransform.TransformDirection(Vector3.forward);
				Vector3 targetDirection = lastVisiblePlayerPosition - myTransform.position;
				targetDirection.y = 0;
	
				float angle = Vector3.Angle(targetDirection, forward);
				
				// Start shooting if close and player is in sight
				if(distance < shootRange && angle < shootAngle){
					yield return StartCoroutine(Shoot());
				}
				
			}else{
				if(timeout > 0.0f){
					speedAmt = runSpeed;
					yield return StartCoroutine(SearchPlayer(lastVisiblePlayerPosition));
				}else{
					if(!CanSeeTarget()){
						speedAmt = walkSpeed;
						yield break;
					}
				}
			}
	
			yield return 0;//dont wait any frames for smooth NPC movement while attacking player
		}
	}
	
	IEnumerator SearchPlayer ( Vector3 position  ){
		// Run towards the player but after 3 seconds timeout and go back to Patroling

		while(timeout > 0.0f){
	
			// We found the player or reached place we saw them last
			if(CanSeeTarget() || MoveTowards(position)){
				yield break;
			}
			timeout -= Time.deltaTime;

			yield break;//dont wait any frames for smooth NPC movement while searching for player
		}
		yield break;
	}
	
	void RotateTowards ( Vector3 position  ){

		SetSpeed(0.0f);
		
		Vector3 direction = position - myTransform.position;
		direction.y = 0;
		if(direction.magnitude < 0.1f){
			return;
		}
		// Rotate towards the target
		myTransform.rotation = Quaternion.Slerp (myTransform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime * 10);
		myTransform.eulerAngles = new Vector3(0, myTransform.eulerAngles.y, 0);
	}
	
	bool MoveTowards ( Vector3 position  ){
		Vector3 direction = position - myTransform.position;
		direction.y = 0;
		if(direction.magnitude < 0.5f){
			SetSpeed(0.0f);
			return true;
		}
		
		// Rotate towards the target
		myTransform.rotation = Quaternion.Slerp (myTransform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
		myTransform.eulerAngles = new Vector3(0, myTransform.eulerAngles.y, 0);
		// Modify speed so we slow down when we are not facing the target
		Vector3 forward = myTransform.TransformDirection(Vector3.forward);
		float speedModifier = Vector3.Dot(forward, direction.normalized);
		speedModifier = Mathf.Clamp01(speedModifier);
		// Move the character
		direction = forward * speedAmt * speedModifier;
		controller.SimpleMove(direction);
	
		SetSpeed(speedAmt * speedModifier);
		
		return false;
		
	}
	
	//pick the next waypoint and determine if patrol 
	//should continue forward or backward through waypoint group
	AutoWayPoint PickNextWaypoint ( AutoWayPoint currentWaypoint  ){
	
		AutoWayPoint best = currentWaypoint;
	
		for (int i = currentWaypoint.connected.Count - 1; i > -1; i--){
			if(!countBackwards){
				if(currentWaypoint.waypointNumber != currentWaypoint.connected.Count){
					if(currentWaypoint.waypointNumber + 1 == currentWaypoint.connected[i].waypointNumber){
						best = currentWaypoint.connected[i];
						break;
					}
				}else{
					if(currentWaypoint.waypointNumber -1 == currentWaypoint.connected[i].waypointNumber){
						best = currentWaypoint.connected[i];
						countBackwards = true;
						break;
					}
				}
			}else{
				if(currentWaypoint.waypointNumber != 1){
					if(currentWaypoint.waypointNumber - 1 == currentWaypoint.connected[i].waypointNumber){
						best = currentWaypoint.connected[i];
						break;
					}
				}else{
					if(currentWaypoint.waypointNumber + 1 == currentWaypoint.connected[i].waypointNumber){
						best = currentWaypoint.connected[i];
						countBackwards = false;
						break;
					}
				}
			
			}
			
			
		}
		
		return best;
	}
	
	//allow the NPCs to push rigidbodies in their path
	void OnControllerColliderHit ( ControllerColliderHit hit  ){
	    Rigidbody body = hit.collider.attachedRigidbody;
	    // no rigidbody
	    if (body == null || body.isKinematic || body.gameObject.layer == 11){
	        return;
		}
	        
	    // We dont want to push objects below us
	    if (hit.moveDirection.y < -0.3f) 
	        return;
	    
	    // Calculate push direction from move direction, 
	    // we only push objects to the sides never up and down
	    Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
	    // If you know how fast your character is trying to move,
	    // then you can also multiply the push velocity by that.
	    
	    // Apply the push
	    body.velocity = pushDir * pushPower;
	}
	
	void SetSpeed ( float speed  ){
		if (speed > walkSpeed){
			objectWithAnims.GetComponent<Animation>().CrossFade("run");
		}else{
			if(speed > 0){
				objectWithAnims.GetComponent<Animation>().CrossFade("walk");
			}else{
				objectWithAnims.GetComponent<Animation>().CrossFade("idle");
			}
		}
	}
}