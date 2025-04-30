// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
using CentauriCore.EventDispatcher;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Any script that wants to automatically enter/exit ForceFields using collider triggers must inherit from this class.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ForceInteractor : MonoBehaviour, IForceRef
    {
        public abstract Vector3[] ForcePositions { get; }
        public virtual void OnForcesUpdated(Vector3[] forces) {}
        
        /// <summary>
        /// All colliders for the rigidbody
        /// </summary>
        public Collider[] ObjectColliders;
        
        /// <summary>
        /// Global dictionary containing all the ForceInteractorColliders.
        /// Is used to control entering/exiting ForceFields
        /// </summary>
        public static Dictionary<Collider, ForceInteractor> InteractorColliderStorage = new Dictionary<Collider, ForceInteractor>();

        public Rigidbody Rigidbody { get; set; }

        #region Unity Methods

        public virtual void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();

            Rigidbody.useGravity = false; // Ensures the rigidbody isn't using global gravity

            if (ObjectColliders == null || ObjectColliders.Length < 1)
            {
                Debug.LogError($"{gameObject.name} does not have any valid ForceInteractor colliders!");
                gameObject.SetActive(false);
            }
            
            // Make all colliders be keys to this script
            foreach (var col in ObjectColliders)
            {
                InteractorColliderStorage.Add(col, this);
            }
        }

        public virtual void OnDestroy()
        {
            // Clean up this script from the storage
            foreach (var col in ObjectColliders)
            {
                InteractorColliderStorage.Remove(col);
            }
            
            foreach (var field in ActiveFields)
            {
                field.JobHandler.RemoveForceRef(this);
            }
        }

        public virtual void OnDisable()
        {
            if (ActiveFields.Count < 1) return;

            var fields = new List<ForceField>(ActiveFields);
            foreach (var field in fields)
            {
                //field.JobHandler.RemoveForceRef(this);
                foreach (var listener in field.ForceBounds.Colliders)
                {
                    listener.RemoveInteractor(this);
                }
            }
        }

        #endregion
        
        #region Force Field Detection

        /// <summary>
        /// List of all fields that the interactor is currently inside.
        /// </summary>
        public List<ForceField> ActiveFields = new List<ForceField>();

        #endregion
        
        #region OnEnter/OnExit

        /// <summary>
        /// Fires once per unique field being entered.
        /// </summary>
        public virtual void OnEnterForceField(ForceField newField)
        {
            ActiveFields.Add(newField);
            
            newField.JobHandler.AddForceRef(this);
        }

        /// <summary>
        /// Fires once per unique field being exited.
        /// </summary>
        public virtual void OnExitForceField(ForceField oldField)
        {
            ActiveFields.Remove(oldField);
            
            oldField.JobHandler.RemoveForceRef(this);
        }

        #endregion

        #region Collider Incrementing

        // Internally stores the amount of colliders that the object is in, within each field.
        private Dictionary<ForceField, int> activeFieldColliderCount = new Dictionary<ForceField, int>();

        /// <summary>
        /// Fires every time the object enters a new forcefield trigger. This can fire multiple times per field.
        /// </summary>
        /// <param name="field"></param>
        public virtual void OnEnteredForceFieldTrigger(ForceField field)
        {
            if (activeFieldColliderCount.TryGetValue(field, out int counter))
            {
                var newCount = counter + 1;
                activeFieldColliderCount[field] = newCount;
            }
            else
            {
                activeFieldColliderCount.Add(field, 1);
                OnEnterForceField(field);
            }
        }
        
        /// <summary>
        /// Fires every time the object leaves a forcefield trigger. This can fire multiple times per field.
        /// </summary>
        /// <param name="field"></param>
        public virtual void OnExitedForceFieldTrigger(ForceField field)
        {
            if (activeFieldColliderCount.TryGetValue(field, out int counter))
            {
                var newCount = counter - 1;

                if (newCount <= 0)
                {
                    activeFieldColliderCount.Remove(field);
                    OnExitForceField(field);
                }
                else
                {
                    activeFieldColliderCount[field] = newCount;
                }
            }
        }

        #endregion
    }
}
