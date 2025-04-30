// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Magnet.Physics.Force
{
    public abstract class ForceField : MonoBehaviour
    {
        /// <summary>
        /// Contains data controlling when a forcebody is actively within the ForceField.
        /// </summary>
        public ForceBounds ForceBounds;
        [HideInInspector] public List<Collider> boundColliders = new List<Collider>(); // Internally used for point sampling

        public ForceFieldJobHandler JobHandler;

        /// <summary>
        /// Controls whether to apply force with respect to mass.
        /// </summary>
        public bool IsGravity = false;

        #region Methods

        public virtual void Awake()
        {
            if (!ForceBounds) ForceBounds = GetComponent<ForceBounds>();
            if (!JobHandler) JobHandler = GetComponent<ForceFieldJobHandler>();

            JobHandler.Field = this;
            
            CreateBoundColliders();
        }

        public void CreateBoundColliders()
        {
            boundColliders.Clear();

            for (var i = 0; i < ForceBounds.Colliders.Count; i++)
            {
                if (!ForceBounds.Colliders[i])
                {
                    ForceBounds.Colliders.RemoveAt(i);
                    i -= 1;
                    continue;
                }
                
                ForceBounds.Colliders[i].InitializeListener();
                boundColliders.Add(ForceBounds.Colliders[i].Collider);
            }
        }
        
        /// <summary>
        /// Samples all the positions of a ForceInteractor
        /// </summary>
        /// <param name="target">Target ForceInteractor</param>
        /// <returns>Vector3 force direction and intensity</returns>
        public abstract Vector3[] SamplePoints(ForceInteractor target);
        
        /// <summary>
        /// Samples all the positions, returning in a single frame.
        /// NOTE | Less performant than using ForceInteractors! Use sparingly!
        /// </summary>
        /// <param name="positions">Input array of world-space positions</param>
        /// <returns>Vector3 force direction and intensity</returns>
        public abstract Vector3[] SamplePoints(Vector3[] positions);

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        
        [HideInInspector] public float forceArrowSize = 5f;
        private float arrowDensity = 5f;
        private List<Vector3> storedPoints;
        private Vector3[] storedVectors;
        private Vector3[] storedlocalVectors;
        
        [HideInInspector] public float ForcePointGizmoSize = 1f; 
        
        [HideInInspector] public bool ShowGizmos;
        [HideInInspector] public bool ShowCenterHandle;
        
        [ContextMenu("Generate Debug Points")]
        public void GenerateDebugPoints()
        {
            Awake();
            
            Bounds combinedBounds = boundColliders[0].bounds;
            foreach (Collider col in boundColliders)
            {
                combinedBounds.Encapsulate(col.bounds);
            }
            
            arrowDensity = combinedBounds.size.magnitude / 15f;

            GeneratePoints(out storedPoints);

            List<Vector3> worldSpacePoints = new List<Vector3>(storedPoints);
            for (int i = 0; i < worldSpacePoints.Count; i++)
            {
                worldSpacePoints[i] = transform.TransformPoint(worldSpacePoints[i]);
            }
            
            
            storedVectors = JobHandler.SampleForces(worldSpacePoints.ToArray());
            
            storedlocalVectors = new Vector3[storedVectors.Length];
            for (int i = 0; i < storedVectors.Length; i++)
            {
                storedlocalVectors[i] = transform.InverseTransformDirection(storedVectors[i]);
            }
            
            EditorUtility.SetDirty(this);
        }
        
        public virtual void OnDrawGizmosSelected()
        {
            if (!ShowGizmos) return;
            
            if (!ForceBounds)
            {
                ForceBounds = GetComponent<ForceBounds>();
            }
            
            if (ForceBounds && !ForceBounds.attachedField)
            {
                ForceBounds.attachedField = this;
            }

            if (!JobHandler)
            {
                JobHandler = GetComponent<ForceFieldJobHandler>();
            }
            
            Handles.color = IsGravity ? Color.blue : Color.green;

            if (storedPoints == null || storedPoints.Count < 1 || storedPoints.Count > storedVectors.Length)
            {
                GenerateDebugPoints();
                return;
            }

            if (storedlocalVectors.Length != storedVectors.Length)
            {
                GenerateDebugPoints();
            }

            var adjustedSize = forceArrowSize;
            var maxSize = 10f;
            if (!IsGravity) // If force, adjusting it like this makes it look better visually
            {
                adjustedSize /= 100f;
                maxSize *= 10f;
            }
            else
            {
                adjustedSize /= 10f;
            }

            for (int i = 0; i < storedPoints.Count; i++)
            {
                if (i > 6000) return;
                
                if (transform.TransformPoint(storedPoints[i]) != Vector3.zero && transform.TransformDirection(storedlocalVectors[i]) != Vector3.zero && transform.TransformDirection(storedVectors[i]) != Vector3.zero)
                {
                    Handles.ArrowHandleCap(
                        0,
                        transform.TransformPoint(storedPoints[i]),
                        Quaternion.LookRotation(transform.TransformDirection(storedlocalVectors[i])),
                        Mathf.Clamp(
                            HandleUtility.GetHandleSize(transform.TransformPoint(storedPoints[i])), 
                            0.1f, 
                            arrowDensity / 10) * (adjustedSize * Mathf.Clamp(storedVectors[i].magnitude, 0.01f, maxSize)),
                        EventType.Repaint
                    );
                }
            }
        }
        
        private void GeneratePoints(out List<Vector3> points)
        {
            points = new List<Vector3>();

            // Create bound colliders and store em
            CreateBoundColliders();
            var colliders = boundColliders;

            // If we dont have any bounds yet, dont try to generate points
            if (boundColliders == null || boundColliders.Count < 1) return;

            // Get the combined bounds of all our colliders
            Bounds combinedBounds = colliders[0].bounds;
            foreach (Collider col in colliders)
            {
                combinedBounds.Encapsulate(col.bounds);
            }

            Vector3 min = combinedBounds.min;
            Vector3 max = combinedBounds.max;

            // Iterate through all 3 axis, at the scale of our wanted arrow density
            for (float x = min.x; x <= max.x; x += arrowDensity)
            {
                for (float y = min.y; y <= max.y; y += arrowDensity)
                {
                    for (float z = min.z; z <= max.z; z += arrowDensity)
                    {
                        Vector3 point = new Vector3(x, y, z);

                        foreach (Collider col in colliders)
                        {
                            Vector3 closest = col.ClosestPoint(point);
                            if (closest == point)
                            {
                                points.Add(transform.InverseTransformPoint(point));
                                break;
                            }
                        }
                    }
                }
            }
        }
#endif

        #endregion
    }
}
