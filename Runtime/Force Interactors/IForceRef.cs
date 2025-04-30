// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Allows implementation of ForceField sampling for the inheritor script.
    /// </summary>
    public interface IForceRef
    {
        /// <summary>
        /// World-space positions that are fed into a forcefield when it calculates new forces.
        /// </summary>
        public Vector3[] ForcePositions { get; }

        /// <summary>
        /// Fires every time a field calculates new forces using the Job queue system.
        /// </summary>
        /// <param name="forces"></param>
        public void OnForcesUpdated(Vector3[] forces) { }
    }
}
