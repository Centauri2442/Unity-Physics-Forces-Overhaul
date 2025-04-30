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

        #region Debug Methods
        
        /// <summary>
        /// These were used during development to generate positions within the field. If there is a point we need this functionality, we can re-implement it.
        /// </summary>
        
        /*
        
        [SerializeField] private float GeneratedPointSpacing = 0.5f;

        /// <summary>
        /// We can automatically generate random force points for different collider shapes using this. Honestly not really useful beyond debug testing.
        /// </summary>
        [ContextMenu("Generate force points")]
        private void GeneratePoints()
        {
            forces.Clear();

            List<Vector3> allSurfacePoints = new List<Vector3>();

            foreach (ForceTriggerListener listener in ForceBounds.Colliders)
            {
                Collider currentCollider;
                if (!listener.Collider)
                {
                    listener.Collider = listener.GetComponent<Collider>();
                }
                currentCollider = listener.Collider;
                switch (currentCollider)
                {
                    case BoxCollider box:
                        allSurfacePoints.AddRange(GenerateBoxColliderPoints(box));
                        break;
                    case SphereCollider sphere:
                        allSurfacePoints.AddRange(GenerateSphereColliderPoints(sphere));
                        break;
                    default:
                        allSurfacePoints.AddRange(GenerateColliderSurfacePoints(currentCollider));
                        break;
                }
            }

            foreach (var point in allSurfacePoints)
            {
                forces.Add(new ForceFieldPoint()
                {
                    Force = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f,
                    localPosition = transform.InverseTransformPoint(point)
                });
            }
        }
        
        #region Helpers

        private List<Vector3> GenerateBoxColliderPoints(BoxCollider collider)
        {
            List<Vector3> colliderSurfacePoints = new List<Vector3>();

            var center = collider.center;
            var extents = collider.size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(extents.x, extents.y, extents.z);
            corners[1] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[3] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[5] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[6] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[7] = center + new Vector3(-extents.x, -extents.y, -extents.z);

            // Convert the corners to worldspace. Although we technically convert it back in the actual force points, this just makes all the different collider point helper methods behave the same
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = collider.transform.TransformPoint(corners[i]);
            }

            // Add center and corners to list
            colliderSurfacePoints.Add(collider.bounds.center);
            colliderSurfacePoints.AddRange(corners);

            return colliderSurfacePoints;
        }

        private List<Vector3> GenerateSphereColliderPoints(SphereCollider collider)
        {
            List<Vector3> colliderSurfacePoints = new List<Vector3>();

            var pointSpacing = 4f;
            var rayCount = Mathf.CeilToInt(4 * Mathf.PI * collider.radius * collider.radius / (pointSpacing * pointSpacing));
            rayCount = Mathf.Max(rayCount, 10);

            List<Vector3> sampleDirections = GenerateDirections(rayCount);

            foreach (Vector3 direction in sampleDirections)
            {
                Vector3 point = collider.bounds.center + direction * collider.radius;

                colliderSurfacePoints.Add(point);
            }

            colliderSurfacePoints.Add(collider.bounds.center);

            return colliderSurfacePoints;
        }

        /// <summary>
        /// Gets (mostly) equally distant points on the input collider variable, using the point spacing variable to control point density.
        /// </summary>
        /// <param name="currentCollider"></param>
        /// <returns>A list of all the point positions for the specific input collider.</returns>
        private List<Vector3> GenerateColliderSurfacePoints(Collider collider)
        {
            List<Vector3> colliderSurfacePoints = new List<Vector3>();

            var colliderCenter = collider.bounds.center;
            var boundsRadius = collider.bounds.extents.magnitude;

            // This *should* ensure we get enough rays to properly calculate the surface points.
            var rayCount = Mathf.CeilToInt(4 * Mathf.PI * boundsRadius * boundsRadius / (GeneratedPointSpacing * GeneratedPointSpacing));
            rayCount = Mathf.Max(rayCount, 10);

            List<Vector3> sampleDirections = GenerateDirections(rayCount);
            var rayOriginDistance = boundsRadius * 1.5f;

            // We'll then iterate through the sample directions, shooting a raycast towards the collider to get the closest point.
            foreach (Vector3 direction in sampleDirections)
            {
                Vector3 rayOrigin = colliderCenter + direction * rayOriginDistance;
                Ray ray = new Ray(rayOrigin, -direction);

                if (collider.Raycast(ray, out RaycastHit hitInfo, rayOriginDistance * 2))
                {
                    colliderSurfacePoints.Add(hitInfo.point);
                }
            }

            return colliderSurfacePoints;
        }

        /// <summary>
        /// Creates a list of equally spaced directions within a sphere, so that we can get even spacing on the generated points.
        /// </summary>
        /// <param name="directionCount"></param>
        /// <returns>A list of directions</returns>
        private List<Vector3> GenerateDirections(int directionCount)
        {
            List<Vector3> directionVectors = new List<Vector3>(directionCount);
            var verticalOffset = 2f / directionCount;
            var goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

            // Iterates through the wanted amount of directions, using some math to generate them evenly spaced out
            for (int i = 0; i < directionCount; i++)
            {
                var direction = new Vector3(0f, 0f, 0f);

                direction.y = ((i * verticalOffset) - 1) + (verticalOffset / 2);

                var circleRadius = Mathf.Sqrt(1 - direction.y * direction.y);

                var angle = i * goldenAngle;

                direction.x = Mathf.Cos(angle) * circleRadius;

                direction.z = Mathf.Sin(angle) * circleRadius;

                directionVectors.Add(direction);
                
                // Imma be real with ya, even though I literally made this math myself I still don't quite fully understand it, since it uses some weird golden ratio shit I found when googling
            }

            return directionVectors;
        }

        #endregion */

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
