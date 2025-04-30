// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
using CentauriCore.EventDispatcher;
using UnityEngine;

namespace Magnet.Physics.Force
{
    [RequireComponent(typeof(Rigidbody))]
    public partial class ForceObject : ForceInteractor, IDispatcher
    {
        /// <summary>
        /// All ForceObjectPoints for the ForceObject. Used for Force sampling within ForceFields
        /// </summary>
        public List<ForceObjectPoint> ForcePoints = new List<ForceObjectPoint>();


        #region Unity Methods

        public override void Awake()
        {
            base.Awake();
        }

        public override void OnDestroy()
        {
            // Clean up the event dispatcher
            if (EventDispatcher.HasHandler(EventDispatcher.EventType.FixedUpdate, this))
            {
                EventDispatcher.RemoveHandler(EventDispatcher.EventType.FixedUpdate, this);
            }
            
            base.OnDestroy();
        }

        #endregion

        #region Force Field Sampling

        public override Vector3[] ForcePositions
        {
            get // Converts all force points to world-space and returns them as a Vector3[]
            {
                Vector3[] points = new Vector3[ForcePoints.Count];

                for (int i = 0; i < ForcePoints.Count; i++)
                {
                    points[i] = transform.TransformPoint(ForcePoints[i].localPosition);
                }
                
                return points;
            }
        }

        #endregion

        #region Physics Handling

        private Vector3 totalGravity;

        public void FixedUpdateHandler(float fixedDeltaTime) // TODO - Replace this with Logic System implementation
        {
            totalGravity = Vector3.zero;
            foreach (var field in ActiveFields) // For all active ForceFields, we get the current set of force vectors
            {
                if (field.IsGravity) // When gravity, we get the total averaged force to apply to the center of mass
                {
                    Vector3[] forces = field.SamplePoints(this);
                    
                    foreach (var force in forces)
                    {
                        totalGravity += (force * Rigidbody.mass)/ForcePoints.Count; // Account for mass since all objects fall at the same rate, no matter their mass.
                    }
                }
                else // When a force, we apply each force to their respective ForcePoints
                {
                    Vector3[] forces = field.SamplePoints(this);

                    for (var i = 0; i < forces.Length; i++)
                    {
                        Rigidbody.AddForceAtPosition(forces[i] * (ForcePoints[i].MassPercentage * 0.01f), transform.TransformPoint(ForcePoints[i].localPosition), ForceMode.Force);
                    }
                }
            }
            
            Rigidbody.AddForce(totalGravity, ForceMode.Force); // Applying gravity to center of mass
        }

        #endregion

        #region Field Handling

        public override void OnEnterForceField(ForceField newField) // TODO - Replace this with Logic System implementation
        {
            base.OnEnterForceField(newField);
            
            if (!EventDispatcher.HasHandler(EventDispatcher.EventType.FixedUpdate, this))
            {
                EventDispatcher.AddHandler(EventDispatcher.EventType.FixedUpdate, this);
            }
        }

        public override void OnExitForceField(ForceField oldField) // TODO - Replace this with Logic System implementation
        {
            base.OnExitForceField(oldField);
            
            if (ActiveFields.Count < 1)
            {
                EventDispatcher.RemoveHandler(EventDispatcher.EventType.FixedUpdate, this);
            }
        }

        #endregion

        #region Gizmos

        [SerializeField] private bool ShowCenterOfMass;
        [SerializeField] private bool ShowForcePointGizmos;
        [SerializeField] private bool ShowForces;
        [SerializeField] private float generatedPointDensity = 0.5f; // Used by editor script when generating points

        public void OnDrawGizmosSelected()
        {
            if (ShowForcePointGizmos) // Draws a sphere at each force position, with a size relative to its mass percentage
            {
                Gizmos.color = Color.green;
                foreach (ForceObjectPoint pos in ForcePoints)
                {
                    Gizmos.DrawSphere(transform.TransformPoint(pos.localPosition), pos.MassPercentage / 500f);
                }
            }

            if (ShowCenterOfMass) // Draws a sphere at the rigidbody center of mass
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.TransformPoint(Rigidbody.centerOfMass), 0.1f);
            }
        }

        
        public void OnDrawGizmos()
        {
            if (!Application.isPlaying || !ShowForces) return;
            
            // When showing forces, we have to sample them here again because they are not cached on the ForceObject.
            
            Gizmos.color = Color.green;

            Vector3 gravity = Vector3.zero;
            Vector3[] positions = new Vector3[ForcePoints.Count];
            for (var i = 0; i < ForcePoints.Count; i++)
            {
                positions[i] = transform.TransformPoint(ForcePoints[i].localPosition);
            }
            
            foreach (var field in ActiveFields)
            {
                if (field.IsGravity)
                {
                    Vector3[] forces = field.SamplePoints(this);
                    
                    foreach (var force in forces)
                    {
                        gravity += force/ForcePoints.Count;
                    }
                }
                else
                {
                    Vector3[] forces = field.SamplePoints(this);

                    for (var i = 0; i < forces.Length; i++)
                    {
                        Gizmos.DrawRay(transform.TransformPoint(ForcePoints[i].localPosition), forces[i].normalized);
                    }
                }
            }
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Rigidbody.position, gravity / 4.91f);
        }

        #endregion
    }
}
