using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class LoadScene : MonoBehaviour {

	[Tooltip("Invoked when we are about to load a new scene; receives the name of the scene we're loading.")]
	public StringEvent onLoading;

	[Tooltip("Invoked periodically while level loads; receives progress in range 0-1.")]
	public FloatEvent progress;

	[Tooltip("Invoked when the loading of the level is complete.  Make sure it's on an object that sticks around!")]
	public UnityEvent complete;
	
	[Header("Fade Out (Before Loading Next Scene)")]
	public bool doFadeOut;
	public float fadeTime = 0.5f;
	public CanvasGroupFader fader;

	/// <summary>
	/// Start loading the specified scene (asynchronously).
	/// </summary>
	/// <param name="sceneName">Scene name.</param>
	public void Load(string sceneName) {
		onLoading.Invoke(sceneName);
		StartCoroutine(LoadAsync(sceneName));
	}

	/// <summary>
	/// Coroutine to load the scene asynchronously, invoking the
	/// appropriate callbacks.
	/// </summary>
	/// <param name="sceneName">Scene name.</param>
	IEnumerator LoadAsync(string sceneName) {
		if (doFadeOut) {
			fader.transform.parent.gameObject.SetActive(true);
			fader.FadeIn(fadeTime);
			yield return new WaitForSeconds(fadeTime);
		}
		
		//Debug.Log("Loading level: " + sceneName);
		AsyncOperation async = Application.LoadLevelAsync(sceneName);

		while (!async.isDone) {
			if (progress != null) progress.Invoke(async.progress);
			yield return null;
		}
		
		Debug.Log("Level load complete: " + sceneName);
		if (complete != null) complete.Invoke();
				
	}
}
