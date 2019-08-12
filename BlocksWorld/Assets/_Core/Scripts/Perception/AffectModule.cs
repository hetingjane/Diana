/*
This module captures joint position from Kinect Skeleton frame and posts to the blackboard.

Writes:		user:joint:<jointType> = Vector3
			user:joint:tracked:<jointType> = Boolean
            user:joint:inferred:<jointType> = Boolean
*/

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Windows.Kinect;
using Perception.Kinect;
using Perception.Frames;
using Affdex;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.Runtime.InteropServices;

public class AffectModule : ModuleBase
{
    private Perception.Kinect.KinectSensor sensor;
    private MultiSourceFrameReader multiSourceFrameReader;
    private static double engageMin = 0;
    private static double engageMax = 5;


    private int i = 0;
    Stopwatch sw = new Stopwatch();
    private double totalTime = 0.0;

    Rectangle rectangle;
    Detector detector;
    Affdex.Frame frame;
    protected override void Start()
    {
        base.Start();
        sw.Start();
        sensor = new Perception.Kinect.KinectSensor(Perception.Kinect.KinectSensor.FrameType.Color);

        //This code sets the function to be executed on the arribal of a frame
        sensor.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
        detector = GetComponent<Detector>();
        rectangle = new Rectangle(640, 300, 640, 480);

        //SetValue("user:joy", 0, "initialization");
        //SetValue("user:sadness", 0, "initialization");
    }


    private void OnMultiSourceFrameArrived(object sender, Perception.Kinect.MultiSourceFrameArrivedEventArgs e)
    {

        if (e.ColorFrame != null)
        {
            //UnityEngine.Debug.LogError("frame received!");
            sw.Stop();
            totalTime += sw.Elapsed.TotalSeconds;
            DataStore.SetValue("Time:per:frame", new DataStore.StringValue((++i / totalTime).ToString()), this, sw.Elapsed.ToString());
            sw.Restart();
            //var closestBodyFrame = new ClosestBodyFrame(e.BodyFrame, engageMin, engageMax);
            //var headColorFrame = new HeadColorFrame(e.ColorFrame, closestBodyFrame);
            //var colorFrame = new Perception.Frames.ColorFrame(e.ColorFrame);
            
            Bitmap b = cropAtRect(ColorImageFrameToBitmap(e.ColorFrame), rectangle);

            byte[] data = BitmaptoArray(b);
            var colorArray = new Color32[data.Length / 4];
            for (var i = 0; i < data.Length; i += 4)
            {
                var color = new Color32(data[i + 0], data[i + 1], data[i + 2], data[i + 3]);
                colorArray[i / 4] = color;
            }
            //Color32[] data = colorFrame.GetBytes();

           
            frame = new Affdex.Frame(colorArray, b.Width, b.Height, e.Timestamp);
            if (detector.IsRunning)
            {
                detector.ProcessFrame(frame);
            }
        }
        //else { UnityEngine.Debug.LogError("frame null!"); }
    }
    private Bitmap ColorImageFrameToBitmap(Windows.Kinect.ColorFrame colorFrame)
    {
        colorFrame.CreateFrameDescription(ColorImageFormat.Yuy2);

        byte[] pixelBuffer = new byte[
            colorFrame.FrameDescription.Width *
            colorFrame.FrameDescription.Height *
            colorFrame.FrameDescription.BytesPerPixel];

        colorFrame.CopyConvertedFrameDataToArray(
          pixelBuffer, ColorImageFormat.Yuy2);
        Bitmap bitmapFrame = ArrayToBitmap(pixelBuffer, colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height, PixelFormat.Format32bppRgb);
        return bitmapFrame;
    }
    Bitmap ArrayToBitmap(byte[] array, int width, int height, PixelFormat pixelFormat)
    {
        Bitmap bitmapFrame = new Bitmap(width, height, pixelFormat);

        BitmapData bitmapData = bitmapFrame.LockBits(new Rectangle(0, 0,
        width, height), ImageLockMode.WriteOnly, bitmapFrame.PixelFormat);

        IntPtr intPointer = bitmapData.Scan0;
        Marshal.Copy(array, 0, intPointer, array.Length);

        bitmapFrame.UnlockBits(bitmapData);
        return bitmapFrame;
    }
    private static Bitmap cropAtRect(Bitmap b, Rectangle r)
    {
        Bitmap nb = new Bitmap(r.Width, r.Height);
        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(nb);
        g.DrawImage(b, -r.X, -r.Y);
        return nb;
    }
    private byte[] BitmaptoArray(Bitmap bmp)
    {
        // Lock the bitmap's bits.  
        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap.
        int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
        byte[] rgbValues = new byte[bytes];

        // Copy the RGB values into the array.
        Marshal.Copy(ptr, rgbValues, 0, bytes);
        return rgbValues;
    }


}
