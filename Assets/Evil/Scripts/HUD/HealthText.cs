//HealthText.cs by Azuline StudiosÂ© All Rights Reserved
using UnityEngine;
using System.Collections;

public class HealthText : MonoBehaviour {
	//draw health amount on screen
	public float healthGui;
	private float oldHealthGui = -512;
	public Color textColor;
	public float horizontalOffset = 0.0425f;
	public float verticalOffset = 0.075f;
	public float fontScale = 0.032f;
	public bool showNegativeHP = true;
	
	void Start(){
		GetComponent<GUIText>().material.color = textColor;
		GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
		oldHealthGui = -512;
	}
	
	void Update (){
		//only update GUIText if value to be displayed has changed
	    if(healthGui != oldHealthGui){
			if(healthGui < 0.0f && !showNegativeHP){
				GetComponent<GUIText>().text = "Health : 0";
			}else{
				GetComponent<GUIText>().text = "Health : "+ healthGui.ToString();
			}
			GetComponent<GUIText>().pixelOffset = new Vector2 (Screen.width * horizontalOffset, Screen.height * verticalOffset);
			oldHealthGui = healthGui;
		}
	}
	
}