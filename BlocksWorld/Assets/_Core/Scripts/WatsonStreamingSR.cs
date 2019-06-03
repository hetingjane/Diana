/*	This module does streaming speech recognition via the Watson SDK.
(Requires internet access, and an IBM Watson account.)

Reads:		(nothing)
Writes:		user:isSpeaking (BoolValue)
			user:speech (StringValue)
*/
using UnityEngine;
using System.Collections;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class WatsonStreamingSR : ModuleBase
{
	[Header("Service Credentials")]
	public string _username = null;
	public string _password = null;
	public string _url = null;
	
	[Header("Control Options")]
	public bool pushToTalk = false;
	public KeyCode talkKey = KeyCode.RightShift;
	
	[Header("SR Options")]
	public string[] keywords;
	
	[Header("Feedback")]
	public Image levelMeter;
	public Color aboveThresholdMeterColor = new Color(0, 0.7f, 0);
	public Color belowThresholdMeterColor = new Color(0, 0, 0.4f);
	
	[Header("Events")]
	public UnityEvent onListening;
	public UnityEvent onNotListening;
	public StringEvent onInterim;
	public StringEvent onFinal;
	public UnityEvent onStopped;
	
	public List<string> alternatives;	// alternatives to last parse
	
    private int _recordingRoutine = 0;
    private string _microphoneID = null;
    private AudioClip _recording = null;
	private int _recordingBufferSize = 1;
    private int _recordingHZ = 22050;
	
    private SpeechToText _speechToText;

	void Start() {
        LogSystem.InstallDefaultReactors();

        //  Create credential and instantiate service
	    Credentials credentials = new Credentials(_username.Trim(), _password.Trim(), _url.Trim());

	    _speechToText = new SpeechToText(credentials);
	    alternatives = new List<string>();

	    if (!pushToTalk) {
		    Active = true;
	    	StartRecording();
	    } else {
	    	onNotListening.Invoke();
	    }
    }
    
	protected void Update() {
		if (pushToTalk) {
			if (Input.GetKeyDown(talkKey) && !Active) {
				Active = true;
				StartRecording();
			}
			if (Input.GetKeyUp(talkKey) && Active) {
				Active = false;
			}
		}
	}

    public bool Active
    {
        get { return _speechToText.IsListening; }
        set
        {
            if (value && !_speechToText.IsListening)
            {
                _speechToText.DetectSilence = true;
                _speechToText.EnableWordConfidence = true;
                _speechToText.EnableTimestamps = true;
                _speechToText.SilenceThreshold = 0.1f;
	            _speechToText.MaxAlternatives = 5;
                _speechToText.EnableInterimResults = true;
                _speechToText.OnError = OnError;
                _speechToText.InactivityTimeout = -1;
                _speechToText.ProfanityFilter = false;
                _speechToText.SmartFormatting = true;
                _speechToText.SpeakerLabels = false;
	            _speechToText.WordAlternativesThreshold = null;
	            if (keywords != null) _speechToText.Keywords = keywords;
                _speechToText.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && _speechToText.IsListening)
            {
	            _speechToText.StopListening();
	            onNotListening.Invoke();
            }
        }
    }

	public void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
	        _recordingRoutine = Runnable.Run(RecordingHandler());
	        onListening.Invoke();
        }
    }

	public void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
	        _recordingRoutine = 0;
	        onStopped.Invoke();
        }
    }

    private void OnError(string error)
    {
        Active = false;

        Log.Debug("ExampleStreaming.OnError()", "Error! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        Log.Debug("ExampleStreaming.RecordingHandler()", "devices: {0}", Microphone.devices);
        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let _recordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("ExampleStreaming.RecordingHandler()", "Microphone disconnected.");

                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
	            record.Clip.SetData(samples, 0);
	            
	            if (levelMeter != null) {
	            	levelMeter.fillAmount = record.MaxLevel;
		            if (record.MaxLevel > _speechToText.SilenceThreshold) {
		            	levelMeter.color = aboveThresholdMeterColor;
			            SetValue("user:isSpeaking", true, "input level above threshold");
		            } else {
		            	levelMeter.color = belowThresholdMeterColor;
			            SetValue("user:isSpeaking", false, "input level below threshold");
		            }
	            }
	            
                _speechToText.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }

        }

        yield break;
    }

    private void OnRecognize(SpeechRecognitionEvent result)
	{
		alternatives.Clear();
		string debugTxt = "";
        if (result != null && result.results.Length > 0)  {
            foreach (var res in result.results)  {
            	bool first = true;
                foreach (var alt in res.alternatives)  {
                    string text = string.Format("{0} ({1}, {2:0.00})\n", alt.transcript, res.final ? "Final" : "Interim", alt.confidence);
	                debugTxt += "alt " + (alternatives.Count + 1) + " of " + res.alternatives.Length + ": " + text + "\n";
	                
	                text = alt.transcript.Trim();
	                alternatives.Add(text);
                }

                //if (res.keywords_result != null && res.keywords_result.keyword != null)
                //{
                //    foreach (var keyword in res.keywords_result.keyword)
                //    {
                //        Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                //    }
                //}

                //if (res.word_alternatives != null)
                //{
                //    foreach (var wordAlternative in res.word_alternatives)
                //    {
                //        Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                //        foreach(var alternative in wordAlternative.alternatives)
                //            Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                //    }
                //}
            }
        }
		if (alternatives.Count > 0) {
			Log.Debug("WatsonStreamingSR.OnRecognize [final=" + result.results[0].final + "]", debugTxt);
			SetValue("user:isSpeaking", true, alternatives[0]);
			if (result.results[0].final) {
				SetValue("user:speech", alternatives[0], "final speech");
			} else {
				onInterim.Invoke(alternatives[0]);
			}
		}
    }

    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result)
    {
        if (result != null)
        {
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
            }
        }
    }
}
