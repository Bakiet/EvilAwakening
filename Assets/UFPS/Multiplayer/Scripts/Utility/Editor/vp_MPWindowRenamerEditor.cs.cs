/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPWindowRenamerEditor.cs
//	© VisionPunk. All Rights Reserved.
//	https://twitter.com/VisionPunk
//	http://www.visionpunk.com
//
//	description:	custom inspector for the vp_MPWindowRenamer class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEditor;

[CustomEditor(typeof(vp_MPWindowRenamer))]

public class vp_MPWindowRenamerEditor : Editor
{

	/// <summary>
	/// 
	/// </summary>
	protected  virtual void OnEnable()
	{

		// set window name to the editor-defined product name (ProjectSettings -> Player -> Product Name')
		((vp_MPWindowRenamer)target).ProductName = PlayerSettings.productName;

	}

}

