using System;
using System.Collections.Generic;
using System.Diagnostics;
using ARKit;
using SceneKit;
using UUID = Foundation.NSUuid;

namespace ARNativePortal.Models
{
    public class ARSceneViewHandler: ARSCNViewDelegate
    {
        private readonly Dictionary<UUID, SCNNode> anchorNodeDic = new Dictionary<UUID, SCNNode>();
        public override SCNNode GetNode(ISCNSceneRenderer renderer, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                Debug.WriteLine("GetNode with plane anchor: {0}", planeAnchor);
                var uuid = anchor.Identifier;
                if (anchorNodeDic.TryGetValue(uuid, out SCNNode retNode))
                    return retNode;

                var plane = new SCNPlane();
                var material = SCNMaterial.Create();
                material.Diffuse.ContentColor = UIKit.UIColor.Yellow.ColorWithAlpha(0.4f);
                plane.FirstMaterial = material;
                //Why it doesn't work?
                retNode = new SCNNode
                {
                    Geometry = plane,
                    WorldTransform = anchor.Transform.ToSCNMatrix4()
                };
                var rotation = retNode.Rotation;
                rotation.X += (float)Math.PI / 2;
                retNode.Rotation = rotation;
                //retNode.LocalRotate(SCNQuaternion.FromAxisAngle(SCNVector3.UnitY, (float)Math.PI/2));
                anchorNodeDic[uuid] = retNode;
                return retNode;
            }
            return base.GetNode(renderer, anchor);
        }

        public override void DidRemoveNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                Debug.WriteLine("DidRemoveNode with plane anchor: {0}", planeAnchor);
                var uuid = anchor.Identifier;
                if (anchorNodeDic.ContainsKey(uuid))
                {
                    anchorNodeDic.Remove(uuid);
                    node.RemoveFromParentNode();
                }
                return;
            }
            base.DidRemoveNode(renderer, node, anchor);
            
        }
    }
}
