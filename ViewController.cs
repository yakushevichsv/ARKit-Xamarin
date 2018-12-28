using System;
using System.Diagnostics;
using System.Globalization;
using ARKit;
using ARNativePortal.Models;
using CoreFoundation;
using UIKit;

namespace ARNativePortal
{
    public partial class ViewController : UIViewController
    {
        private ARSession arSession => arSceneView.Session;
        private readonly ARSessionDelegate arSessionHandler = new ARSessionHandler();
        private readonly ARSCNViewDelegate arSceneHandler = new ARSceneViewHandler();
        private readonly DispatchQueue arQueue = new DispatchQueue("AR.Session.Queue", false);

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
            arSceneView.DebugOptions = SceneKit.SCNDebugOptions.ShowBoundingBoxes;
            arSceneView.ShowsStatistics = true;
#endif

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

        private void DefineObservers() {
            var handler = arSessionHandler as ARSessionHandler;
            handler.DidChangeTrackingState += DidChangeTrackingState;
            handler.DidChangeSessionInterruptionState += DidChangeSessionInterruptionState;
        }

        private void Deinit() {
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
                PlaneDetection = ARPlaneDetection.Horizontal | ARPlaneDetection.Vertical
            };
            return (configuration, options);
        }
            
        #endregion
    }
}
