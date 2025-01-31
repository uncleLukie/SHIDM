using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    public class SimpleBulletAimController : MonoBehaviour, IInputAxisOwner
    {
        public enum CouplingMode
        {
            Coupled,            // Bullet rotates with camera always
            CoupledWhenMoving,  // Bullet rotates with camera only if bullet is moving
            Decoupled           // Bullet rotation is independent of camera rotation
        }

        [Tooltip("How bullet rotation is coupled to the camera rotation:\n" +
                 "Coupled: bullet rotates with camera.\n" +
                 "CoupledWhenMoving: bullet rotates with camera only when bullet is moving.\n" +
                 "Decoupled: bullet rotation is independent.")]
        public CouplingMode BulletRotation = CouplingMode.CoupledWhenMoving;

        [Tooltip("If bullet rotates to match camera direction when bullet is moving, how fast does that happen?")]
        public float RotationDamping = 0.2f;

        [Tooltip("Horizontal Look axis: range in degrees, wrap = true for 360 spin.")]
        public InputAxis HorizontalLook = new()
        {
            Range = new Vector2(-180, 180),
            Wrap = true,
            Recentering = InputAxis.RecenteringSettings.Default
        };

        [Tooltip("Vertical Look axis: range in degrees, clamp to -70..70 or so.")]
        public InputAxis VerticalLook = new()
        {
            Range = new Vector2(-70, 70),
            Wrap = false,
            Recentering = InputAxis.RecenteringSettings.Default
        };

        SimpleBulletController m_Bullet;
        Transform m_BulletTransform;
        Quaternion m_DesiredWorldRotation;

        #region IInputAxisOwner Implementation

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref HorizontalLook,
                Name = "Horizontal Look",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
            axes.Add(new()
            {
                DrivenAxis = () => ref VerticalLook,
                Name = "Vertical Look",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.Y
            });
        }

        #endregion

        void OnValidate()
        {
            HorizontalLook.Validate();
            VerticalLook.Range.x = Mathf.Clamp(VerticalLook.Range.x, -180, 180);
            VerticalLook.Range.y = Mathf.Clamp(VerticalLook.Range.y, -180, 180);

            VerticalLook.Validate();
        }

        void OnEnable()
        {
            m_Bullet = GetComponentInParent<SimpleBulletController>();
            if (m_Bullet == null)
            {
                Debug.LogError("SimpleBulletController not found on parent object.", this);
                enabled = false;
                return;
            }

            m_BulletTransform = m_Bullet.transform;
            
            m_Bullet.PreUpdate += UpdateBulletRotation;
            m_Bullet.PostUpdate += PostUpdate;
        }

        void OnDisable()
        {
            if (m_Bullet != null)
            {
                m_Bullet.PreUpdate -= UpdateBulletRotation;
                m_Bullet.PostUpdate -= PostUpdate;
            }
            m_BulletTransform = null;
        }

        void UpdateBulletRotation()
        {
            transform.localRotation = Quaternion.Euler(VerticalLook.Value, HorizontalLook.Value, 0);
            m_DesiredWorldRotation = transform.rotation;
            
            switch (BulletRotation)
            {
                case CouplingMode.Coupled:
                    RecenterBullet();
                    break;

                case CouplingMode.CoupledWhenMoving:
                    if (m_Bullet.IsFired)
                        RecenterBullet(RotationDamping);
                    break;

                case CouplingMode.Decoupled:
                    break;
            }

            // Let the input system handle recenters
            VerticalLook.UpdateRecentering(Time.deltaTime, VerticalLook.TrackValueChange());
            HorizontalLook.UpdateRecentering(Time.deltaTime, HorizontalLook.TrackValueChange());
        }
        
        void PostUpdate(Vector3 vel, float speed)
        {
            if (BulletRotation == CouplingMode.Decoupled)
            {
                transform.rotation = m_DesiredWorldRotation;
                
                Quaternion delta = Quaternion.Inverse(m_BulletTransform.rotation) * m_DesiredWorldRotation;
                Vector3 eul = delta.eulerAngles;
                VerticalLook.Value = NormalizeAngle(eul.x);
                HorizontalLook.Value = NormalizeAngle(eul.y);
            }
        }
        
        public void RecenterBullet(float damping = 0f)
        {
            if (m_BulletTransform == null)
                return;
            
            Vector3 rot = transform.localRotation.eulerAngles;
            float delta = NormalizeAngle(rot.y);
            
            if (damping > 0f)
                delta = Damper.Damp(delta, damping, Time.deltaTime);
            
            m_BulletTransform.rotation = Quaternion.AngleAxis(delta, m_BulletTransform.up) * m_BulletTransform.rotation;
            
            HorizontalLook.Value -= delta;
            rot.y -= delta;
            transform.localRotation = Quaternion.Euler(rot);
        }

        static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
