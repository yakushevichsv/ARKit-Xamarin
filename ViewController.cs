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
        private ARSession aRSession => arSceneView.Session;
        private ARSessionHandler arSessionHandler = new ARSessionHandler();
        private DispatchQueue arQueue = new DispatchQueue("AR.Session.Queue", false);

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

            aRSession.Delegate = arSessionHandler;
            aRSession.DelegateQueue = arQueue;
            arSessionHandler.DidChangeTrackingState += DidChangeTrackingState;

            var options = configurationTuple.options;
            aRSession.Run(configuration, options);

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

        private void Deinit() => arSessionHandler.DidChangeTrackingState -= DidChangeTrackingState;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Deinit();
        }


        private void PauseSession()
        {
            aRSession?.Pause();
        }

        private void RestartSession()
        {
            var configuration = aRSession?.Configuration;
            if (configuration != null)
            {
                aRSession.Run(configuration, 0); // Try to restore everything.... What to do if you moved away...
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
