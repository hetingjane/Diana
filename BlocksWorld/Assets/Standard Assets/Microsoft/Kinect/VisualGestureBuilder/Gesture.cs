using RootSystem = System;
using System.Linq;
using System.Collections.Generic;
namespace Windows.Kinect.VisualGestureBuilder
{
    //
    // Windows.Kinect.VisualGestureBuilder.Gesture
    //
    public sealed partial class Gesture : Helper.INativeWrapper

    {
        internal RootSystem.IntPtr _pNative;
        RootSystem.IntPtr Helper.INativeWrapper.nativePtr { get { return _pNative; } }

        // Constructors and Finalizers
        internal Gesture(RootSystem.IntPtr pNative)
        {
            _pNative = pNative;
            Microsoft_Kinect_VisualGestureBuilder_Gesture_AddRefObject(ref _pNative);
        }

        ~Gesture()
        {
            Dispose(false);
        }

        [RootSystem.Runtime.InteropServices.DllImport("KinectVisualGestureBuilderUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void Microsoft_Kinect_VisualGestureBuilder_Gesture_ReleaseObject(ref RootSystem.IntPtr pNative);
        [RootSystem.Runtime.InteropServices.DllImport("KinectVisualGestureBuilderUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern void Microsoft_Kinect_VisualGestureBuilder_Gesture_AddRefObject(ref RootSystem.IntPtr pNative);
        private void Dispose(bool disposing)
        {
            if (_pNative == RootSystem.IntPtr.Zero)
            {
                return;
            }

            __EventCleanup();

            Helper.NativeObjectCache.RemoveObject<Gesture>(_pNative);
                Microsoft_Kinect_VisualGestureBuilder_Gesture_ReleaseObject(ref _pNative);

            _pNative = RootSystem.IntPtr.Zero;
        }


        // Public Properties
        [RootSystem.Runtime.InteropServices.DllImport("KinectVisualGestureBuilderUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern Windows.Kinect.VisualGestureBuilder.GestureType Microsoft_Kinect_VisualGestureBuilder_Gesture_get_GestureType(RootSystem.IntPtr pNative);
        public  Windows.Kinect.VisualGestureBuilder.GestureType GestureType
        {
            get
            {
                if (_pNative == RootSystem.IntPtr.Zero)
                {
                    throw new RootSystem.ObjectDisposedException("Gesture");
                }

                return Microsoft_Kinect_VisualGestureBuilder_Gesture_get_GestureType(_pNative);
            }
        }

        [RootSystem.Runtime.InteropServices.DllImport("KinectVisualGestureBuilderUnityAddin", CallingConvention=RootSystem.Runtime.InteropServices.CallingConvention.Cdecl, SetLastError=true)]
        private static extern RootSystem.IntPtr Microsoft_Kinect_VisualGestureBuilder_Gesture_get_Name(RootSystem.IntPtr pNative);
        public  string Name
        {
            get
            {
                if (_pNative == RootSystem.IntPtr.Zero)
                {
                    throw new RootSystem.ObjectDisposedException("Gesture");
                }

                RootSystem.IntPtr objectPointer = Microsoft_Kinect_VisualGestureBuilder_Gesture_get_Name(_pNative);
                Helper.ExceptionHelper.CheckLastError();

                var managedString = RootSystem.Runtime.InteropServices.Marshal.PtrToStringUni(objectPointer);
                RootSystem.Runtime.InteropServices.Marshal.FreeCoTaskMem(objectPointer);
                return managedString;
            }
        }

        private void __EventCleanup()
        {
        }
    }

}
