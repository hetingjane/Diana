using RootSystem = System;
using System.Linq;
using System.Collections.Generic;
namespace Windows.Kinect.Face
{
    //
    // Windows.Kinect.Face.FaceModelBuilderAttributes
    //
    [RootSystem.Flags]
    public enum FaceModelBuilderAttributes : uint
    {
        None                                     =0,
        SkinColor                                =1,
        HairColor                                =2,
    }

}
