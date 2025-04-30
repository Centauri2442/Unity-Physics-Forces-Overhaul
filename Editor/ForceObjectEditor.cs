// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Magnet.Utilities;
using UnityEditor;
using UnityEngine;

namespace Magnet.Physics.Force
{
    public partial class ForceObject
    {
        [CustomEditor(typeof(ForceObject))]
        public class ForceObjectEditor : Editor
        {
            private ForceObject script;

            private List<EditorForceObjectPoint> forcePoints = new List<EditorForceObjectPoint>();

            private SerializedProperty collidersProperty;

            private SerializedProperty generatedPointDensityProperty;

            private SerializedProperty showGizmosProperty;
            private SerializedProperty showForcesProperty;
            private SerializedProperty showCenterOfMassProperty;


            private void OnEnable()
            {
                script = target as ForceObject;

                collidersProperty = serializedObject.FindProperty(nameof(script.ObjectColliders));
                showGizmosProperty = serializedObject.FindProperty(nameof(script.ShowForcePointGizmos));
                showForcesProperty = serializedObject.FindProperty(nameof(script.ShowForces));
                showCenterOfMassProperty = serializedObject.FindProperty(nameof(script.ShowCenterOfMass));
                generatedPointDensityProperty = serializedObject.FindProperty(nameof(script.generatedPointDensity));

                if (script.ObjectColliders == null || script.ObjectColliders.Length < 1)
                {
                    script.ObjectColliders = new Collider[1];
                }

                Undo.undoRedoPerformed += PullFromScript;

                PullFromScript();
            }

            private void OnDisable()
            {
                Undo.undoRedoPerformed -= PullFromScript;
            }

            private void PullFromScript()
            {
                if (!script.Rigidbody)
                {
                    script.Rigidbody = script.GetComponent<Rigidbody>();
                }

                if (script.Rigidbody.automaticCenterOfMass)
                {
                    script.Rigidbody.automaticCenterOfMass = false;
                }

                forcePoints = ConvertForcePoints(script.ForcePoints);

                if (forcePoints.Count < 1) // Ensures there is always at least 1 force point
                {
                    forcePoints.Add(new EditorForceObjectPoint()
                    {
                        localPosition = Vector3.zero,
                        MassPercentage = 100f
                    });

                    PushToScript();
                }
            }

            private void PushToScript()
            {
                Undo.RecordObject(script, "Pushed Values to ForceObject");

                script.ForcePoints = ConvertForcePoints(forcePoints);

                UpdatePrefabData();

                EditorUtility.SetDirty(script);
            }

            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Space(5);
                        EditorGUILayout.PropertyField(showGizmosProperty, new GUIContent("Show Handles", "Display handle gizmos in the scene view!"), GUILayout.MaxWidth(100));

                        EditorGUILayout.PropertyField(showForcesProperty, new GUIContent("Show Forces", "Display forces being applied to the ForceObject during runtime!"), GUILayout.MaxWidth(100));

                        EditorGUILayout.PropertyField(showCenterOfMassProperty, new GUIContent("Show Center Of Mass", "Display center of mass of the ForceObject!"), GUILayout.MaxWidth(100));
                    }
                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                #region Force Points

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Space(5);

                        for (var i = 0; i < forcePoints.Count; i++)
                        {
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) // Focus
                                    {
                                        if (GUILayout.Button("Focus", EditorStyles.largeLabel))
                                        {
                                            if (SceneView.lastActiveSceneView)
                                            {
                                                SceneView.lastActiveSceneView.Frame(new Bounds(script.transform.TransformPoint(forcePoints[i].localPosition), Vector3.one), false);
                                            }
                                        }
                                    }

                                    GUILayout.Space(10);

                                    GUILayout.Label(new GUIContent($"Force Point {i + 1}"), EditorStyles.largeLabel, GUILayout.MaxWidth(350));

                                    GUILayout.FlexibleSpace();

                                    if (forcePoints.Count > 1)
                                    {
                                        if (GUILayout.Button("✖", EditorStyles.largeLabel, GUILayout.Width(20))) // Delete
                                        {
                                            Undo.RecordObject(script, "Deleted Force Point");

                                            forcePoints.RemoveAt(i);

                                            UpdatePrefabData();
                                            break;
                                        }
                                    }
                                }

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    forcePoints[i].localPosition = EditorGUILayout.Vector3Field(new GUIContent("Force Position", "Position of the force point, displayed within local space"), forcePoints[i].localPosition, GUILayout.MinWidth(200), GUILayout.MinHeight(30));

                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Mass Percentage");
                                        var newPercentage = EditorGUILayout.Slider(forcePoints[i].MassPercentage, 0f, 100f);

                                        CalculateMassPercentages(i, newPercentage);
                                    }
                                }
                            }
                        }

                        GUILayout.Space(5);

                        if (GUILayout.Button(new GUIContent("Add Force Point", "Adds a force point to the object.")))
                        {
                            forcePoints.Add(new EditorForceObjectPoint()
                            {
                                localPosition = Vector3.up,
                                MassPercentage = 0f
                            });
                        }

                        GUILayout.Space(10);

                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                        GUILayout.Space(10);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // I gotta adjust the label width cause the text for the property field below this has wayyyy too much whitespace
                            float originalLabelWidth = EditorGUIUtility.labelWidth;
                            float originalFieldWidth = EditorGUIUtility.fieldWidth;
                            EditorGUIUtility.labelWidth = 100f;
                            EditorGUIUtility.fieldWidth = 50f;

                            EditorGUILayout.PropertyField(generatedPointDensityProperty, new GUIContent("Point Density", "Point density of generated points!"));

                            EditorGUIUtility.labelWidth = originalLabelWidth;
                            EditorGUIUtility.fieldWidth = originalFieldWidth;

                            GUIStyle style = new GUIStyle(GUI.skin.button) { richText = true };
                            if (GUILayout.Button(new GUIContent("Auto-Generate Points  [ <color=red><b>WILL WIPE ALL POINTS</b></color> ]", "Auto-Generates points with equal mass across the surface of the object, wiping existing points."), style, GUILayout.MinWidth(400)))
                            {
                                AutoGeneratePoints();
                            }
                        }
                    }
                }

                #endregion

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                #region Colliders

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIStyle listHelpBox = new GUIStyle(EditorStyles.helpBox) // I adjust padding here so that the list dropdown properly fits within the helpBox
                    {
                        padding = new RectOffset(20, 10, 2, 2)
                    };

                    using (new EditorGUILayout.VerticalScope(listHelpBox))
                    {
                        GUILayout.Space(5);

                        // Clear all null values from collider array
                        List<Collider> colliders = new List<Collider>(script.ObjectColliders);
                        colliders.RemoveNullValues();
                        script.ObjectColliders = colliders.ToArray();

                        EditorGUILayout.PropertyField(collidersProperty, new GUIContent("Object Colliders", "All the attached object colliders. These are used for object detection for ForceFields!"), true);

                        GUILayout.Space(5);

                        if (GUILayout.Button("Grab All Child Colliders"))
                        {
                            var newCols = GetAllColliders(script.gameObject);

                            foreach (var col in newCols) // Makes sure we dont add duplicates
                            {
                                if (!colliders.Contains(col))
                                {
                                    colliders.Add(col);
                                }
                            }

                            script.ObjectColliders = colliders.ToArray();

                            EditorUtility.SetDirty(script);
                            UpdatePrefabData();
                        }
                    }
                }

                #endregion

                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                {
                    PushToScript();
                }

                //base.OnInspectorGUI();
            }

            private void OnSceneGUI()
            {
                EditorGUI.BeginChangeCheck();

                Transform parent = script.transform;

                #region Handles

                for (int i = 0; i < forcePoints.Count; i++)
                {
                    if (script.ShowForcePointGizmos)
                    {
                        Vector3 worldPos = parent.TransformPoint(script.ForcePoints[i].localPosition);
                        Vector3 newWorldPos = Handles.PositionHandle(worldPos, parent.rotation);


                        // Convert back to local space and update the list if moved
                        if (newWorldPos != worldPos)
                        {
                            Undo.RecordObject(script, "Move Gizmo Point");

                            forcePoints[i].localPosition = parent.InverseTransformPoint(newWorldPos);

                            UpdateCenterOfMass();

                            EditorUtility.SetDirty(script);
                        }

                        #region Handle Label

                        // Displays helpful point information in the scene view
                        if (SceneView.lastActiveSceneView)
                        {
                            GUIStyle labelStyle = new GUIStyle()
                            {
                                richText = true
                            };
                            labelStyle.normal.textColor = Color.white;

                            Vector3 screenPos = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(worldPos);
                            screenPos.x += 20f;
                            screenPos.y += 20f;

                            Vector3 labelWorldPos = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(screenPos);

                            Handles.Label(labelWorldPos, $"<color=white>Force Point</color> {i + 1} \n " +
                                                         $"<color=white>Mass</color> : <color=yellow>{Mathf.Round(script.Rigidbody.mass * (forcePoints[i].MassPercentage / 100f))}</color> \n" +
                                                         $"<color=white>Percentage</color> : <color=green>{Mathf.Round((forcePoints[i].MassPercentage))}%</color>", labelStyle);
                        }

                        #endregion
                    }
                }

                #endregion

                if (EditorGUI.EndChangeCheck())
                {
                    PushToScript();
                }
            }

            private void UpdatePrefabData()
            {
                if (PrefabUtility.IsPartOfAnyPrefab(script.gameObject))
                {
                    PrefabUtility.UnpackPrefabInstance(script.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                }
            }

            #region Force Object Point Methods

            private void CalculateMassPercentages(int adjustedIndex, float newPercentage)
            {
                // Clamp the value and then store it
                newPercentage = Mathf.Clamp(newPercentage, 0f, 100f);
                forcePoints[adjustedIndex].MassPercentage = newPercentage;

                // Get the remaining percentage for us to distribute between all the other points
                float remainingTotal = 100f - newPercentage;

                float totalOtherMass = 0f;
                List<int> otherPoints = new List<int>(); // Gotta store the index of all the other positions we'll be changin

                for (int i = 0; i < forcePoints.Count; i++)
                {
                    if (i == adjustedIndex) continue; // Skip the one we changed

                    forcePoints[i].MassPercentage = Mathf.Clamp(forcePoints[i].MassPercentage, 0f, 100f);
                    totalOtherMass += forcePoints[i].MassPercentage;
                    otherPoints.Add(i);
                }

                // If the other points all have 0 mass or the total mass is 0, we'll give everyone the same percentage of mass
                if (totalOtherMass <= 0f && otherPoints.Count > 0)
                {
                    float mass = remainingTotal / otherPoints.Count;
                    foreach (int i in otherPoints)
                    {
                        forcePoints[i].MassPercentage = mass;
                    }
                }
                else // Otherwise we'll iterate through all the points and calculate their mass percentages
                {
                    foreach (int index in otherPoints)
                    {
                        forcePoints[index].MassPercentage = (forcePoints[index].MassPercentage / totalOtherMass) * remainingTotal;
                    }
                }

                UpdateCenterOfMass();
            }

            private void UpdateCenterOfMass()
            {
                #region Center of Mass Adjustment

                script.Rigidbody.automaticCenterOfMass = false;

                Vector3 newCenterOfMass = Vector3.zero;
                float totalMass = script.Rigidbody.mass;
                float accumulatedMass = 0f;

                for (int i = 0; i < forcePoints.Count; i++) // We iterate through the points, multiplying the point position by the point mass
                {
                    float pointMass = (forcePoints[i].MassPercentage / 100f) * totalMass;
                    newCenterOfMass += forcePoints[i].localPosition * pointMass;
                    accumulatedMass += pointMass;
                }

                if (accumulatedMass > 0) // We now gotta divide the new center of mass by the accumulated mass, to get the accurate center of all the points
                {
                    newCenterOfMass /= accumulatedMass;
                    script.Rigidbody.centerOfMass = newCenterOfMass;
                }

                #endregion
            }

            private List<EditorForceObjectPoint> ConvertForcePoints(List<ForceObjectPoint> points)
            {
                List<EditorForceObjectPoint> final = new List<EditorForceObjectPoint>();

                for (int i = 0; i < points.Count; i++)
                {
                    final.Add(new EditorForceObjectPoint
                    {
                        localPosition = points[i].localPosition,
                        MassPercentage = points[i].MassPercentage
                    });
                }

                return final;
            }

            private List<ForceObjectPoint> ConvertForcePoints(List<EditorForceObjectPoint> points)
            {
                List<ForceObjectPoint> final = new List<ForceObjectPoint>();

                for (int i = 0; i < points.Count; i++)
                {
                    final.Add(new ForceObjectPoint
                    {
                        localPosition = points[i].localPosition,
                        MassPercentage = points[i].MassPercentage
                    });
                }

                return final;
            }

            private class EditorForceObjectPoint
            {
                public Vector3 localPosition;
                public float MassPercentage;
            }

            #endregion

            #region Helpers

            /// <summary>
            /// Quick and dirty recursive method to get all child colliders.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns>List of all child colliders</returns>
            private static List<Collider> GetAllColliders(GameObject obj)
            {
                List<Collider> colliders = new List<Collider>();

                colliders.AddRange(obj.GetComponents<Collider>());

                foreach (Transform child in obj.transform)
                {
                    colliders.AddRange(GetAllColliders(child.gameObject));
                }

                return colliders;
            }

            #endregion

            #region Point Generation

            /// <summary>
            /// Automatically generates points on the surface of the ForceObject
            /// </summary>
            private void AutoGeneratePoints()
            {
                forcePoints.Clear();

                List<Vector3> allSurfacePoints = new List<Vector3>();

                foreach (var collider in script.ObjectColliders)
                {
                    switch (collider)
                    {
                        case BoxCollider box:
                            allSurfacePoints.AddRange(GenerateBoxColliderPoints(box));
                            break;
                        case SphereCollider sphere:
                            allSurfacePoints.AddRange(GenerateSphereColliderPoints(sphere));
                            break;
                        default:
                            allSurfacePoints.AddRange(GenerateColliderSurfacePoints(collider));
                            break;
                    }
                }

                allSurfacePoints = CombinePoints(0.1f, allSurfacePoints);

                foreach (var point in allSurfacePoints)
                {
                    forcePoints.Add(new EditorForceObjectPoint()
                    {
                        localPosition = script.transform.InverseTransformPoint(point),
                        MassPercentage = 100f / allSurfacePoints.Count
                    });
                }
            }

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

                // Add corners to list
                colliderSurfacePoints.AddRange(corners);

                return colliderSurfacePoints;
            }

            private List<Vector3> GenerateSphereColliderPoints(SphereCollider collider)
            {
                List<Vector3> colliderSurfacePoints = new List<Vector3>();

                var pointSpacing = 4f;
                var rayCount = Mathf.CeilToInt(4 * Mathf.PI * collider.radius * collider.radius / (pointSpacing * pointSpacing));
                rayCount = Mathf.Max(rayCount, 6);

                List<Vector3> sampleDirections = GenerateDirections(rayCount);

                foreach (Vector3 direction in sampleDirections)
                {
                    Vector3 point = collider.bounds.center + direction * collider.radius;

                    colliderSurfacePoints.Add(point);
                }

                //colliderSurfacePoints.Add(collider.bounds.center);

                return colliderSurfacePoints;
            }

            /// <summary>
            /// Gets (mostly) equally distant points on the input collider variable, using the point spacing variable to control point density.
            /// </summary>
            /// <param name="collider"></param>
            /// <returns>A list of all the point positions for the specific input collider.</returns>
            private List<Vector3> GenerateColliderSurfacePoints(Collider collider)
            {
                List<Vector3> colliderSurfacePoints = new List<Vector3>();

                var colliderCenter = collider.bounds.center;
                var boundsRadius = collider.bounds.extents.magnitude;

                // This *should* ensure we get enough rays to properly calculate the surface points.
                var rayCount = Mathf.CeilToInt(4 * Mathf.PI * boundsRadius * boundsRadius / (generatedPointDensityProperty.floatValue * generatedPointDensityProperty.floatValue));
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

            private List<Vector3> CombinePoints(float threshold, List<Vector3> points)
            {
                int pointCount = points.Count;

                // Each point initially is its own "group" (component).
                int[] groupParent = new int[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    groupParent[i] = i;
                }

                // Iterate over all unique pairs of points.
                for (int i = 0; i < pointCount; i++)
                {
                    for (int j = i + 1; j < pointCount; j++)
                    {
                        // Check if the points are within the threshold distance.
                        if (Vector3.Distance(points[i], points[j]) <= threshold)
                        {
                            MergeGroups(i, j);
                        }
                    }
                }

                // Group the points based on their final representative.
                Dictionary<int, List<Vector3>> clusters = new Dictionary<int, List<Vector3>>();
                for (int i = 0; i < pointCount; i++)
                {
                    int clusterRepresentative = FindGroup(i);
                    if (!clusters.ContainsKey(clusterRepresentative))
                    {
                        clusters[clusterRepresentative] = new List<Vector3>();
                    }

                    clusters[clusterRepresentative].Add(points[i]);
                }

                // Compute the centroid (average point) for each cluster.
                List<Vector3> mergedPoints = new List<Vector3>();
                foreach (List<Vector3> clusterPoints in clusters.Values)
                {
                    Vector3 centroid = Vector3.zero;
                    foreach (Vector3 point in clusterPoints)
                    {
                        centroid += point;
                    }

                    centroid /= clusterPoints.Count;
                    mergedPoints.Add(centroid);
                }

                return mergedPoints;

                // Find: Recursively finds the representative for a point's group.
                int FindGroup(int index)
                {
                    // Path compression: make each node point directly to the group representative.
                    if (groupParent[index] != index)
                    {
                        groupParent[index] = FindGroup(groupParent[index]);
                    }

                    return groupParent[index];
                }

                // Union: Merge two groups by linking their representatives.
                void MergeGroups(int indexA, int indexB)
                {
                    int groupA = FindGroup(indexA);
                    int groupB = FindGroup(indexB);

                    // If the groups are different, merge groupB into groupA.
                    if (groupA != groupB)
                    {
                        groupParent[groupB] = groupA;
                    }
                }
            }



            #endregion
        }
    }
}

#endif