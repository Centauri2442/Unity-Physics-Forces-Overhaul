// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CentauriCore.EventDispatcher;
using Magnet.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace Magnet.Physics.Force
{
    /// <summary>
    /// Handles all ForceField sampling using the Jobs system with fast async multithreading.
    /// </summary>
    [BurstCompile]
    public class ForceFieldJobHandler : MonoBehaviour
    {
        #region Debug

        public static ulong pointCounter = 0;

        #endregion
        
        #region Variables

        private uint minFramesBetweenJobs = 10;
        private uint currentFrame = 0;

        /// <summary>
        /// Referenced ForceField
        /// </summary>
        public ForceField Field;

        #region Persistent Data Containers

        // Internal blittable arrays for storing important thread-safe data
        private NativeArray<SphereColliderData> sphereColliderData;
        private NativeArray<CapsuleColliderData> capsuleColliderData;
        private NativeArray<BoxColliderData> boxColliderData;

        private NativeArray<ForceFieldJobPoint> forcePoints;
        
        private NativeList<float3> flattenedPositionList;
        private NativeArray<float3> flattenedForceList;

        private NativeReference<int> fieldType; // 0 - Zone | 1 - Sphere

        /// <summary>
        /// Handle for currently active Job in the queue
        /// </summary>
        private JobHandle handle;
        private Coroutine jobRoutine;

        #endregion

        #region Force Object Data Container

        private Dictionary<MonoBehaviour, Vector3[]> allForceObjects = new Dictionary<MonoBehaviour, Vector3[]>(); // Used to connect the Jobs guid to the ForceObject script
        private BiDictionary<MonoBehaviour, IForceRef> allForceRefs = new BiDictionary<MonoBehaviour, IForceRef>(); // Used to connect the Jobs guid to the ForceObject script

        #endregion

        #region Temp Variables

        private List<IForceRef> currentForceObjects = new List<IForceRef>();
        private List<int> lengths = new List<int>();
        private Vector3 lastPos; // Used to check if we gotta update the containers

        #endregion

        #endregion

        #region Unity Methods

        private void OnDestroy()
        {
            CleanupMemory();
        }

        private void OnDisable()
        {
            CleanupMemory();
        }

        #endregion

        #region Force Sampling
        
        /// <summary>
        /// Sample the current stored forces for the target MonoBehaviour.
        /// </summary>
        /// <param name="target">Target MonoBehaviour</param>
        /// <returns>Vector3[] of all forces. Will return all Vector3.zero values if no forces found.</returns>
        public Vector3[] SampleForces<ForceRefMonoBehaviour>(ForceRefMonoBehaviour target) where ForceRefMonoBehaviour : MonoBehaviour, IForceRef
        {
            if (allForceObjects.TryGetValue(target, out Vector3[] forces))
            {
                return forces;
            }
            
            return new Vector3[target.ForcePositions.Length];
        }

        /// <summary>
        /// Sample the Vector[] of positions immediately within a single frame, returning the final data immediately.
        /// NOTE | Much less performant than using the queue, please use sparingly!
        /// </summary>
        /// <param name="positions">Vector3[] of world-space positions to sample.</param>
        /// <returns>FIFO Vector3[] of forces, of the same array size as the input positions.</returns>
        public Vector3[] SampleForces(Vector3[] positions)
        {
            #region Memory Allocation

            #region Containers
            
            NativeArray<SphereColliderData> sphereColData;
            NativeArray<CapsuleColliderData> capsuleColData;
            NativeArray<BoxColliderData> boxColData;

            List<SphereCollider> sphereCols = new List<SphereCollider>();
            List<CapsuleCollider> capsuleCols = new List<CapsuleCollider>();
            List<BoxCollider> boxCols = new List<BoxCollider>();

            var bounds = Field.boundColliders.ToArray();

            for (int i = 0; i < bounds.Length; i++)
            {
                switch (bounds[i])
                {
                    case SphereCollider sphere:
                        sphereCols.Add(sphere);
                        break;
                    case CapsuleCollider capsule:
                        capsuleCols.Add(capsule);
                        break;
                    case BoxCollider box:
                        boxCols.Add(box);
                        break;
                }
            }
            
            if (sphereCols.Count > 0)
            {
                sphereColData = new NativeArray<SphereColliderData>(sphereCols.Count, Allocator.TempJob);

                for (int i = 0; i < sphereCols.Count; i++)
                {
                    sphereColData[i] = JobColliderData.ConvertSphere(sphereCols[i]);
                }
            }
            else
            {
                sphereColData = new NativeArray<SphereColliderData>(0, Allocator.TempJob);
            }

            if (capsuleCols.Count > 0)
            {
                capsuleColData = new NativeArray<CapsuleColliderData>(capsuleCols.Count, Allocator.TempJob);
                
                for (int i = 0; i < capsuleCols.Count; i++)
                {
                    capsuleColData[i] = JobColliderData.ConvertCapsule(capsuleCols[i]);
                }
            }
            else
            {
                capsuleColData = new NativeArray<CapsuleColliderData>(0, Allocator.TempJob);
            }

            if (boxCols.Count > 0)
            {
                boxColData = new NativeArray<BoxColliderData>(boxCols.Count, Allocator.TempJob);
                
                for (int i = 0; i < boxCols.Count; i++)
                {
                    boxColData[i] = JobColliderData.ConvertBox(boxCols[i]);
                }
            }
            else
            {
                boxColData = new NativeArray<BoxColliderData>(0, Allocator.TempJob);
            }

            #endregion

            NativeList<float3> positionList = new NativeList<float3>(positions.Length, Allocator.TempJob); 
            NativeArray<float3> forceList = new NativeArray<float3>(positions.Length, Allocator.TempJob);
            NativeReference<int> tempFieldType = new NativeReference<int>(Allocator.TempJob); // 0 - Zone | 1 - Sphere
            NativeArray<ForceFieldJobPoint> points;
            
            switch (Field) // Depending on the field type, we need to handle the internal force positions/vectors differently
            {
                case ForceFieldZone zone: // For a zone, we gotta feed in all the FieldFieldPoints
                    points = new NativeArray<ForceFieldJobPoint>(zone.forces.Count, Allocator.TempJob);
                    
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = ForceFieldJobPoint.ConvertFromNormalPoint(zone.forces[i], zone.transform);
                    }

                    tempFieldType.Value = 0;
                    break;
                case ForceFieldSphere sphere: // For a sphere, we can just create a ForceFieldJobPoint using the center and force towards it.
                    points = new NativeArray<ForceFieldJobPoint>(1, Allocator.TempJob);
                    points[0] = new ForceFieldJobPoint()
                    {
                        Force = new Vector3(0f, 0f, sphere.ForceTowardsCenter),
                        position = sphere.transform.TransformPoint(sphere.localCenter)
                    };

                    tempFieldType.Value = 1;
                    break;
                default: // Just in case I forgor to add handling for new field types
                    throw new Exception($"Job handler for field {Field.gameObject.name} isn't a valid forcefield type! Setup job handling for it!");
            }

            for (int i = 0; i < positions.Length; i++) // Converts the Vector[] positions into a thread-safe float3[] NativeArray
            {
                positionList.Add(positions[i]);
            }

            #endregion

            #region Schedule Job

            CalculateForcesJob job = new CalculateForcesJob() // Create the job to be completed.
            {
                sphereColliders = sphereColData,
                capsuleColliders = capsuleColData,
                boxColliders = boxColData,
                fieldType = tempFieldType,
                positions = positionList,
                forces = forceList,
                forcePoints = points
            };
            
            // Schedule job and force it to complete immediately
            job.Schedule(positionList.Length, 1).Complete();

            #endregion

            #region Forces Assignment
            
            // Get the forces data out of the job, converting it back to Vector3s
            Vector3[] forces = new Vector3[forceList.Length];

            for (int i = 0; i < forceList.Length; i++)
            {
                forces[i] = forceList[i];
            }

            #endregion

            #region Cleanup Memory

            // Ensure we cleanup memory to prevent any memory leaks.
            positionList.Dispose();
            forceList.Dispose();
            tempFieldType.Dispose();
            points.Dispose();
            sphereColData.Dispose();
            capsuleColData.Dispose();
            boxColData.Dispose();

            #endregion

            return forces;
        }

        #endregion

        #region Memory Cleanup

        // Cleans up all memory within the entire job handler. Will also complete any active job.
        private void CleanupMemory()
        {
            ClearHandle();

            if (forcePoints.IsCreated)
            {
                forcePoints.Dispose();
                forcePoints = default;
            }
            if (flattenedPositionList.IsCreated)
            {
                flattenedPositionList.Dispose();
                flattenedPositionList = default;
            }
            if (flattenedForceList.IsCreated)
            {
                flattenedForceList.Dispose();
                flattenedForceList = default;
            }
            if (fieldType.IsCreated)
            {
                fieldType.Dispose();
                fieldType = default;
            }
            
            if (containersActive) // Ensures memory is cleared on object destruction
            {
                DisposeContainers();
            }
            currentForceObjects.Clear();
            allForceObjects.Clear();
            allForceRefs.Clear();
        }

        #endregion

        #region Collider Bounds Containers

        private bool containersActive;

        /// <summary>
        /// Generates collider data in a format readable by Jobs
        /// </summary>
        /// <param name="bounds"></param>
        private void CreateContainers(Collider[] bounds)
        {
            if (containersActive)
            {
                DisposeContainers();
            }
            
            List<SphereCollider> sphereCols = new List<SphereCollider>();
            List<CapsuleCollider> capsuleCols = new List<CapsuleCollider>();
            List<BoxCollider> boxCols = new List<BoxCollider>();

            // Gather up all the collider data, sorting it by type.
            for (int i = 0; i < bounds.Length; i++)
            {
                switch (bounds[i])
                {
                    case SphereCollider sphere:
                        sphereCols.Add(sphere);
                        break;
                    case CapsuleCollider capsule:
                        capsuleCols.Add(capsule);
                        break;
                    case BoxCollider box:
                        boxCols.Add(box);
                        break;
                }
            }

            #region NativeArray Creation
            
            // Using the sorted out colliders, convert all of it into Jobs compatible format.

            if (sphereCols.Count > 0)
            {
                sphereColliderData = new NativeArray<SphereColliderData>(sphereCols.Count, Allocator.Persistent);

                for (int i = 0; i < sphereCols.Count; i++)
                {
                    sphereColliderData[i] = JobColliderData.ConvertSphere(sphereCols[i]);
                }
            }
            else
            {
                sphereColliderData = new NativeArray<SphereColliderData>(0, Allocator.Persistent);
            }

            if (capsuleCols.Count > 0)
            {
                capsuleColliderData = new NativeArray<CapsuleColliderData>(capsuleCols.Count, Allocator.Persistent);
                
                for (int i = 0; i < capsuleCols.Count; i++)
                {
                    capsuleColliderData[i] = JobColliderData.ConvertCapsule(capsuleCols[i]);
                }
            }
            else
            {
                capsuleColliderData = new NativeArray<CapsuleColliderData>(0, Allocator.Persistent);
            }

            if (boxCols.Count > 0)
            {
                boxColliderData = new NativeArray<BoxColliderData>(boxCols.Count, Allocator.Persistent);
                
                for (int i = 0; i < boxCols.Count; i++)
                {
                    boxColliderData[i] = JobColliderData.ConvertBox(boxCols[i]);
                }
            }
            else
            {
                boxColliderData = new NativeArray<BoxColliderData>(0, Allocator.Persistent);
            }

            #endregion

            containersActive = true;
        }

        /// <summary>
        /// Updates collider data to new values
        /// </summary>
        /// <param name="bounds"></param>
        private void UpdateContainers(Collider[] bounds) // TODO - Figure out how to support moving and rotating fields without having to reconstruct the containers
        {
            if (!containersActive) return;
            
            CreateContainers(bounds);
        }

        /// <summary>
        /// Clears all collider data from memory
        /// </summary>
        private void DisposeContainers()
        {
            if (!containersActive) return;

            ClearHandle();

            sphereColliderData.Dispose();
            sphereColliderData = default;
            capsuleColliderData.Dispose();
            capsuleColliderData = default;
            boxColliderData.Dispose();
            boxColliderData = default;

            containersActive = false;
        }

        #endregion

        #region ForceObject Add/Remove

        /// <summary>
        /// Adds ForceRef to job pool
        /// </summary>
        /// <param name="forceRef">Monobehaviour containing IForceRef</param>
        public void AddForceRef<ForceRefMonoBehaviour>(ForceRefMonoBehaviour forceRef) where ForceRefMonoBehaviour : MonoBehaviour, IForceRef
        {
            if (allForceObjects.ContainsKey(forceRef)) return;

            if (allForceObjects.TryAdd(forceRef, new Vector3[forceRef.ForcePositions.Length]))
            {
                allForceRefs.Add((MonoBehaviour)forceRef, (IForceRef)forceRef);

                if (allForceObjects.Count == 1) // If we're adding the first object, start up the queue
                {
                    TryAddJobRoutine();
                }
            }
        }

        /// <summary>
        /// Removes ForceRef from job pool
        /// </summary>
        /// <param name="forceRef">Monobehaviour containing IForceRef</param>
        public void RemoveForceRef<ForceRefMonoBehaviour>(ForceRefMonoBehaviour forceRef) where ForceRefMonoBehaviour : MonoBehaviour, IForceRef
        {
            if (!allForceObjects.ContainsKey(forceRef)) return;
            
            if (allForceObjects.Remove(forceRef))
            {
                allForceRefs.Remove((MonoBehaviour)forceRef);

                if (allForceObjects.Count < 1) // If there aren't any objects in the field, we'll disable the queue
                {
                    TryRemoveJobRoutine();
                }
            }
        }

        #endregion

        #region Job Loop

        /// <summary>
        /// Attempt to start the job coroutine
        /// </summary>
        public void TryAddJobRoutine()
        {
            if (jobRoutine == null)
            {
                CreateContainers(Field.boundColliders.ToArray());
                
                jobRoutine = StartCoroutine(nameof(JobCoroutine));
            }
        }

        /// <summary>
        /// Attempt to stop the job coroutine
        /// </summary>
        public void TryRemoveJobRoutine()
        {
            if (jobRoutine != null)
            {
                StopCoroutine(jobRoutine);
                jobRoutine = null;
                
                CleanupMemory();
            }
        }

        public IEnumerator JobCoroutine()
        {
            while (true)
            {
                yield return new WaitUntil(() => handle.IsCompleted); // Wait till all the force calculations are done before doing a new set

                while (currentFrame < minFramesBetweenJobs) // Have a minimum calculation time delay, to prevent GC alloc issues due to calculating forces too fast.
                {
                    currentFrame++;
                    yield return new WaitForEndOfFrame();
                }
                currentFrame = 0;
                
                // Cleanup previous job
                
                handle.Complete();
                if (flattenedForceList.IsCreated) // Will call OnFinishJob if a job had just completed, and data exists to be processed.
                {
                    OnFinishJob();
                }
            
                // Create next job
                
                CreateJob();
            }
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Fires every time a Job in the queue finishes. Does NOT fire for the Vector3[] immediate point sampling, only for the queue.
        /// </summary>
        private void OnFinishJob()
        {
            pointCounter += (ulong)flattenedForceList.Length;
            
            // Unflattens the forces array back into the Dictionary containing all the current calculated forces!
            MonoBehaviour target;
            int startIndex = 0;
            int length = 0;
            for (int i = 0; i < currentForceObjects.Count; i++)
            {
                if (!allForceRefs.Contains(currentForceObjects[i]) || 
                    (allForceRefs.GetByValue(currentForceObjects[i], out target) && !target)) // If the ForceObject has been destroyed or left the field, skip it while making sure to adjust startIndex!
                {
                    startIndex += length;
                    continue;
                }

                try // Wrapping this in a try/catch just in case. It *shouldn't* ever have issues, but Im not 100% certain yet. TODO - Once Ive tested this enough, remove the try/catch
                {
                    allForceRefs.GetByValue(currentForceObjects[i], out target);
                    length = lengths[i];
                    var subArray = flattenedForceList.GetSubArray(startIndex, length).Reinterpret<Vector3>().ToArray();

                    allForceObjects[target] = subArray;

                    startIndex += length;
                    
                    currentForceObjects[i].OnForcesUpdated(subArray);
                }
                catch (Exception ex) // TODO - Currently this fires when the subArray is larger than the native array!
                {
                    //Debug.LogError($"Unable to apply {gameObject.name}'s forces to their objects! | {ex.Message}");
                }
            }
            
            currentForceObjects.Clear();

            if (transform.position != lastPos) // If the field has moved, we'll have to update the collider containers
            {
                UpdateContainers(Field.boundColliders.ToArray());
                lastPos = transform.position;
            }
        }

        private void CreateJob()
        {
            handle = new JobHandle();
            lastPos = transform.position;

            #region Populate Container Set
            // Populates and stores the current container set, which will get data applied back to it after calculations are finished!
            
            // Clear old data
            if (flattenedPositionList.IsCreated)
            {
                flattenedPositionList.Dispose();
                flattenedPositionList = default;
            }
            if (flattenedForceList.IsCreated)
            {
                flattenedForceList.Dispose();
                flattenedForceList = default;
            }
            if (forcePoints.IsCreated)
            {
                forcePoints.Dispose();
                forcePoints = default;
            }
            if (fieldType.IsCreated)
            {
                fieldType.Dispose();
                fieldType = default;
            }
            lengths.Clear();
            currentForceObjects.Clear();
            
            // Create new NativeList for positions
            flattenedPositionList = new NativeList<float3>(0, Allocator.Persistent);
            
            int size = 0;
            // Get the lengths before flattening, add current ForceObjects, and populate flattenedPositionList!
            foreach (var keyValuePairs in allForceRefs)
            {
                IForceRef forceRef = keyValuePairs.Value;
                var forcePositions = forceRef.ForcePositions;
                
                lengths.Add(forcePositions.Length);
                currentForceObjects.Add(forceRef);
                size += forcePositions.Length;
                
                // Since we gotta convert from Vector3 to float3, I iterate through it here. TODO - I want to make this more performant!
                for (int i = 0; i < forcePositions.Length; i++)
                {
                    flattenedPositionList.Add(forcePositions[i]);
                }
            }
            
            // Create new NativeArray for forces, we won't be populating it outside the job so we can keep it as an Array instead of a List
            flattenedForceList = new NativeArray<float3>(size, Allocator.Persistent);
            
            #endregion

            #region Get Force Points

            fieldType = new NativeReference<int>(Allocator.Persistent);

            switch (Field) // Depending on field type, I either grab all its points or create one using the data contained within the field
            {
                case ForceFieldZone zone:
                    forcePoints = new NativeArray<ForceFieldJobPoint>(zone.forces.Count, Allocator.Persistent);
                    
                    for (int i = 0; i < forcePoints.Length; i++)
                    {
                        forcePoints[i] = ForceFieldJobPoint.ConvertFromNormalPoint(zone.forces[i], zone.transform);
                    }

                    fieldType.Value = 0;
                    break;
                case ForceFieldSphere sphere:
                    forcePoints = new NativeArray<ForceFieldJobPoint>(1, Allocator.Persistent);
                    forcePoints[0] = new ForceFieldJobPoint()
                    {
                        Force = new Vector3(0f, 0f, sphere.ForceTowardsCenter),
                        position = sphere.transform.TransformPoint(sphere.localCenter)
                    };

                    fieldType.Value = 1;
                    break;
                default: // Just in case I forgor to add handling for new field types
                    throw new Exception($"Job handler for field {Field.gameObject.name} isn't a valid forcefield type! Setup job handling for it!");
            }

            #endregion

            #region Create & Schedule Job

            CalculateForcesJob job = new CalculateForcesJob() // Create job to be scheduled
            {
                sphereColliders = sphereColliderData,
                capsuleColliders = capsuleColliderData,
                boxColliders = boxColliderData,
                fieldType = fieldType,
                positions = flattenedPositionList,
                forces = flattenedForceList,
                forcePoints = forcePoints
            };
            
            // Assign our handle to it so we can keep track of when it completes
            handle = job.Schedule(flattenedPositionList.Length, 1);

            #endregion
        }

        /// <summary>
        /// Clears job handle if it's active
        /// </summary>
        private void ClearHandle()
        {
            if (handle.Equals(default)) return;
            
            handle.Complete();
        }

        [BurstCompile]
        private struct CalculateForcesJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeArray<SphereColliderData> sphereColliders;
            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeArray<CapsuleColliderData> capsuleColliders;
            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeArray<BoxColliderData> boxColliders;

            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeReference<int> fieldType;

            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeArray<ForceFieldJobPoint> forcePoints;
            [Unity.Collections.ReadOnly, NativeDisableParallelForRestriction] public NativeList<float3> positions;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<float3> forces;
            
            public void Execute(int index)
            {
                if (!IsWithinBounds(ref index)) return; // Return if not within bounds

                switch (fieldType.Value)
                {
                    case 0: // Zone
                        float3 weightedDirSum = float3.zero;
                        float weightedMagSum = 0f;
                        float totalWeight = 0f;

                        for (var i = 0; i < forcePoints.Length; i++)
                        {
                            // Using squared distance to avoid using sqrt
                            var delta = positions[index] - forcePoints[i].position;
                            var distSq = math.lengthsq(delta) + 0.0001f; // Avoid zero division
                            var weight = math.rsqrt(distSq); // Faster than division

                            // Then get magnitude and normalized direction for blending
                            float forceMagSq = math.lengthsq(forcePoints[i].Force);
                            float invMag = math.rsqrt(forceMagSq + 0.0001f); // Avoid zero division
                            float3 direction = forcePoints[i].Force * invMag; // Normalize
                            float magnitude = forceMagSq * invMag;

                            weightedDirSum += direction * weight;
                            weightedMagSum += magnitude * weight;
                            totalWeight += weight;
                        }

                        if (totalWeight > 0f)
                        {
                            // Average direction and magnitude
                            float3 blendedDirection = weightedDirSum / totalWeight;
                            var blendedMagnitude = weightedMagSum / totalWeight;

                            // Normalize blended direction and scale by magnitude
                            forces[index] = blendedDirection * blendedMagnitude * math.rsqrt(math.lengthsq(blendedDirection) + 0.0001f);
                        }
                        break;
                    case 1: // Sphere
                        
                        // Using more performant way to normalize without doing a square root calculation
                        float lengthSq = math.lengthsq(forcePoints[0].position - positions[index]);
                        if (lengthSq > 0.0001f)
                        {
                            forces[index] = ((forcePoints[0].position - positions[index]) * math.rsqrt(lengthSq)) * forcePoints[0].Force.z;
                        }
                        else
                        {
                            forces[index] = float3.zero;
                        }
                        break;
                }
            }

            #region Bounds Checks

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool IsWithinBounds(ref int index)
            {
                bool inside = false;

                // Check each collider type. We'll check from fastest to slowest computation time, and exit asap.
                
                if (sphereColliders.Length > 0)
                {
                    CheckSpheres(ref inside, ref index);
                }

                if (boxColliders.Length > 0 && !inside)
                {
                    CheckBoxes(ref inside, ref index);
                }
                
                if (capsuleColliders.Length > 0 && !inside)
                {
                    CheckCapsules(ref inside, ref index);
                }

                return inside;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckSpheres(ref bool inside, ref int index)
            {
                foreach (var sphere in sphereColliders)
                {
                    if (math.lengthsq(positions[index] - sphere.position) <= (sphere.radius * sphere.radius)) // Using squared distance since it's cheaper
                    {
                        inside = true; // If this point is inside, we just return and also mark all the other parallel operations to skip through to avoid the math
                        break;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckCapsules(ref bool inside, ref int index)
            {
                float3 dir;
                float3 rotatedDirection;
                float halfCylinderHeight;
                float3 sphereCenter1;
                float3 sphereCenter2;
                float3 closestPoint;

                foreach (var capsule in capsuleColliders)
                {
                    dir = float3.zero; // Get the direction
                    switch (capsule.direction)
                    {
                        default:
                            dir.x = 1;
                            break;
                        case 1:
                            dir.y = 1;
                            break;
                        case 2:
                            dir.z = 1;
                            break;
                    }

                    // Rotate the local direction using the rotation matrix
                    rotatedDirection = math.mul(capsule.rotation, dir);

                    // Get the half-length of the cylinder part (Not counting each end cause then it'll be too long!)
                    halfCylinderHeight = math.max(0, (capsule.height * 0.5f) - capsule.radius);

                    // Figure out the world-space positions of the two sphere centers
                    sphereCenter1 = capsule.position - rotatedDirection * halfCylinderHeight;
                    sphereCenter2 = capsule.position + rotatedDirection * halfCylinderHeight;
                    
                    // Find the closest point on the capsule's axis
                    closestPoint = GetClosestPointOnSegment(ref sphereCenter1, ref sphereCenter2, positions[index]);

                    // Check if the point is within the capsule's radius
                    if (math.lengthsq(positions[index] - closestPoint) <= capsule.radius * capsule.radius)
                    {
                        inside = true;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CheckBoxes(ref bool inside, ref int index)
            {
                foreach (var box in boxColliders)
                {
                    // Convert world-space point to local-space of the box
                    float3 localPoint = math.mul(math.inverse(box.rotation), (positions[index] - box.position));

                    // Check if the localPoint is within the extents
                    if (math.all(math.abs(localPoint) <= box.extents))
                    {
                        inside = true;
                    }
                }
            }

            #endregion

            #region Helpers

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private float3 GetClosestPointOnSegment(ref float3 segmentStart, ref float3 segmentEnd, float3 targetPoint)
            {
                float3 segmentVector = segmentEnd - segmentStart;
                float projectionFactor = math.dot(targetPoint - segmentStart, segmentVector) / math.dot(segmentVector, segmentVector);
                projectionFactor = math.clamp(projectionFactor, 0f, 1f);
                return segmentStart + projectionFactor * segmentVector;
            }

            #endregion
        }

        #endregion
    }
}
