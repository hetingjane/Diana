using RootSystem = System;
using System.Linq;
using System.Collections.Generic;
namespace Windows.Kinect.Face
{
    //
    // Windows.Kinect.Face.KinectFaceUnityAddinUtils
    //
    public sealed partial class KinectFaceUnityAddinUtils
    {
        [RootSystem.Runtime.InteropServices.DllImport("KinectFaceUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void KinectFaceUnityAddin_FreeMemory(RootSystem.IntPtr pToDealloc);
        public static void FreeMemory(RootSystem.IntPtr pToDealloc)
        {
            KinectFaceUnityAddin_FreeMemory(pToDealloc);
        }
    }

}
