// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using NaughtyAttributes;
using UnityEngine;

namespace Magnet.Physics.Force
{
    public class SamplerTest : MonoBehaviour
    {
        public ForceField TargetField;
        public int SampleCount;
        private Vector3[] PositionsToSample;
        private Vector3[] SampledForces;


        [Button]
        public void Sample()
        {
            PositionsToSample = new Vector3[SampleCount];
            double time = Time.realtimeSinceStartupAsDouble;
            
            SampledForces = TargetField.SamplePoints(PositionsToSample);
            
            Debug.Log($"Processing time: {(Time.realtimeSinceStartupAsDouble - time) * 1000.0}");
        }
    }
}
