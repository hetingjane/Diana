using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Windows.Kinect;

namespace Perception.Frames
{
    public sealed class HeadColorFrame : SegmentedColorFrame
    {
        public HeadColorFrame headColorFrame;

        public HeadColorFrame(Windows.Kinect.ColorFrame cf, ClosestBodyFrame cbf) : base(cf, cbf)
        {
            Type = FrameType.HeadColor;
        }

        protected override void SetCenter()
        {
            if (UnderlyingClosestBodyFrame.Engaged)
            {
                var pos = UnderlyingClosestBodyFrame.Joints[JointType.Head].Position;
                ColorSpacePoint p = UnderlyingColorFrame.ColorFrameSource.KinectSensor.CoordinateMapper.MapCameraPointToColorSpace(pos);
                posX = p.X;
                posY = p.Y;
            }
        }
        //public Color32[] GetBytes()
        //{
        //    byte[] data = headColorFrame.colorData;
            
        //    var colorArray = new Color32[data.Length / 4];
        //    for (var i = 0; i < data.Length; i += 4)
        //    {
        //        var color = new Color32(data[i + 0], data[i + 1], data[i + 2], data[i + 3]);
        //        colorArray[i / 4] = color;
        //    }
        //    return colorArray;
        //}
    }
}
