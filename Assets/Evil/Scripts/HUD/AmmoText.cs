//AmmoText.cs by Azuline StudiosÂ© All Rights Reserved
using UnityEngine;
using System.Collections;

public class AmmoText : MonoBehaviour {
	//draw ammo amount on screen
	public int ammoGui;//bullets remaining in clip
	public int ammoGui2;//total ammo in inventory
	private int oldAmmo = -512;
	private int oldAmmo2 = -512;
	public Color textColor;
	public float horizontalOffset = 0.95f;
	public float verticalOffset = 0.075f;
	[HideInInspector]
	public float horizontalOffsetAmt = 0.78f;
	[HideInInspector]
	public float verticalOffsetAmt = 0.1f;
	public float fontScale = 0.032f;
	
	void OnEnable(){
		horizontalOffsetAmt = horizontalOffset;
		verticalOffsetAmt = verticalOffset;	
		GetComponent<GUIText>().fontSize = Mathf.RoundToInt(Screen.height * fontScale);
		oldAmmo = -512;
		oldAmmo2 = -512;
	}
	
	void Update(){
		//only update GUIText if value to be displayed has changed
	    if((ammoGui != oldAmmo) || (ammoGui2 != oldAmmo2)) {
			GetComponent<GUIText>().pixelOffset = new Vector2 (Screen.width * horizontalOffsetAmt, Screen.height * verticalOffsetAmt);
			
			GetComponent<GUIText>().text = "Ammo : "+ ammoGui.ToString()+" / "+ ammoGui2.ToString();
			
			GetComponent<GUIText>().material.color = textColor;
		    oldAmmo = ammoGui;
			oldAmmo2 = ammoGui2;
	    }
	
	}
}