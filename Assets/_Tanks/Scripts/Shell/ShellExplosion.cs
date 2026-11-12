using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Complete
{
    public class ShellExplosion : MonoBehaviour
    {
        private bool m_HasExploded;
        public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion.
        [HideInInspector] public float m_MaxLifeTime = 2f;  // The time in seconds before the shell is removed.

        // All those are hidden in inspector as they will actually come from the TankShooting scripts
        [HideInInspector] public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank.
        [HideInInspector] public float m_ExplosionForce = 50f;                // The amount of force added to a tank at the centre of the explosion.
        [HideInInspector] public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected.


        private void Start ()
        {
            // If it isn't destroyed by then, destroy the shell after its lifetime.
            Destroy (gameObject, m_MaxLifeTime);
        }


        private void OnTriggerEnter (Collider other)
        {
            // Pickup and zone triggers must not detonate shells in mid-air.
            if (m_HasExploded || other.isTrigger) return;
            m_HasExploded = true;
			// Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            Collider[] colliders = Physics.OverlapSphere (transform.position, m_ExplosionRadius, m_TankMask);
            var damaged = new HashSet<TankHealth>();

            // Go through all the colliders...
            for (int i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody.
                Rigidbody targetRigidbody = colliders[i].attachedRigidbody;

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // Find the TankHealth script associated with the rigidbody.
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth> ();

                // If there is no TankHealth script attached to the gameobject, go on to the next collider.
                if (!targetHealth || !damaged.Add(targetHealth))
                    continue;

                var targetMovement = targetRigidbody.GetComponent<TankMovement>();
                if (targetMovement != null)
                    targetMovement.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

                // Calculate the amount of damage the target should take based on it's distance from the shell.
                float damage = CalculateDamage (targetRigidbody.position);

                // Armour applies only to the tank physically hit by this shell.
                // Nearby tanks retain ordinary distance-based splash damage.
                if (other.attachedRigidbody == targetRigidbody)
                {
                    var shellBody = GetComponent<Rigidbody>();
                    Vector3 incoming = shellBody != null ? shellBody.linearVelocity : transform.forward;
                    damage *= DuelArmor.GetDirectHitMultiplier(targetRigidbody.transform,
                        other.ClosestPoint(transform.position), incoming);
                }

                // Deal this damage to the tank.
                targetHealth.TakeDamage (damage);
            }

            // Unparent the particles from the shell.
            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.transform.parent = null;
                m_ExplosionParticles.Play();
                ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
                Destroy (m_ExplosionParticles.gameObject, mainModule.duration);
            }

            // Play the explosion sound effect.
            if (m_ExplosionAudio != null)
            {
                if (m_ExplosionAudio.transform.IsChildOf(transform) && (m_ExplosionParticles == null || m_ExplosionAudio.transform != m_ExplosionParticles.transform))
                {
                    m_ExplosionAudio.transform.parent = null;
                    float clipLen = m_ExplosionAudio.clip != null ? m_ExplosionAudio.clip.length : 1.5f;
                    Destroy(m_ExplosionAudio.gameObject, clipLen);
                }
                m_ExplosionAudio.Play();
            }

            // Destroy the shell.
            Destroy (gameObject);
        }


        private float CalculateDamage (Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            Vector3 explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_MaxDamage;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max (0f, damage);

            return damage;
        }
    }
}
