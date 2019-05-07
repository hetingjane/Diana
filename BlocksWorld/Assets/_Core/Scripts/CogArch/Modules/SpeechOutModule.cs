/*
This module watches for the agent's intent to speak, and actually does
the speech output.

Eventually this will involve model animation etc., but for now it just
displays the text on screen, and also invokes Apple speech synthesis
if available.

Reads:		me:speech:intent
Writes:		me:speech:current
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.RTVoice;

public class SpeechOutModule : ModuleBase {

	public StringEvent onSpeaking;
	
	float speechStartTime;
	float speechDuration;
	string nowSpeaking;
	
	Crosstales.RTVoice.Model.Voice voice;
	Crosstales.RTVoice.Speaker speaker;
	AudioSource audio;
	
	protected override void Start() {
		base.Start();
		DataStore.Subscribe("me:speech:intent", NoteSpeechIntent);
		speaker = GameObject.FindObjectOfType<Crosstales.RTVoice.Speaker>();
		audio = GetComponentInChildren<AudioSource>();
	}
	
	void ConfigureVoice() {
		string voices = DataStore.GetStringValue("me:voice");
		if (voices == null) return;
		foreach (string v in voices.Split(new char[]{';'})) {
			Debug.Log("Trying voice: " + v);
			voice = Crosstales.RTVoice.Speaker.VoiceForName(v);
			if (voice != null) return;
		}
		Debug.LogError("Unable to get voice matching " + voices);
	}
	
	void NoteSpeechIntent(string key, DataStore.IValue value) {
		var speech = value.ToString();
		if (string.IsNullOrEmpty(speech)) StopSpeaking();
		else Speak(speech);
	}

	void Speak(string speech) {
		if (voice == null) ConfigureVoice();
		Speaker.Speak(speech, audio, voice);
		
		onSpeaking.Invoke(speech);
		speechStartTime = Time.time;
		speechDuration = 2 + speech.Length * 0.1f;
		SetValue("me:speech:current", speech, "started speaking");
		//if (speechSynth != null) speechSynth.Speak(speech);
		nowSpeaking = speech;
	}
	
	void StopSpeaking() {
		onSpeaking.Invoke(null);
		SetValue("me:speech:current", "", "stopped speaking");
		if (DataStore.GetValue("me:speech:intent").ToString() == nowSpeaking) {
			SetValue("me:speech:intent", "", "stopped speaking");
		}
		nowSpeaking = null;
	}

	protected void Update() {
		if (nowSpeaking != null && Time.time - speechStartTime > speechDuration) {
			StopSpeaking();
		}
	}
}
