//Test script for repeated item spawning.
using UnityEngine;
using System.Collections;

public class WeaponSpawn : MonoBehaviour {

	public GameObject gunPrefab;
	public float spawnTime = 30.0f;
	public GameObject gunInstance = null;
	private float timeLeft;

	void Start (){
		timeLeft = 0; // You don't need to have weapons from the beginning
	}

	void Update (){
		if(timeLeft > spawnTime || gunInstance){
			timeLeft = spawnTime;
		}else if(timeLeft <= 0){
			timeLeft = 0;
			Spawn();
		}

		if(!gunInstance){
			timeLeft-=Time.deltaTime; // Reduce the time counter
		}
	}

	void Spawn (){
		// Make an instance of the weapon
		gunInstance = Instantiate(gunPrefab,transform.position,transform.rotation) as GameObject; 
		timeLeft = spawnTime; // Reset the time needed to the normal amount of time
	}
}