// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System.Collections;
using System.Collections.Generic;
using CentauriCore.EventDispatcher;
using UnityEngine;

namespace Magnet.Physics.Force
{
    /// <summary>
    /// Adds ForceField functionality to a ParticleSystem.
    /// Uses attached rigidbody mass and drag for each particle's simulation.
    /// </summary>
    public class ForceParticleSystem : ForceInteractor, IDispatcher // TODO - Clean this up and add editor tooling!
    {
        [Tooltip("Target ParticleSystem")]
        public ParticleSystem Particles;

        [Header("Debug")]
        [SerializeField] private bool ShowForces;

        private Vector3[] particlePositions = new Vector3[0];
        
        public override Vector3[] ForcePositions
        {
            get { return new Vector3[1]; }
        }

        #region Unity Methods

        public override void Awake()
        {
            base.Awake();

            // Applies the drag from the attached rigidbody
            var limitVel = Particles.limitVelocityOverLifetime;

            limitVel.enabled = true;
            limitVel.dragMultiplier = Rigidbody.linearDamping;
        }

        #endregion

        public void FixedUpdateHandler(float fixedDeltaTime)
        {
            // Get all our particles
            ParticleSystem.Particle[] currentParticles = new ParticleSystem.Particle[Particles.particleCount];
            int particleCount = Particles.GetParticles(currentParticles);

            if (particleCount < 1) return;
            
            // Get their positions to feed into the fields
            particlePositions = new Vector3[particleCount];
            for (int i = 0; i < particleCount; i++)
            {
                particlePositions[i] = currentParticles[i].position;
            }
            
            foreach (var field in ActiveFields)
            {
                if (field.IsGravity)
                {
                    // Sample field
                    Vector3[] forces = field.SamplePoints(particlePositions);

                    for (int i = 0; i < particleCount; i++)
                    {
                        currentParticles[i].velocity += forces[i] * fixedDeltaTime; // Apply the forces to each particle's velocity.
                        
                        if(ShowForces) Debug.DrawRay(currentParticles[i].position, forces[i]/ (9.82f * 2f), Color.blue);
                    }
                
                    Particles.SetParticles(currentParticles, particleCount);
                }
                else
                {
                    // Sample field
                    Vector3[] forces = field.SamplePoints(particlePositions);

                    for (int i = 0; i < particleCount; i++)
                    {
                        currentParticles[i].velocity += (forces[i] * fixedDeltaTime) / Rigidbody.mass; // Apply the forces to each particle's velocity, accounting for the mass.
                        
                        if(ShowForces) Debug.DrawRay(currentParticles[i].position, (forces[i] * fixedDeltaTime) / Rigidbody.mass, Color.green);
                    }
                }
            }
            
            // Send the modified particles back into the system
            Particles.SetParticles(currentParticles, particleCount);
        }
        
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
    }
}
