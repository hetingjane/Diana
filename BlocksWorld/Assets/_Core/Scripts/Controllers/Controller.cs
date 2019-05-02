using System;
using UnityEngine;

public abstract class Controller : MonoBehaviour {

    #region EventArgs
    public class PointingEventArgs : EventArgs
    {
        // The pointing position
        public Vector2 Position
        {
            private set;
            get;
        }

        public PointingEventArgs(Vector2 position)
        {
            Position = position;
        }

        public float X { get { return Position.x; } }
        public float Y { get { return Position.y; } }

    }
    public class PointedEventArgs : EventArgs
    {
        // The pointing position
        public Vector2 Position
        {
            private set;
            get;
        }

        public PointedEventArgs(Vector2 position)
        {
            Position = position;
        }

        public float X { get { return Position.x; } }
        public float Y { get { return Position.y; } }

    }
    #endregion

    #region Events
    public event EventHandler<PointingEventArgs> Pointing;
    public event EventHandler<PointedEventArgs> Pointed;
    public event EventHandler StoppedPointing; // Use EventArgs.Empty in the caller
    public event EventHandler Grabbed;
    public event EventHandler AskedToIgnore;
    public event EventHandler Waved;
    #endregion

    #region Enable event invocations for derived classes
    //Override all these in derived classes to provide their own behavior

    protected virtual void OnStoppedPointing(EventArgs e)
    {
        EventHandler handler = StoppedPointing;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnGrabbed(EventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        EventHandler handler = Grabbed;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnAskedToIgnore(EventArgs e)
    {
        EventHandler handler = AskedToIgnore;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnPointing(PointingEventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        EventHandler<PointingEventArgs> handler = Pointing;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnPointed(PointedEventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        EventHandler<PointedEventArgs> handler = Pointed;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    protected virtual void OnWaved(EventArgs e)
    {
        // Make a temporary copy of the event to avoid possibility of
        // a race condition if the last subscriber unsubscribes
        // immediately after the null check and before the event is raised.
        EventHandler handler = Waved;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    
    #endregion
}