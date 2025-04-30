// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Magnet.Physics.Force;
using UnityEditor;
using UnityEngine;

namespace Magnet.Physics
{
    #region Force Field Sphere

    [CustomEditor(typeof(ForceFieldSphere)), CanEditMultipleObjects]
    public class ForceFieldSphereEditor : Editor
    {
        private ForceFieldSphere script;

        private SerializedProperty gravityToggleProperty;
        
        private SerializedProperty fieldCenterProperty;
        
        private SerializedProperty centerHandleVisibleProperty;
        
        private SerializedProperty forcePowerProperty;
        
        private SerializedProperty showGizmosProperty;
        private SerializedProperty arrowGizmoSizeProperty;
        private SerializedProperty centerGizmoSizeProperty;

        private bool lastActive;
        private Vector3 lastFieldCenterProperty;
        

        private void OnEnable()
        {
            script = target as ForceFieldSphere;
            
            gravityToggleProperty = serializedObject.FindProperty(nameof(script.IsGravity));
            fieldCenterProperty = serializedObject.FindProperty(nameof(script.localCenter));
            forcePowerProperty = serializedObject.FindProperty(nameof(script.ForceTowardsCenter));

            centerHandleVisibleProperty = serializedObject.FindProperty(nameof(script.ShowCenterHandle));
            
            showGizmosProperty = serializedObject.FindProperty(nameof(script.ShowGizmos));
            arrowGizmoSizeProperty = serializedObject.FindProperty(nameof(script.forceArrowSize));
            centerGizmoSizeProperty = serializedObject.FindProperty(nameof(script.ForcePointGizmoSize));
            
            if(script.gameObject.activeInHierarchy) script.GenerateDebugPoints(); // Ensure we only generate the debug points when the script is active. (This caused a crash on disabled objects when it wasn't, oops)

            lastActive = script.gameObject.activeInHierarchy;
            
            Undo.undoRedoPerformed += PullFromScript;
            
            PullFromScript();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= PullFromScript;
        }

        private void PullFromScript()
        {
            gravityToggleProperty.boolValue = script.IsGravity;
            forcePowerProperty.floatValue = script.ForceTowardsCenter;
        }

        private void PushToScript()
        {
            if (script.gameObject.activeInHierarchy && fieldCenterProperty.vector3Value != lastFieldCenterProperty)
            {
                script.GenerateDebugPoints(); // Update debug points
            }
            
            script.IsGravity = gravityToggleProperty.boolValue;
            script.ForceTowardsCenter = forcePowerProperty.floatValue;
        }

        public override void OnInspectorGUI()
        {
            if (!lastActive && script.gameObject.activeInHierarchy) // We'll generate the points every time the script gameobject is enabled
            {
                script.GenerateDebugPoints();
            }
            
            EditorGUI.BeginChangeCheck();
            
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    lastFieldCenterProperty = fieldCenterProperty.vector3Value;
                    GUILayout.Space(5);
                    
                    EditorGUILayout.PropertyField(gravityToggleProperty, new GUIContent("Apply as Gravity", "Controls whether the field is applied as gravity or as force. Force respects mass, and may not apply solely to center of mass."));
                    
                    GUILayout.Space(5);

                    var lastPower = forcePowerProperty.floatValue;
                    forcePowerProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Force Towards Center", "Force to apply towards center, either as gravity or force. Negative values push objects away."), forcePowerProperty.floatValue);

                    if (script.gameObject.activeInHierarchy && forcePowerProperty.floatValue != lastPower)
                    {
                        script.GenerateDebugPoints();
                    }
                    
                    GUILayout.Space(5);
                    fieldCenterProperty.vector3Value = EditorGUILayout.Vector3Field(new GUIContent("Field Center", "Center of the field, displayed within local space"), fieldCenterProperty.vector3Value, GUILayout.MinWidth(200), GUILayout.MinHeight(30));

                    using (new EditorGUILayout.HorizontalScope()) // Field Center Handle (We handle the actual handle logic in OnSceneGUI)
                    {
                        if (!centerHandleVisibleProperty.boolValue)
                        {
                            if (GUILayout.Button(new GUIContent("Show Field Center Handle", "Show the center-point anchor handle.")))
                            {
                                Tools.current = Tool.None; // Hide transform handle
                                centerHandleVisibleProperty.boolValue = true;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent("Hide Field Center Handle", "Hide the center-point anchor handle.")))
                            {
                                centerHandleVisibleProperty.boolValue = false;
                            }
                        }
                        
                        if (GUILayout.Button(new GUIContent("Move Center to Self", "Moves the center position of the field to the current editor camera position.")))
                        {
                            if (SceneView.lastActiveSceneView)
                            {
                                fieldCenterProperty.vector3Value = script.transform.InverseTransformPoint(SceneView.lastActiveSceneView.camera.transform.position);
                            }
                        }
                    }
                }
            }
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(10);
                    showGizmosProperty.boolValue = EditorGUILayout.Foldout(showGizmosProperty.boolValue, "Show Gizmos", true, EditorStyles.foldout);
                }
            }
            
            if (showGizmosProperty.boolValue)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        arrowGizmoSizeProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Arrow Gizmo Size"), arrowGizmoSizeProperty.floatValue, 1f, 10f);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        centerGizmoSizeProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Center Gizmo Size"), centerGizmoSizeProperty.floatValue, 0.5f, 10f);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            
            if (EditorGUI.EndChangeCheck())
            {
                PushToScript();
            }
        }

        private void OnSceneGUI()
        {
            if (centerHandleVisibleProperty.boolValue)
            {
                if (Tools.current != Tool.None || !script.gameObject.activeInHierarchy) // If transform handle is visible again or gameobject is disabled
                {
                    centerHandleVisibleProperty.boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    DrawHandle();
                }
            }
        }

        private void DrawHandle()
        {
            Vector3 worldPos = script.transform.TransformPoint(script.localCenter);

            Vector3 newWorldPos = Handles.PositionHandle(worldPos, script.transform.rotation);

            if (newWorldPos != worldPos)
            {
                Undo.RecordObject(script, "Move Gizmo Point");
                
                script.localCenter = script.transform.InverseTransformPoint(newWorldPos);
                
                script.GenerateDebugPoints(); // Update debug points
                
                EditorUtility.SetDirty(script);
                UpdatePrefabData();
            }
        }

        private void UpdatePrefabData()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(script.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(script.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
        }
    }

    #endregion
    
    #region Force Field Zone
    
    [CustomEditor(typeof(ForceFieldZone)), CanEditMultipleObjects]
    public class ForceFieldZoneEditor : Editor
    {
        private ForceFieldZone script;

        private List<EditorForceFieldPoint> ForceFieldPoints = new List<EditorForceFieldPoint>();
        
        private SerializedProperty gravityToggleProperty;
        
        private SerializedProperty showGizmosProperty;
        private SerializedProperty arrowGizmoSizeProperty;
        
        private SerializedProperty forcePointGizmoSizeProperty;

        private bool lastActive;
        

        private void OnEnable()
        {
            script = target as ForceFieldZone;
            
            gravityToggleProperty = serializedObject.FindProperty(nameof(script.IsGravity));
            
            showGizmosProperty = serializedObject.FindProperty(nameof(script.ShowGizmos));
            arrowGizmoSizeProperty = serializedObject.FindProperty(nameof(script.forceArrowSize));
            
            forcePointGizmoSizeProperty = serializedObject.FindProperty(nameof(script.ForcePointGizmoSize));
            
            if(script.gameObject.activeInHierarchy) script.GenerateDebugPoints(); // Ensure we only generate the debug points when the script is active. (This caused a crash on disabled objects when it wasn't, oops)

            lastActive = script.gameObject.activeInHierarchy;
            
            Undo.undoRedoPerformed += PullFromScript;
            PullFromScript();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= PullFromScript;
        }

        private void PullFromScript()
        {
            gravityToggleProperty.boolValue = script.IsGravity;
            ForceFieldPoints = ConvertForcePoints(script.forces); // Annoyingly, I have to convert points to classes in-editor so that they can be modified without a full reconstruction of the ForceFieldPoint. (Will allow me to add extra info anyways, so oh well)
        }

        private void PushToScript()
        {
            if (script.gameObject.activeInHierarchy)
            {
                script.GenerateDebugPoints(); // Update debug points
            }
            
            Undo.RecordObject(script, "Pushed Values to ForceFieldZone");
            
            script.IsGravity = gravityToggleProperty.boolValue;
            script.forces = ConvertForcePoints(ForceFieldPoints); // Annoyingly, I have to convert points to classes in-editor so that they can be modified without a full reconstruction of the ForceFieldPoint. (Will allow me to add extra info anyways, so oh well)
            UpdatePrefabData();
        }

        public override void OnInspectorGUI()
        {
            if (!lastActive && script.gameObject.activeInHierarchy) // We'll generate the points every time the script gameobject is enabled
            {
                script.GenerateDebugPoints();
            }
            
            EditorGUI.BeginChangeCheck();
            
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(5);
                    
                    EditorGUILayout.PropertyField(gravityToggleProperty, new GUIContent("Apply as Gravity", "Controls whether the field is applied as gravity or as force. Force respects mass, and may not apply solely to center of mass."));
                    
                    GUILayout.Space(5);
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(5);

                    if (ForceFieldPoints.Count > 0)
                    {
                        for (var i = 0; i < ForceFieldPoints.Count; i++)
                        {
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                                    {
                                        if (GUILayout.Button("Focus on Point", EditorStyles.largeLabel))
                                        {
                                            if (SceneView.lastActiveSceneView)
                                            {
                                                SceneView.lastActiveSceneView.Frame(new Bounds(script.transform.TransformPoint(ForceFieldPoints[i].localPosition), Vector3.one * 5f), false);
                                                
                                                if (script.gameObject.activeInHierarchy)
                                                {
                                                    script.GenerateDebugPoints();
                                                }
                                            }
                                        }
                                    }
                                    GUILayout.FlexibleSpace();
                                    
                                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                                    {
                                        if (GUILayout.Button("✖", EditorStyles.largeLabel, GUILayout.Width(20)))
                                        {
                                            ForceFieldPoints.RemoveAt(i);

                                            if (script.gameObject.activeInHierarchy)
                                            {
                                                script.GenerateDebugPoints();
                                            }
                                            
                                            break;
                                        }
                                    }
                                }
                                
                                ForceFieldPoints[i].localPosition = EditorGUILayout.Vector3Field(new GUIContent("Force Position", "Position of the force, displayed within local space"), ForceFieldPoints[i].localPosition, GUILayout.MinWidth(200), GUILayout.MinHeight(30));
                                ForceFieldPoints[i].Force = EditorGUILayout.Vector3Field(new GUIContent("Force Power", "Direction and intensity of the force, in local space"), ForceFieldPoints[i].Force, GUILayout.MinWidth(200), GUILayout.MinHeight(30));
                            }
                        }
                    }
                    
                    GUILayout.Space(5);

                    if (GUILayout.Button(new GUIContent("Add Force Point", "Add a force point to the zone.")))
                    {
                        ForceFieldPoints.Add(new EditorForceFieldPoint()
                        {
                            localPosition = Vector3.up,
                            Force = Vector3.down * 9.807f
                        });
                        
                        if (script.gameObject.activeInHierarchy)
                        {
                            script.GenerateDebugPoints();
                        }
                    }
                }
            }
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(10);
                    showGizmosProperty.boolValue = EditorGUILayout.Foldout(showGizmosProperty.boolValue, "Show Gizmos", true, EditorStyles.foldout);
                }
            }
            
            if (showGizmosProperty.boolValue)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        arrowGizmoSizeProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Arrow Gizmo Size"), arrowGizmoSizeProperty.floatValue, 1f, 10f);
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        forcePointGizmoSizeProperty.floatValue = EditorGUILayout.Slider(new GUIContent("Force Point Gizmo Size"), forcePointGizmoSizeProperty.floatValue, 0.5f, 10f);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            
            if (EditorGUI.EndChangeCheck())
            {
                PushToScript();
            }
            
            //base.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            if (!script.ShowGizmos || !script.gameObject.activeInHierarchy) return;

            for (int i = 0; i < ForceFieldPoints.Count; i++)
            {
                switch (Tools.current)
                {
                    case Tool.Move:
                        DrawMoveHandle(i);
                        break;
                    case Tool.Rotate:
                        DrawRotateHandle(i);
                        break;
                    case Tool.Scale:
                        DrawScaleHandle(i);
                        break;
                }
            }
        }

        private void DrawMoveHandle(int forceIndex)
        {
            if (script.forces[forceIndex].Force.magnitude <= 0) return;
            
            Vector3 worldPos = script.transform.TransformPoint(script.forces[forceIndex].localPosition);

            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.LookRotation(script.forces[forceIndex].Force.normalized, script.transform.up));

            if (newWorldPos != worldPos)
            {
                Undo.RecordObject(script, "Move Gizmo Point");
                
                PullFromScript();
                
                ForceFieldPoints[forceIndex].localPosition = script.transform.InverseTransformPoint(newWorldPos);
                
                PushToScript();
                
                //serializedObject.ApplyModifiedProperties();
                
                script.GenerateDebugPoints(); // Update debug points
                
                EditorUtility.SetDirty(script);
                UpdatePrefabData();
            }
        }

        private void DrawRotateHandle(int forceIndex) // TODO - This has issues at specific angles, I need to figure out a quaternion approach. I might have to just have a quaternion backing for every force point
        {
            Vector3 force = script.forces[forceIndex].Force;
            float magnitude = force.magnitude;
            
            if (magnitude < 0.01f) return;

            Vector3 worldForce = script.transform.TransformDirection(force);

            Quaternion initialRotation = Quaternion.FromToRotation(Vector3.forward, worldForce.normalized);

            Quaternion newRotation = Handles.RotationHandle(initialRotation, script.transform.TransformPoint(script.forces[forceIndex].localPosition));

            if (Quaternion.Angle(initialRotation, newRotation) > 0.1f)
            {
                Undo.RecordObject(script, "Rotate Force Direction");
                PullFromScript();

                Vector3 newForceDirection = newRotation * Vector3.forward * magnitude;

                ForceFieldPoints[forceIndex].Force = script.transform.InverseTransformDirection(newForceDirection);

                PushToScript();
                serializedObject.ApplyModifiedProperties();
                script.GenerateDebugPoints();
                EditorUtility.SetDirty(script);
                UpdatePrefabData();
            }
        }

        private void DrawScaleHandle(int forceIndex)
        {
            Vector3 lastForceScale = script.forces[forceIndex].Force;

            if (lastForceScale.magnitude <= 0) return;
            
            var position = script.transform.TransformPoint(script.forces[forceIndex].localPosition);
            var dirRot = Quaternion.LookRotation(script.transform.TransformDirection(lastForceScale));

            float typeMultiplier = 1f;

            if (script.IsGravity)
            {
                typeMultiplier = 4f;
            }
            
            Vector3 newForceScale = lastForceScale.normalized * Handles.ScaleSlider(lastForceScale.magnitude, position, script.transform.TransformDirection(lastForceScale).normalized, dirRot, HandleUtility.GetHandleSize(position) * 0.75f, 1f * typeMultiplier);

            if (newForceScale != lastForceScale)
            {
                Undo.RecordObject(script, "Scale Force Direction");
                
                PullFromScript();

                ForceFieldPoints[forceIndex].Force = newForceScale;
                
                PushToScript();

                serializedObject.ApplyModifiedProperties();
                
                script.GenerateDebugPoints();
                
                EditorUtility.SetDirty(script);
                UpdatePrefabData();
            }
        }

        [Serializable]
        private class EditorForceFieldPoint
        {
            public Vector3 localPosition;
            public Vector3 Force;
        }

        #region Helper Methods

        private List<ForceFieldPoint> ConvertForcePoints(List<EditorForceFieldPoint> points)
        {
            List<ForceFieldPoint> final = new List<ForceFieldPoint>();

            for (int i = 0; i < points.Count; i++)
            {
                final.Add(new ForceFieldPoint()
                {
                    localPosition = points[i].localPosition,
                    Force = points[i].Force
                });
            }

            return final;
        }
        
        private List<EditorForceFieldPoint> ConvertForcePoints(List<ForceFieldPoint> points)
        {
            List<EditorForceFieldPoint> final = new List<EditorForceFieldPoint>();

            for (int i = 0; i < points.Count; i++)
            {
                final.Add(new EditorForceFieldPoint()
                {
                    localPosition = points[i].localPosition,
                    Force = points[i].Force
                });
            }

            return final;
        }
        
        private void UpdatePrefabData()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(script.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(script.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
        }

        #endregion
    }
    
    #endregion
    
    [CustomEditor(typeof(ForceField)), CanEditMultipleObjects] 
    public class ForceFieldEditor : Editor { }

    [CustomEditor(typeof(ForceFieldJobHandler)), CanEditMultipleObjects]
    public class ForceFieldJobHandlerEditor : Editor
    {
        public override void OnInspectorGUI() { }
    }
    
    [CustomEditor(typeof(ForceBounds)), CanEditMultipleObjects]
    public class ForceBoundsEditor : Editor
    {
        private ForceBounds script;
        
        private bool showDebug;

        private List<ForceTriggerListener> Colliders = new List<ForceTriggerListener>();
        
        private void OnEnable()
        {
            script = target as ForceBounds;
            
            Undo.undoRedoPerformed += PullFromScript;
            
            PullFromScript();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= PullFromScript;
        }

        private void PullFromScript()
        {
            //Debug.Log($"Pulled from script: {script.gameObject.name}");
            
            Colliders = script.Colliders;
        }

        private void PushToScript()
        {
            script.Colliders = Colliders;
        }
        
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(new GUIContent("Add Sphere", "Add a sphere collider bound")))
                {
                    Colliders.Add(CreateSphere());
                }
                
                if (GUILayout.Button(new GUIContent("Add Box", "Add a box collider bound")))
                {
                    Colliders.Add(CreateBox());
                }
                
                if (GUILayout.Button(new GUIContent("Add Capsule", "Add a capsule collider bound")))
                {
                    Colliders.Add(CreateCapsule());
                }
            }
            
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(5);

                    for (var i = 0; i < Colliders.Count; i++)
                    {
                        if (!Colliders[i])
                        {
                            Colliders.RemoveAt(i);
                            i -= 1;
                            continue;
                        }
                        
                        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) // Focus
                            {
                                if (GUILayout.Button("Select", EditorStyles.largeLabel))
                                {
                                    if (SceneView.lastActiveSceneView)
                                    {
                                        SceneView.lastActiveSceneView.Frame(new Bounds(Colliders[i].transform.position, Colliders[i].Collider.bounds.size), false);
                                    }

                                    Selection.activeGameObject = Colliders[i].gameObject;
                                }
                            }
                            
                            GUILayout.Space(10);
                            
                            GUILayout.Label(new GUIContent(Colliders[i].gameObject.name), EditorStyles.largeLabel, GUILayout.MaxWidth(350));
                            
                            GUILayout.FlexibleSpace();

                            if (Colliders.Count > 1)
                            {
                                if (GUILayout.Button("✖", EditorStyles.largeLabel, GUILayout.Width(20))) // Delete
                                {
                                    Undo.RecordObject(script, "Deleted trigger bound");
                                    
                                    DestroyImmediate(Colliders[i].gameObject);
                                    Colliders.RemoveAt(i);
                                    
                                    script.attachedField.GenerateDebugPoints();
                                    UpdatePrefabData();
                                    break;
                                }
                            }
                        }
                    }

                    GUILayout.Space(5);
                }
            }
            
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Space(10);
                    showDebug = EditorGUILayout.Foldout(showDebug, "Show Debug", true, EditorStyles.foldout);
                }
                
                if (showDebug)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        base.OnInspectorGUI();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            
            if (EditorGUI.EndChangeCheck())
            {
                PushToScript();
            }
        }

        #region Generate Colliders

        private ForceTriggerListener CreateSphere()
        {
            Undo.RecordObject(script, "Created trigger bound");
            
            ForceTriggerListener listener = null;

            GameObject newBounds = new GameObject($"Sphere Trigger", new Type[]
            {
                typeof(SphereCollider),
                typeof(ForceTriggerListener)
            });

            GameObjectUtility.SetParentAndAlign(newBounds, script.gameObject);

            var collider = newBounds.GetComponent<SphereCollider>();
            listener = newBounds.GetComponent<ForceTriggerListener>();
            
            collider.radius = 15f;
            collider.isTrigger = true;
            
            listener.Collider = collider;
            listener.Field = script.attachedField;
            
            UpdatePrefabData();

            return listener;
        }
        
        private ForceTriggerListener CreateBox()
        {
            Undo.RecordObject(script, "Created trigger bound");
            
            ForceTriggerListener listener = null;

            GameObject newBounds = new GameObject($"Box Trigger", new Type[]
            {
                typeof(BoxCollider),
                typeof(ForceTriggerListener)
            });

            GameObjectUtility.SetParentAndAlign(newBounds, script.gameObject);

            var collider = newBounds.GetComponent<BoxCollider>();
            listener = newBounds.GetComponent<ForceTriggerListener>();
            
            collider.size = Vector3.one * 15f;
            collider.isTrigger = true;
            
            listener.Collider = collider;
            listener.Field = script.attachedField;
            
            UpdatePrefabData();

            return listener;
        }
        
        private ForceTriggerListener CreateCapsule()
        {
            Undo.RecordObject(script, "Created trigger bound");
            
            ForceTriggerListener listener = null;

            GameObject newBounds = new GameObject($"Capsule Trigger", new Type[]
            {
                typeof(CapsuleCollider),
                typeof(ForceTriggerListener)
            });

            GameObjectUtility.SetParentAndAlign(newBounds, script.gameObject);

            var collider = newBounds.GetComponent<CapsuleCollider>();
            listener = newBounds.GetComponent<ForceTriggerListener>();
            
            collider.radius = 5f;
            collider.height = 15f;
            collider.isTrigger = true;
            
            listener.Collider = collider;
            listener.Field = script.attachedField;

            UpdatePrefabData();

            return listener;
        }

        #endregion

        private void UpdatePrefabData()
        {
            if (PrefabUtility.IsPartOfAnyPrefab(script.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(script.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
        }
    }
}
#endif
