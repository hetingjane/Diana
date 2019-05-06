using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFader : MonoBehaviour {
	#region Public Properties

	[System.Serializable]
	public class Events {
		public UnityEvent fadingOut;
		public UnityEvent fadedOut;
		public UnityEvent fadingIn;
		public UnityEvent fadedIn;
	}

	[Tooltip("If true, set interactable and blocksRaycast according to current alpha")]
	public bool updateInteractable = true;
	
	public bool fadingIn { get { return mode == Mode.FadingIn; } }
	public bool fadingOut { get { return mode == Mode.FadingOut; } }
	
	public Events events;

	#endregion
	//--------------------------------------------------------------------------------
	#region Private Properties
	enum Mode {
		FadingIn,
		FadingOut,
		Done
	}
	
	CanvasGroup cg;
	float duration;
	Mode mode = Mode.Done;

	#endregion
	//--------------------------------------------------------------------------------
	#region MonoBehaviour Events
	void Start() {
		cg = GetComponent<CanvasGroup>();
	}

	void Update() {
		if (mode == Mode.Done) return;
		
		float alpha = cg.alpha;
		float maxDelta = Time.deltaTime / duration;
		if (mode == Mode.FadingOut) {
			alpha = Mathf.MoveTowards(alpha, 0, maxDelta);
			if (alpha == 0) {
				if (events.fadedOut != null) events.fadedOut.Invoke();
				mode = Mode.Done;
			}
		} else if (mode == Mode.FadingIn) {
			alpha = Mathf.MoveTowards(alpha, 1, maxDelta);
			if (alpha == 1) {
				if (events.fadedIn != null) events.fadedIn.Invoke();
				mode = Mode.Done;
			}
		}
		UpdateAlphaOnly(alpha);
	}

	#endregion
	//--------------------------------------------------------------------------------
	#region Public Methods
	public void FadeOut(float fadeTime) {
		if (GetAlpha() < 0.01f) return;	// already out!
		if (events.fadingOut != null) events.fadingOut.Invoke();
		duration = fadeTime;
		mode = Mode.FadingOut;
	}

	public void FadeIn(float fadeTime) {
		if (GetAlpha() > 0.99f) return;	// already in!
		if (events.fadingIn != null) events.fadingIn.Invoke();
		duration = fadeTime;
		mode = Mode.FadingIn;
	}

	public void ToggleFade(float fadeTime) {
		if (GetAlpha() > 0.5f) FadeOut(fadeTime);
		else FadeIn(fadeTime);
	}

	public void SetAlpha(float alpha) {
		UpdateAlphaOnly(alpha);
		mode = Mode.Done;
	}
	
	void UpdateAlphaOnly(float alpha) {
		if (cg == null) cg = GetComponent<CanvasGroup>();
		cg.alpha = alpha;
		if (updateInteractable) {
			cg.interactable = (alpha > 0.01f);
			cg.blocksRaycasts = cg.interactable;
		}		
	}
	
	public float GetAlpha() {
		if (cg == null) cg = GetComponent<CanvasGroup>();
		return cg.alpha;
	}
	#endregion
}
