// Unity derives Camera Input Component UI from this file
using UnityEngine;
using System.Collections;

namespace Affdex
{
    /// <summary>
    /// Provides WebCam access to the detector.  Sample rate set per second.  Use
    /// </summary>
    [RequireComponent(typeof(Detector))]
    public class CameraInput : MonoBehaviour, IDetectorInput
    {
        /// <summary>
        /// Number of frames per second to sample.  Use 0 and call ProcessFrame() manually to run manually.
        /// Enable/Disable to start/stop the sampling
        /// </summary>
        public float sampleRate = 20;

        /// <summary>
        /// Desired width for capture
        /// </summary>
        public int targetWidth = 1024;

        /// <summary>
        /// Desired height for capture
        /// </summary>
        public int targetHeight = 768;



        /// <summary>
        /// Web Cam texture
        /// </summary>
        private WebCamTexture cameraTexture;


        /// <summary>
        /// The detector that is on this game object
        /// </summary>
        public Detector detector
        {
            get; private set;
        }

        /// <summary>
        /// The texture that is being modified for processing
        /// </summary>
        public Texture Texture
        {
            get
            {
                return cameraTexture;

            }
        }

        void Start()
        {

            if (!AffdexUnityUtils.ValidPlatform())
                return;
            detector = GetComponent<Detector>();

        }

        /// <summary>
        /// Set the target device (by name)
        /// </summary>
        /// <param cameraName="choice">The name of the webcam usr select from pop-up panel.</param>

        public void OnCameraChange(string cameraName)
        {
            if (cameraName == null || cameraName == string.Empty)
            {
                Debug.LogError("cameraName is null or empty!");
                return;
            }
            if(cameraTexture == null)
            {

                Debug.Log($"Setting camera for the first time: {cameraName}");
                cameraTexture = new WebCamTexture(cameraName, targetWidth, targetHeight, (int)sampleRate);
                cameraTexture.Play();
            }
            else
            {
                Debug.Log($"Changing camera from: {cameraTexture.deviceName} to {cameraName}");

                cameraTexture.Stop();
                cameraTexture = new WebCamTexture(cameraName, targetWidth, targetHeight, (int)sampleRate);
                cameraTexture.Play();
            }
        }

        void OnEnable()
        {
            if (!AffdexUnityUtils.ValidPlatform())
                return;

            //get the selected camera!

            if (sampleRate > 0)
                StartCoroutine(SampleRoutine());
        }

        /// <summary>
        /// Coroutine to sample frames from the camera
        /// </summary>
        /// <returns></returns>
        private IEnumerator SampleRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(1 / sampleRate);
                ProcessFrame();
            }
        }


        /// <summary>
        /// Sample an individual frame from the webcam and send to detector for processing.
        /// </summary>
        public void ProcessFrame()
        {
            if (cameraTexture != null)
            {
                if (detector.IsRunning)
                {

                    if (cameraTexture.isPlaying)
                    {
                        Frame.Orientation orientation = Frame.Orientation.Upright;

                        Frame frame = new Frame(cameraTexture.GetPixels32(), cameraTexture.width, cameraTexture.height, orientation, Time.realtimeSinceStartup);
                        detector.ProcessFrame(frame);
                    }
                }
            }
        }
        void OnDestroy()
        {
            if (cameraTexture != null)
            {
                cameraTexture.Stop();
            }
        }
    }
}
