// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Stores a localPosition and MassPercentage for ForceObjects
    /// </summary>
    [System.Serializable]
    public struct ForceObjectPoint
    {
        /// <summary>
        /// Local position offset from the ForceObject.
        /// </summary>
        public Vector3 localPosition;

        /// <summary>
        /// A percentage value controlling how much of an ForceObject's mass this point controls.
        /// </summary>
        [Range(0f, 100f)] public float MassPercentage;
    }
}
