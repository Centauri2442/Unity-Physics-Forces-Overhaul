// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Stores all colliders that a field uses for field interactor detection. Also acts as a connector for OnTrigger events from the ForceTriggerListeners.
    /// </summary>
    public class ForceBounds : MonoBehaviour
    {
        public ForceField attachedField;
        public List<ForceTriggerListener> Colliders = new List<ForceTriggerListener>();

        private void Awake()
        {
            if (Colliders == null || Colliders.Count < 1)
            {
                Debug.LogError($"{gameObject.name} doesn't have any colliders assigned to ForceBounds!");
            }

            if (!attachedField)
            {
                Debug.LogError($"{gameObject.name}'s ForceBounds does not have an attached ForceField!");
            }
            
            int counter = 0;
            foreach (var trigger in Colliders)
            {
                if (trigger)
                {
                    trigger.Field = attachedField;
                }
                else
                {
                    Debug.LogError($"Trigger not valid in {gameObject.name}! Collider index: {counter}");
                }

                counter++;
            }
        }
    }
}
