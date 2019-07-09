using System;
using System.Collections.Generic;

namespace Perception.Kinect
{
    public abstract class KinectEventArgs : EventArgs
    {
        public long Timestamp
        {
            get;
        }

        public KinectEventArgs(long timestamp)
        {
            Timestamp = timestamp;
        }
    }

    public class MultiSourceFrameArrivedEventArgs : KinectEventArgs
    {
        public Windows.Kinect.ColorFrame ColorFrame
        {
            get;
        }

        public Windows.Kinect.DepthFrame DepthFrame
        {
            get;
        }

        public Windows.Kinect.BodyFrame BodyFrame
        {
            get;
        }

        public Windows.Kinect.BodyIndexFrame BodyIndexFrame
        {
            get;
        }

        public Windows.Kinect.InfraredFrame InfraredFrame
        {
            get;
        }

        public MultiSourceFrameArrivedEventArgs(long timestamp, Windows.Kinect.ColorFrame colorFrame,
            Windows.Kinect.DepthFrame depthFrame, Windows.Kinect.BodyFrame bodyFrame,
            Windows.Kinect.BodyIndexFrame bodyIndexFrame, Windows.Kinect.InfraredFrame infraredFrame) : base(timestamp)
        {
            ColorFrame = colorFrame;
            DepthFrame = depthFrame;
            BodyFrame = bodyFrame;
            BodyIndexFrame = bodyIndexFrame;
            InfraredFrame = infraredFrame;
        }
    }

    public class AudioBeamFrameArrivedEventArgs : KinectEventArgs
    {
        public IList<Windows.Kinect.AudioBeamFrame> AudioBeamFrameList
        {
            get;
        }

        public AudioBeamFrameArrivedEventArgs(long timestamp, IList<Windows.Kinect.AudioBeamFrame> audioBeamFrameList) : base(timestamp)
        {
            AudioBeamFrameList = audioBeamFrameList;
        }
    }

    public class FaceFrameArrivedEventArgs : KinectEventArgs
    {
        public Windows.Kinect.Face.FaceFrameResult FaceFrameResult
        {
            get;
        }

        public FaceFrameArrivedEventArgs(long timestamp, Windows.Kinect.Face.FaceFrameResult faceFrameResult) : base(timestamp)
        {
            FaceFrameResult = faceFrameResult;
        }
    }
}
