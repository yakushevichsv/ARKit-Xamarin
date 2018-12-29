using System;
using System.Collections.Generic;
using System.Diagnostics;
using ARKit;
using ARNativePortal.Helpers;
using SceneKit;

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

        private bool FireOnce = false;

        private void AdjustPlaneNode(SCNNode node, ARPlaneAnchor planeAnchor)
        {
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {

                var width = planeAnchor.Extent.X;
                var length = planeAnchor.Extent.Z;
                var count = planeNode.ParticleSystems?.Length ?? 0;

                if (planeNode.Geometry is SCNPlane planeGeometry)
                {
                    planeGeometry.Width = width;
                    planeGeometry.Height = length;
                }

                var angle = planeAnchor.Alignment == ARPlaneAnchorAlignment.Horizontal ? (float)(-Math.PI * 0.5) : 0.0f;
                planeNode.Transform = planeAnchor.Transform.ToSCNMatrix4();
                planeNode.Position = planeAnchor.Center.ToSCNVector3();
                planeNode.LocalRotate(SCNQuaternion.FromAxisAngle(SCNVector3.UnitX, angle));

                SCNParticleSystem fireSystem;
                if (FireOnce && count == 0)
                    return;

                if (count == 0)
                {
                    fireSystem = CreateFire();
                    planeNode.AddParticleSystem(fireSystem);
                    planeNode.Geometry.FirstMaterial.Diffuse.ContentColor = fireSystem.ParticleColor.ColorWithAlpha(0.4f);
                    FireOnce = true;
                }
                else 
                {
                    fireSystem = planeNode.ParticleSystems[count - 1];
                }


                var shape = fireSystem.EmitterShape;

                if (shape is SCNBox box)
                {
                    box.Length = length;
                    box.Width = width;
                    box.Height = 0.04f; //cm... 4* 5 == 20 cm...
                    //fireSystem.EmitterShape = box;
                }
                else if (shape is SCNPlane plane)
                {
                    plane.Width = width;
                    plane.Height = length;
                    //fireSystem.EmitterShape = plane;
                }
            }
        }

        private static SCNParticleSystem CreateFire()
        {
            var particleSystem = SCNParticleSystem.Create("Fire2.scnp", null);
            return particleSystem;
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
            BeginInvokeOnMainThread(() =>
            {
                node.RemoveFromParentNode();
                if (FireOnce)
                {
                    var planeNode = renderer.Scene.RootNode.FindChildNode(Constants.PlaneNodeName, true);
                    FireOnce = planeNode != null;
                }
               });
        }
    }
}
