// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using Magnet.Utilities;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Needs to be attached to any colliders that a field uses for bounds checks.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ForceTriggerListener : MonoBehaviour // TODO - Once we handle OnTrigger events better, re-factor this code
    {
        /// <summary>
        /// Attached Collider
        /// </summary>
        [HideInInspector] public Collider Collider;
        /// <summary>
        /// Attached ForceField reference
        /// </summary>
        public ForceField Field;

        /// <summary>
        /// Used to keep track of the amount of colliders of a force object are currently within the field.
        /// </summary>
        private Dictionary<ForceInteractor, int> forceObjectCounter = new Dictionary<ForceInteractor, int>();
        
        public void InitializeListener()
        {
            Collider = GetComponent<Collider>();

            if (!Collider.isTrigger)
            {
                Collider.isTrigger = true;
            }
        }

        private void OnDestroy()
        {
            RemoveAllInteractors();
        }

        private void OnDisable()
        {
            RemoveAllInteractors();
        }

        private void OnEnable()
        {
            if (!Collider) return;
            
            StartCoroutine(OnEnableDelayed());
        }

        private IEnumerator OnEnableDelayed() // Allows OnTrigger events to play when the object is enabled
        {
            yield return new WaitForFixedUpdate();
            Collider.enabled = false;
            yield return new WaitForFixedUpdate();
            Collider.enabled = true;
        }

        private void OnTriggerEnter(Collider col)
        {
            if (ForceInteractor.InteractorColliderStorage.TryGetValue(col, out ForceInteractor forceObj)) // Checks if collider is connected to an interactor
            {
                if (forceObjectCounter.TryGetValue(forceObj, out int counter)) // We use counters to allow interactors with multiple colliders
                {
                    var newCount = counter + 1;
                    forceObjectCounter[forceObj] = newCount;
                }
                else
                {
                    forceObjectCounter.Add(forceObj, 1);
                    forceObj.OnEnteredForceFieldTrigger(Field);
                }
            }
        }
        
        public void OnTriggerExit(Collider col)
        {
            if (ForceInteractor.InteractorColliderStorage.TryGetValue(col, out ForceInteractor forceObj)) // Checks if collider is connected to an interactor
            {
                if (forceObjectCounter.TryGetValue(forceObj, out int counter)) // We use counters to allow interactors with multiple colliders
                {
                    var newCount = counter - 1;

                    if (newCount <= 0)
                    {
                        forceObj.OnExitedForceFieldTrigger(Field);
                        forceObjectCounter.Remove(forceObj);
                    }
                    else
                    {
                        forceObjectCounter[forceObj] = newCount;
                    }
                }
            }
        }

        /// <summary>
        /// Used by interactors to remove themselves from the trigger when disabled since OnTriggerExit doesn't fire.
        /// </summary>
        /// <param name="interactor"></param>
        public void RemoveInteractor(ForceInteractor interactor)
        {
            if (forceObjectCounter.TryGetValue(interactor, out int value))
            {
                for (int i = 0; i < value; i++)
                {
                    interactor.OnExitedForceFieldTrigger(Field);
                }

                forceObjectCounter.Remove(interactor);
            }
        }

        /// <summary>
        /// Since OnTriggerExit doesn't fire when a trigger is disabled or destroyed, we need to handle it manually.
        /// </summary>
        private void RemoveAllInteractors()
        {
            if (forceObjectCounter.Count > 0)
            {
                var keys = forceObjectCounter.Keys;
                foreach (var key in keys)
                {
                    for (int i = 0; i < forceObjectCounter[key]; i++) // Calls till counter is zero for each ForceInteractor
                    {
                        key.OnExitedForceFieldTrigger(Field);
                    }
                }
                
                forceObjectCounter.Clear();
            }
        }
    }
}
