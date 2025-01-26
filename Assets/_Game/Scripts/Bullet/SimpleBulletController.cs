using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        // Existing fields...
        public float NormalSpeed = 20f;
        public float BulletTimeSpeed = 5f;
        public bool Strafe = false;
        [Range(0f, 1f)]
        public float RotationDamping = 0.2f;

        public InputAxis MoveX = InputAxis.DefaultMomentary;
        public InputAxis MoveZ = InputAxis.DefaultMomentary;

        public UnityEvent OnBulletFired;

        // --- New: Expose these events so the AimController can hook in (like the sample).
        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;
        // "Vector3, float" signature parallels the sample's "PostUpdate(Vector3 velocity, float jumpAnimationScale)"

        private bool _fired;
        private bool _isBulletTime;
        private Vector3 _currentVelocity;

        // For convenience, provide a read-only property for the Aim script:
        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.01f;

        void OnEnable()
        {
            _fired = false;
            _isBulletTime = false;
            _currentVelocity = Vector3.zero;
        }

        void Update()
        {
            // Fire PreUpdate event (like the sample does)
            PreUpdate?.Invoke();

            // If bullet hasn't been fired yet, do nothing
            if (!_fired)
            {
                // PostUpdate with zero velocity
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            // Gather input
            var rawInput = new Vector3(MoveX.Value, 0f, MoveZ.Value);

            float speed = _isBulletTime ? BulletTimeSpeed : NormalSpeed;

            // Decide direction
            Vector3 desiredVelocity;
            if (Strafe)
            {
                // local strafe
                var localDir = new Vector3(rawInput.x, 0f, rawInput.z);
                if (localDir.sqrMagnitude > 1f) localDir.Normalize();
                desiredVelocity = transform.TransformDirection(localDir) * speed;
            }
            else
            {
                // directional
                var localDir = new Vector3(rawInput.x, 0f, rawInput.z);
                if (localDir.sqrMagnitude > 1f) localDir.Normalize();
                desiredVelocity = transform.TransformDirection(localDir) * speed;

                if (localDir.sqrMagnitude < 0.001f)
                    desiredVelocity = _currentVelocity.magnitude * transform.forward;
            }

            float t = 1f - Mathf.Clamp01(RotationDamping);
            _currentVelocity = Vector3.Slerp(_currentVelocity, desiredVelocity, t);

            // Move
            transform.position += _currentVelocity * Time.deltaTime;

            // If not strafing, rotate to face velocity
            if (!Strafe && _currentVelocity.sqrMagnitude > 0.01f)
            {
                var dir = _currentVelocity.normalized;
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            }

            // Fire PostUpdate event, passing local velocity
            // The sample uses "jumpAnimationScale" for the second param; we can just pass 1f.
            var localVel = Quaternion.Inverse(transform.rotation) * _currentVelocity;
            PostUpdate?.Invoke(localVel, 1f);
        }

        public void FireBullet()
        {
            if (_fired) return;
            _fired = true;
            _currentVelocity = transform.forward * NormalSpeed;
            OnBulletFired?.Invoke();
        }

        public void EnterBulletTime() => _isBulletTime = true;
        public void ExitBulletTime() => _isBulletTime = false;

        // IInputAxisOwner
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref MoveX,
                Name = "Move X",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
            axes.Add(new()
            {
                DrivenAxis = () => ref MoveZ,
                Name = "Move Z",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.Y
            });
        }
    }
}
