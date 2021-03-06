﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ARKit;
using ARNativePortal.Helpers;
using AudioToolbox;
using CoreMedia;
using SceneKit;
using UIKit;

namespace ARNativePortal.Models
{
    public class ARSceneViewHandler: ARSCNViewDelegate
    {
        public delegate void DidGetAudioSampleDelegate(long value);
        public event DidGetAudioSampleDelegate DidGetAudioSample;

        public delegate void DidAddCustomNodeDelegate(SCNNode node, ARAnchor anchor);
        public event DidAddCustomNodeDelegate DidAddCustomNode;

        public delegate void DidUpdateCustomNodeDelegate(SCNNode node, ARAnchor anchor);
        public event DidUpdateCustomNodeDelegate DidUpdateCustomNode;

        public delegate void DidRemoveCustomNodeDelegate(SCNNode node, ARAnchor anchor);
        public event DidRemoveCustomNodeDelegate DidRemoveCustomNode;

        private bool TryToAdjustNode(SCNNode node, ARAnchor anchor)
        {
            if (anchor is ARPlaneAnchor planeAnchor)
            {
                AdjustPlaneNode(node, planeAnchor);
                return true;
            }
            return false;
        }

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


                if (count == 0)
                    return;
                var fireSystem = planeNode.ParticleSystems[count - 1];


                var shape = fireSystem.EmitterShape;
                if (shape is SCNBox box)
                {
                    box.Length = length;
                    box.Width = width;
                    box.Height = 0.04f; //cm... 4* 5 == 20 cm...
                    //fireSystem.Reset();
                }
                else if (shape is SCNPlane plane)
                {
                    plane.Width = width;
                    plane.Height = length;
                    //fireSystem.Reset();
                }
            }
        }

        public void ActivateFire(SCNNode node)
        {
            RemoveParticles(node);
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {
                var fireSystem = CreateFire();
                planeNode.AddParticleSystem(fireSystem);
                planeNode.Geometry.FirstMaterial.Diffuse.ContentColor = fireSystem.ParticleColor.ColorWithAlpha(0.4f);
            }
        }

        public void RemoveParticles(SCNNode node)
        {
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {
                planeNode.RemoveAllParticleSystems();
            }
        }

        public void RemoveEqualizer(SCNNode node)
        {
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {
                var equalizerNodes = planeNode.FindNodes((SCNNode curNode, out bool stop) =>
                {
                    stop = false;
                    return curNode.Name == Constants.EqualizerNodeName;
                });
                foreach (var equilazerNode in equalizerNodes)
                {
                    equilazerNode.RemoveFromParentNode();
                }
            }
        }

        public void ActivateEqualizer(SCNNode node, long value)
        {
            var planeNode = node.FindChildNode(Constants.PlaneNodeName, false);
            if (planeNode != null)
            {
                var count = 0;

                var color = UIColor.White;

                if (value > (long)VolumeRange.high)
                {
                    // Extra hight..
                    color = UIColor.Red.ColorWithAlpha(1.0f);
                    var scale = value/ (float)VolumeRange.high;
                    count = (int)Math.Ceiling(scale * 15);

                } else if (value > (long)VolumeRange.medium)
                {

                    // Extra hight..
                    var range = (float)(VolumeRange.high - VolumeRange.medium);
                    var scale = (value - (long)VolumeRange.medium) / range;
                    var alpha = (float)((scale + 1.0) * 0.5f);

                    count = (int)Math.Ceiling(scale * 10);
                    color = new UIColor(alpha, 0, 0, 1.0f);//Red.ColorWithAlpha((nfloat)alpha);
                } else if (value > (long)VolumeRange.low)
                {
                    // medium...
                    var range = (float)(VolumeRange.medium - VolumeRange.low);
                    var scale = (value - (long)VolumeRange.low) / range;
                    var alpha = (float)((scale + 1.0) * 0.5f);

                    color = new UIColor(alpha, alpha, 0, 1.0f);//UIColor.Yellow.ColorWithAlpha((nfloat)alpha);
                    count = (int)Math.Ceiling(scale * 5);
                }
                else
                {
                    //kind of noise...
                    var range = (float)VolumeRange.low;
                    var scale = (value - 0) / range;
                    var alpha = (float)((scale + 1.0) * 0.5f);
                    color = color = new UIColor(alpha, alpha, alpha, 1.0f);//UIColor.Yellow.ColorWithAlpha((nfloat)alpha);
                    //UIColor.White.ColorWithAlpha((nfloat)alpha);
                    count = (int)Math.Ceiling(scale * 2);
                }

                var height = (nfloat)0.1f;
                var yPosition = height * 0.5f;
                for (var i = 0; i < count; i++)
                {
                    var planeGeometry = planeNode.Geometry as SCNPlane;
                    var width = (nfloat)Math.Min(0.1f, planeGeometry.Width);
                    var length = (nfloat)Math.Min(0.1f, planeGeometry.Height);
                    var box = SCNBox.Create(width, height, length, 0);
                    var material = new SCNMaterial();
                    material.Diffuse.ContentColor = color;
                    material.Emission.ContentColor = color;
                    box.FirstMaterial = material;
                    var boxNode = SCNNode.Create();

                    boxNode.Name = Constants.EqualizerNodeName;
                    boxNode.Geometry = box;
                    boxNode.Position = new SCNVector3(0, 0, (float)yPosition);
                    planeNode.AddChildNode(boxNode);
                    yPosition += height;
                }
            }
            else
                Debug.Fail("No plane Node in " + node.DebugDescription);
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
                node.Name = anchor.Identifier.ToString();
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
                    DidAddCustomNode?.Invoke(node, anchor);
                });
            }
        }

        public override void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            node.Name = anchor.Identifier.ToString();

            BeginInvokeOnMainThread(() =>
            {
                TryToAdjustNode(node, anchor);
                DidUpdateCustomNode?.Invoke(node, anchor);
            });
        }

        public override void DidRemoveNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            BeginInvokeOnMainThread(() => 
            {
                node.RemoveFromParentNode();
                DidRemoveCustomNode?.Invoke(node, anchor);
            });
        }

        public override void DidOutputAudioSampleBuffer(ARSession session, CMSampleBuffer audioSampleBuffer)
        {
            var desc = audioSampleBuffer.GetAudioFormatDescription();
            Debug.WriteLine("Description " + desc);
            var streamDesc = desc.AudioStreamBasicDescription;
            /*var buffers = new AudioBuffers(1);
            var buffer = new AudioBuffer();
            buffer.NumberChannels = 1;
            buffer.DataByteSize = 0;*/
            double finalValueTemp = 0;
            Debug.WriteLine("Number of Samples: " + audioSampleBuffer.NumSamples);
            //Value 174 = no voice...
            var error = audioSampleBuffer.CallForEachSample((activeBuffer, index) =>
            {
                var blockBuffer = activeBuffer.GetDataBuffer();
                var count = blockBuffer.DataLength;
                var bytes = new byte[count];
                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                IntPtr pointer;
                var forEachError = CMSampleBufferError.None;
                try
                {
                    pointer = handle.AddrOfPinnedObject();

                    var currentError = blockBuffer.CopyDataBytes(0, count, pointer);
                    if (currentError == CMBlockBufferError.None)
                    {
                        long sum = 0;
                        var delta = sizeof(short);
                        var longCount = (long)count;
                        var newCount = longCount/delta;
                        for (var i = 0; i < longCount; i += delta)
                        {
                            var value = (long)BitConverter.ToInt16(bytes, i);
                            sum += (value * value)/newCount;
                        }
                        finalValueTemp += Math.Sqrt(sum)/audioSampleBuffer.NumSamples;
                    }
                    forEachError = (CMSampleBufferError)currentError;
                }
                catch (Exception exp)
                {
                    Debug.WriteLine("Exception " + exp);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
                return forEachError;
            });
            //var dataBuffer = audioSampleBuffer.GetDataBuffer();
            long finalValue = (long)finalValueTemp;
            Debug.WriteLine("Value " + finalValue);
            if (finalValue != 0)
            {
                DidGetAudioSample?.Invoke(finalValue);
            }
            //AudioBuffer(mNumberChannels: 1, mDataByteSize: 0, mData: nil))


            //buffers[0] = buffer;
            //streamDesc?.SampleRate
            //https://stackoverflow.com/questions/33030425/capturing-volume-levels-with-avcaptureaudiodataoutputsamplebufferdelegate-in-swi
        }

    }
}
