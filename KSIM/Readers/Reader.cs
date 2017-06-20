﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.IO;

namespace KSIM.Readers
{
    public abstract class Reader
    {
        public abstract Frame read(MultiSourceFrame f);
    }

    public enum FrameType { Color=1, Skeleton, Audio, Depth, ClosestBody, LHDepth, RHDepth, HeadDepth };

    public abstract class Frame : IDisposable
    {
        private int width = 0, height = 0;

        public int Width
        {
            get { return width; }
            protected set { width = value; }
        }

        public int Height
        {
            get { return height; }
            protected set { height = value; }
        }

        private FrameType ft;

        public FrameType Type
        {
            get { return ft; }
            protected set { ft = value; }
        }

        // No sync by default
        private long timestamp = -1;

        // To allow syncing of different types of frames over network by their timestamp
        public long Timestamp
        {
            get { return timestamp; }
            protected set { timestamp = value; }
        }

        // Note that serialization will follow little-endian format, even for network transfers
        // so write clients accordingly
        public abstract void Serialize(Stream stream);
        
        // Disposable pattern starts
        protected bool disposed = false; // To detect redundant calls

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // Disposable pattern ends

    }
}
