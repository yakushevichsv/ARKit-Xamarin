using System;
using System.Diagnostics;
using ARKit;
using ARNativePortal.Helpers;
using Foundation;

namespace ARNativePortal.Models
{
    public class ARSessionHandler: ARSessionDelegate
    {
        public delegate void DidChangeTrackingStateDelegate(ARTrackingState state);
        public event DidChangeTrackingStateDelegate DidChangeTrackingState;

        private DebugHelper debugHelper = DebugHelper.Instance;

        public override void CameraDidChangeTrackingState(ARSession session, ARCamera camera)
        {
            var functionName = debugHelper.FunctionName();
            Debug.WriteLine(functionName + "State " + camera.TrackingState);

            var handler = DidChangeTrackingState;
            var state = camera.TrackingState;
            if (handler != null)
            {
                BeginInvokeOnMainThread(() => handler?.Invoke(state));
            }
        }

        public override void DidFail(ARSession session, NSError error)
        {
            base.DidFail(session, error);
            var functionName = debugHelper.FunctionName();
            Debug.WriteLine(functionName + "Error " + error);
        }
    }
}

