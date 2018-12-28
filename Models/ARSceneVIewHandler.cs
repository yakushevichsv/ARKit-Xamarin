using System;
using System.Collections.Generic;
using System.Diagnostics;
using ARKit;
using ARNativePortal.Helpers;
using SceneKit;
using UUID = Foundation.NSUuid;

namespace ARNativePortal.Models
{
    public class ARSceneViewHandler: ARSCNViewDelegate
    {
        private bool TryToAdjustNode(SCNNode node, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                AdjustPlaneNode(node, planeAnchor);
                return true;
            }
            return false;
        }

        private static void AdjustPlaneNode(SCNNode node, ARPlaneAnchor planeAnchor)
        {
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {

                if (planeNode.Geometry is SCNPlane planeGeometry)
                {
                    planeGeometry.Width = planeAnchor.Extent.X;
                    planeGeometry.Height = planeAnchor.Extent.Z;
                }

                var angle = planeAnchor.Alignment == ARPlaneAnchorAlignment.Horizontal ? (float)(Math.PI * 0.5) : 0.0f;
                planeNode.Transform = planeAnchor.Transform.ToSCNMatrix4();
                planeNode.LocalRotate(SCNQuaternion.FromAxisAngle(SCNVector3.UnitX, angle));
            }
        }

        public override void DidAddNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                Debug.WriteLine("!!! GetNode with plane anchor: {0}", planeAnchor);

                var plane = new SCNPlane();
                var material = SCNMaterial.Create();
                material.DoubleSided = true;
                material.Diffuse.ContentColor = UIKit.UIColor.Red.ColorWithAlpha(0.4f);
                plane.FirstMaterial = material;

                var retNode = new SCNNode
                {
                    Name = Constants.PlaneNodeName,
                    Geometry = plane
                };

                Debug.WriteLine("!!!Return Node {0} Transform {1}", retNode.WorldPosition, retNode.WorldTransform);
                BeginInvokeOnMainThread(() =>
                {
                    node.AddChildNode(retNode);
                    TryToAdjustNode(node, anchor);
                });
            }
        }

        public override void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            BeginInvokeOnMainThread(() =>
            {
                TryToAdjustNode(node, anchor);
            });
        }

        public override void DidRemoveNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            BeginInvokeOnMainThread(node.RemoveFromParentNode);
        }
    }
}
