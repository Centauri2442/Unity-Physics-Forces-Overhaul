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
    public class ForceFieldGenerator
    {
        [MenuItem("GameObject/Magnet/Force Fields/Sphere", false, 10)]
        public static void CreateSphereForceField(MenuCommand menuCommand)
        {
            #region Create Parent GameObject

            GameObject fieldObj = new GameObject("ForceField Sphere", new Type[]
            {
                typeof(ForceFieldSphere),
                typeof(ForceBounds),
                typeof(ForceFieldJobHandler)
            });

            if (Selection.activeGameObject != null)
            {
                GameObjectUtility.SetParentAndAlign(fieldObj, menuCommand.context as GameObject);
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                fieldObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            #endregion
            
            #region Generate Default Collider

            GameObject defaultTriggerBounds = new GameObject("Sphere Trigger", new Type[]
            {
                typeof(SphereCollider),
                typeof(ForceTriggerListener)
            });

            GameObjectUtility.SetParentAndAlign(defaultTriggerBounds, fieldObj);
            
            #endregion

            #region Fetch New Components

            var forceField = fieldObj.GetComponent<ForceFieldSphere>();
            var forceBounds = fieldObj.GetComponent<ForceBounds>();
            var jobHandler = fieldObj.GetComponent<ForceFieldJobHandler>();
            var sphereCollider = defaultTriggerBounds.GetComponent<SphereCollider>();
            var triggerListener = defaultTriggerBounds.GetComponent<ForceTriggerListener>();

            #endregion

            #region Assign Relevant Dependencies

            forceField.ForceBounds = forceBounds;
            forceBounds.attachedField = forceField;
            jobHandler.Field = forceField;
            
            sphereCollider.radius = 15f;
            sphereCollider.isTrigger = true;

            triggerListener.Collider = sphereCollider;
            triggerListener.Field = forceField;

            forceBounds.Colliders = new List<ForceTriggerListener>() { triggerListener };

            forceField.ForceTowardsCenter = 9.807f;

            #endregion

            Selection.activeGameObject = fieldObj;
        }
        
        [MenuItem("GameObject/Magnet/Force Fields/Zone", false, 10)]
        public static void CreateZoneForceField(MenuCommand menuCommand)
        {
            #region Create Parent GameObject

            GameObject fieldObj = new GameObject("ForceField Zone", new Type[]
            {
                typeof(ForceFieldZone),
                typeof(ForceBounds),
                typeof(ForceFieldJobHandler)
            });

            if (Selection.activeGameObject != null)
            {
                GameObjectUtility.SetParentAndAlign(fieldObj, menuCommand.context as GameObject);
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                fieldObj.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            #endregion
            
            #region Generate Default Collider

            GameObject defaultTriggerBounds = new GameObject("Box Trigger", new Type[]
            {
                typeof(BoxCollider),
                typeof(ForceTriggerListener)
            });

            GameObjectUtility.SetParentAndAlign(defaultTriggerBounds, fieldObj);
            
            #endregion

            #region Fetch New Components

            var forceField = fieldObj.GetComponent<ForceFieldZone>();
            var forceBounds = fieldObj.GetComponent<ForceBounds>();
            var jobHandler = fieldObj.GetComponent<ForceFieldJobHandler>();
            var boxCollider = defaultTriggerBounds.GetComponent<BoxCollider>();
            var triggerListener = defaultTriggerBounds.GetComponent<ForceTriggerListener>();

            #endregion

            #region Assign Relevant Dependencies

            forceField.ForceBounds = forceBounds;
            forceBounds.attachedField = forceField;
            jobHandler.Field = forceField;
            
            boxCollider.size = Vector3.one * 15f;
            boxCollider.isTrigger = true;

            triggerListener.Collider = boxCollider;
            triggerListener.Field = forceField;

            forceBounds.Colliders = new List<ForceTriggerListener>() { triggerListener };
            
            forceField.forces.Add(new ForceFieldPoint()
            {
                Force = new Vector3(0f, -9.807f, 0f),
                localPosition = Vector3.up
            });

            #endregion

            Selection.activeGameObject = fieldObj;
        }
    }
}
#endif
