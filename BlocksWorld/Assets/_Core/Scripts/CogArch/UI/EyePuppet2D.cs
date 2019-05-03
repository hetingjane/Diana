/*
This script controls a pair of 2D cartoon eyes.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class EyePuppet2D : MonoBehaviour {

	[Header("Controls")]
	[Range(0,1)]	public float lidsOpen = 0.7f;
	[Range(-1,1)]	public float lookLeftRight;
	[Range(-1,1)]	public float lookUpDown;
	[Range(0,1)]	public float convergence = 0.8f;

	[Header("References")]
	public RectTransform leftIris;
	public RectTransform rightIris;
	public Image leftUpperLid;
	public Image rightUpperLid;
	public Image leftLowerLid;
	public Image rightLowerLid;

	[Header("Resources")]
	public Sprite[] upperLidSprites;
	public Sprite[] lowerLidSprites;
	
	[Header("Configuration")]
	public float irisMaxY = 10;
	public float irisMinY = -14;
	public float irisMaxX = 20;
	public float irisMinX = -20;
	public float maxIrisConvergence = 10;
	
	void Update() {
		if (leftLowerLid == null || rightUpperLid == null) return;
		
		float lidUpFrac = 1f - Mathf.Clamp01(lidsOpen + lookUpDown * 0.25f);
		leftUpperLid.sprite = rightUpperLid.sprite
			= upperLidSprites[Mathf.RoundToInt(lidUpFrac * (upperLidSprites.Length-1))];
		float lidDnFrac = 1f - Mathf.Clamp01(lidsOpen - lookUpDown * 0.25f);
		leftLowerLid.sprite = rightLowerLid.sprite
			= lowerLidSprites[Mathf.RoundToInt(lidDnFrac * (lowerLidSprites.Length-1))];
			
		float y = Mathf.Lerp(irisMinY, irisMaxY, lookUpDown*0.5f + 0.5f);
		float x = Mathf.Lerp(irisMinX, irisMaxX, lookLeftRight*0.5f + 0.5f);
		float conv = maxIrisConvergence * convergence;
		leftIris.anchoredPosition = new Vector2(x - conv, y);
		rightIris.anchoredPosition = new Vector2(x + conv, y);
	}
}
