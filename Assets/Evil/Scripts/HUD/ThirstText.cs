//ThirstText.cs by Azuline StudiosÂ© All Rights Reserved
using UnityEngine;
using System.Collections;

public class ThirstText : MonoBehaviour {
	//draw Thirst amount on screen
	[HideInInspector]
	public float thirstGui;
	private float oldThirstGui = -512;
	public Color textColor; 
	public float horizontalOffset = 0.0425f;
	public float verticalOffset = 0.075f;
	public float fontScale = 0.032f;
	
	void Start(){
		GetComponent<GUIText>().material.color = textColor;
		GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
		oldThirstGui = -512;
	}
	
	void Update (){
		//only update GUIText if value to be displayed has changed
	    if(thirstGui != oldThirstGui){
			GetComponent<GUIText>().text = "Thirst : "+ thirstGui.ToString();
			GetComponent<GUIText>().pixelOffset = new Vector2 (Screen.width * horizontalOffset, Screen.height * verticalOffset);
			oldThirstGui = thirstGui;
		}
	}
	
}