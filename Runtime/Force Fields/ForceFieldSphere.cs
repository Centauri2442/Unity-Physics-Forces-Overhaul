// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

#if UNITY_EDITOR
using Magnet.Physics.EditorExtensions;
using UnityEditor;
#endif
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Implementation of a ForceField that applies a Force towards a point in space.
    /// </summary>
    public class ForceFieldSphere : ForceField
    {
        /// <summary>
        /// Point in space where force is directed towards, local to the script transform's position.
        /// </summary>
        public Vector3 localCenter = Vector3.zero;
        
        /// <summary>
        /// Amount of Force to be applied towards the center of the ForceFieldSphere.
        /// </summary>
        public float ForceTowardsCenter = 1f;
        
        public override Vector3[] SamplePoints(ForceInteractor target)
        {
            return JobHandler.SampleForces(target);
        }

        public override Vector3[] SamplePoints(Vector3[] positions)
        {
            return JobHandler.SampleForces(positions);
        }

        #region Gizmos

        #if UNITY_EDITOR
        
        private Texture2D anchorTex;
        
        public override void OnDrawGizmosSelected()
        {
            if (!ShowGizmos) return;
            
            if (!anchorTex)
            {
                anchorTex = Resources.Load<Texture2D>("Force System/ForceAnchor");
            }
            
            GizmosExtensions.DrawTextureAtPosition(transform.position + localCenter, anchorTex, ForcePointGizmoSize);
            
            base.OnDrawGizmosSelected();
        }

        #endif
        #endregion
    }
}
