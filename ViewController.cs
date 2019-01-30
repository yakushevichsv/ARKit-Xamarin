using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ARKit;
using ARNativePortal.Models;
using CoreFoundation;
using Foundation;
using SceneKit;
using UIKit;

namespace ARNativePortal
{
    public partial class ViewController : UIViewController
    {
        private ARSession arSession => arSceneView.Session;
        private readonly ARSessionDelegate arSessionHandler = new ARSessionHandler();
        private readonly ARSCNViewDelegate arSceneHandler = new ARSceneViewHandler();
        private readonly DispatchQueue arQueue = new DispatchQueue("AR.Session.Queue", false);
        private readonly Dictionary<ARAnchor, PlaneState> planeState = new Dictionary<ARAnchor, PlaneState>(); 
        private long audioValue = 0;
        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            var configurationTuple = CreateConfigurationTuple();
            var configuration = configurationTuple.configuration;
            if (configuration == null)
            {
                //Print message...
                messageLabel.Text = "AR World Configuration is not supported by this device. Should be iPhone 6S as minimum.";
                return;
            }

            arSession.Delegate = arSessionHandler;
            arSession.DelegateQueue = arQueue;
            arSceneView.Delegate = arSceneHandler;

            DefineObservers();

            var options = configurationTuple.options;
            arSession.Run(configuration, options);

            //PauseSession();
#if DEBUG
            arSceneView.DebugOptions = SCNDebugOptions.ShowPhysicsShapes;
            arSceneView.ShowsStatistics = true;
#endif

        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            var count = evt.AllTouches.Count;
            if (count == 0)
                return;

            var enumerator = touches.GetEnumerator();
            var current = enumerator.Current;
            while (current == null && enumerator.MoveNext()){
                current = enumerator.Current;
            }
            if (current is UITouch touch)
            {
                var position = touch.LocationInView(arSceneView);
                var items = arSceneView.HitTest(position, ARHitTestResultType.ExistingPlaneUsingExtent | ARHitTestResultType.EstimatedHorizontalPlane) ?? new ARHitTestResult[0];
                if (items.Length == 0)
                    return;

                var index = 0;

                while (index < items.Length && items[index].Anchor == null)
                {
                    index++;
                }

                if (index == items.Length)
                    return;

                var hitTest = items[index];
                var anchor = hitTest.Anchor;

                if (!planeState.TryGetValue(anchor, out PlaneState value))
                    value = PlaneState.None;

                var values = (PlaneState[])Enum.GetValues(value.GetType());
                index = Array.IndexOf(values, value);
                if (index >= 0)
                {
                    index += 1;
                    index = index % values.Length;
                    value = (PlaneState)index;
                }
                planeState[anchor] = value;

                var node = arSceneView.GetNode(anchor);
                if (node == null)
                {
                    Debug.Assert(false);
                    return;
                }

                switch (value) {
                    case PlaneState.Fire:
                        ((ARSceneViewHandler)arSceneHandler).RemoveEqualizer(node);
                        ((ARSceneViewHandler)arSceneHandler).ActivateFire(node);
                        break;
                    case PlaneState.Audio:
                        ((ARSceneViewHandler)arSceneHandler).RemoveParticles(node);
                        ((ARSceneViewHandler)arSceneHandler).ActivateEqualizer(node, audioValue);
                        break;
                    case PlaneState.None:
                        ((ARSceneViewHandler)arSceneHandler).RemoveParticles(node);
                        ((ARSceneViewHandler)arSceneHandler).RemoveEqualizer(node);
                        break;
                }
            }
        }

        private void DidChangeTrackingState(ARTrackingState state)
        {
            statusLabel.Text = "Tracking Status: " + state.ToString();
            if (state == ARTrackingState.Limited) {
                messageLabel.Text = "Move around slowly to normalize tracking state";
            }
            else if (state == ARTrackingState.Normal) {
                messageLabel.Text = string.Empty;
            }
            Debug.WriteLine("Status Frame {0} View Frame {1}", statusLabel.Frame, View.Frame);
        }

        private void DidChangeSessionInterruptionState(bool interrupted) 
        {
            if (!interrupted) {
                var options = ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors;
                RestartSession(options); // Reset session...
            }
        }

        private void DefineObservers()
        {
            DefineSessionHandlerObservers();
            DefineSceneViewHandlerObservers();
        }

        private void DefineSessionHandlerObservers() {
            var handler = arSessionHandler as ARSessionHandler;
            handler.DidChangeTrackingState += DidChangeTrackingState;
            handler.DidChangeSessionInterruptionState += DidChangeSessionInterruptionState;
        }

        private void DefineSceneViewHandlerObservers()
        {
            var handler = arSceneHandler as ARSceneViewHandler;
            handler.DidGetAudioSample += DidGetAudioSampleHandler;
            handler.DidAddCustomNode += DidAddCustomNodeHandler;
            handler.DidUpdateCustomNode += DidUpdateCustomNodeHandler;
            handler.DidRemoveCustomNode += DidRemoveCustomNodeHandler;
        }

        private void DidAddCustomNodeHandler(SCNNode node, ARAnchor anchor)
        {
            var value = PlaneState.None;
#if DEBUG
            planeState.TryGetValue(anchor, out value);
            Debug.Assert(value == PlaneState.None);
#endif
            planeState[anchor] = value;
        }

        private void DidUpdateCustomNodeHandler(SCNNode node, ARAnchor anchor)
        {}

        private void DidRemoveCustomNodeHandler(SCNNode node, ARAnchor anchor)
        {
            var value = PlaneState.None;
            if (planeState.TryGetValue(anchor, out value))
                planeState.Remove(anchor);
        }

        private void DidGetAudioSampleHandler(long value)
        {
            audioValue = value;
            var currentFrame = arSession.CurrentFrame;
            if (currentFrame == null)
                return;

            foreach (var anchor in currentFrame.Anchors)
            {
                var node = arSceneView.GetNode(anchor);
                if (node == null)
                {
                    Debug.Assert(false);
                    return;
                }

                if (planeState.TryGetValue(anchor, out PlaneState valueState) && valueState == PlaneState.Audio) {
                    var handler = (ARSceneViewHandler)arSceneHandler;
                    handler.RemoveParticles(node);
                    handler.ActivateEqualizer(node, audioValue);
                }
            }
        }

        private void Deinit() {
            DeinitSessionHandler();
            DeinitSceneViewHanlder();
        }

        private void DeinitSceneViewHanlder()
        {
            var handler = arSceneHandler as ARSceneViewHandler;
            handler.DidGetAudioSample -= DidGetAudioSampleHandler;
            handler.DidAddCustomNode -= DidAddCustomNodeHandler;
            handler.DidUpdateCustomNode -= DidUpdateCustomNodeHandler;
            handler.DidRemoveCustomNode -= DidRemoveCustomNodeHandler;
        }

        private void DeinitSessionHandler()
        {
            var handler = arSessionHandler as ARSessionHandler;
            handler.DidChangeTrackingState -= DidChangeTrackingState;
            handler.DidChangeSessionInterruptionState -= DidChangeSessionInterruptionState;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Deinit();
        }


        private void PauseSession()
        {
            arSession?.Pause();
        }

        private void RestartSession()
        {
            RestartSession(0);
        }

        private void RestartSession(ARSessionRunOptions options)
        {
            var configuration = arSession?.Configuration;
            if (configuration != null)
            {
                arSession.Run(configuration, options); // Try to restore everything.... What to do if you moved away...
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            RestartSession();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            PauseSession();
        }



        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        #region Scene View

        static (ARConfiguration configuration, ARSessionRunOptions options) CreateConfigurationTuple() {
            var options = ARSessionRunOptions.ResetTracking | ARSessionRunOptions.RemoveExistingAnchors;
            return CreateConfigurationTuple(options);
        }

        static (ARConfiguration configuration, ARSessionRunOptions options) CreateConfigurationTuple(ARSessionRunOptions options)
        {
#pragma warning disable RECS0030 // Suggests using the class declaring a static function when calling it
            if (!ARWorldTrackingConfiguration.IsSupported)
                return (null, options);
#pragma warning restore RECS0030 // Suggests using the class declaring a static function when calling it
            var configuration = new ARWorldTrackingConfiguration
            {
                WorldAlignment = ARWorldAlignment.Gravity,
                LightEstimationEnabled = true,
                ProvidesAudioData = true,
                PlaneDetection = ARPlaneDetection.Horizontal //| ARPlaneDetection.Vertical
            };
            return (configuration, options);
        }
            
        #endregion
    }
}
