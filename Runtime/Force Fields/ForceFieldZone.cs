// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using Magnet.Physics.EditorExtensions;
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Implementation of a ForceField that allows a List of points with their own Force vectors, with smooth blending between them within the field.
    /// </summary>
    public class ForceFieldZone : ForceField
    {
        /// <summary>
        /// List of all ForceFieldPoints within the field 
        /// </summary>
        public List<ForceFieldPoint> forces = new List<ForceFieldPoint>();


        #region Point Sampling
        
        public override Vector3[] SamplePoints(ForceInteractor target)
        {
            if (forces.Count < 1) return new Vector3[target.ForcePositions.Length]; // Just returns no force if there isn't any forces within the system

            return JobHandler.SampleForces(target);
        }

        public override Vector3[] SamplePoints(Vector3[] positions)
        {
            return JobHandler.SampleForces(positions);
        }

        #endregion

        #region Gizmos

        #if UNITY_EDITOR
        
        private Texture2D anchorTex;
        
        public override void OnDrawGizmosSelected()
        {
            if (!ShowGizmos) return;
            
            base.OnDrawGizmosSelected();
            
            if (!anchorTex)
            {
                anchorTex = Resources.Load<Texture2D>("Force System/ForceAnchor");
            }

            foreach (var point in forces)
            {
                GizmosExtensions.DrawTextureAtPosition(transform.position + point.localPosition, anchorTex, ForcePointGizmoSize);
            }
        }
        #endif
        
        #endregion
    }
}
