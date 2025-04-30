// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using Unity.Mathematics;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Easy storage of a static localPosition and Force vector for ForceFields
    /// </summary>
    [System.Serializable]
    public struct ForceFieldPoint
    {
        /// <summary>
        /// Local position within the force field
        /// </summary>
        public Vector3 localPosition;

        /// <summary>
        /// Force at stored position, controlling both direction and intensity
        /// </summary>
        public Vector3 Force;
    }
    
    /// <summary>
    /// Unity.Mathematics version of ForceFieldPoint that can be used for Jobs.
    /// </summary>
    public struct ForceFieldJobPoint
    {
        /// <summary>
        /// World position within the force field
        /// </summary>
        public float3 position;

        /// <summary>
        /// Force at stored position, controlling both direction and intensity
        /// </summary>
        public float3 Force;

        public ForceFieldJobPoint(Vector3 worldPos, Vector3 force)
        {
            position = worldPos;
            Force = force;
        }

        /// <summary>
        /// Converts a ForceFieldPoint to a ForceFieldJobPoint
        /// </summary>
        /// <param name="point">Point to be referenced</param>
        /// <param name="FieldParent">Field parent of Point. Is used to convert ForceFieldPoint localPosition to a world-space position.</param>
        /// <returns></returns>
        public static ForceFieldJobPoint ConvertFromNormalPoint(ForceFieldPoint point, Transform FieldParent)
        {
            return new ForceFieldJobPoint(
                worldPos: FieldParent.TransformPoint(point.localPosition),
                force: FieldParent.TransformDirection(point.Force)
            );
        }
    }
}
