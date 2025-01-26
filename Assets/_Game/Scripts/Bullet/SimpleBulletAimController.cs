using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    /// <summary>
    /// An add-on for SimpleBulletController that allows decoupled camera rotation
    /// (like the sample’s SimplePlayerAimController, but for a bullet).
    /// 
    /// Place this as a child object under the bullet root.  The bullet root must have
    /// SimpleBulletController on it.  This child acts as a "bullet aiming core."
    /// 
    /// A CinemachineCamera with ThirdPersonFollow can then Follow this "aim core" transform,
    /// letting the user rotate camera with HorizontalLook/VerticalLook.  If Coupled,
    /// that rotation is also applied to the bullet.  If Decoupled, the bullet's motion is
    /// independent of the camera orientation.
    /// </summary>
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

        SimpleBulletController m_Bullet;     // The parent bullet controller
        Transform m_BulletTransform;         // The parent's transform
        Quaternion m_DesiredWorldRotation;   // We store this to preserve camera orientation

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

            // Subscribe to bullet’s PreUpdate/PostUpdate, like the sample does
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
            // Step 1: apply HorizontalLook/VerticalLook to THIS child's local rotation
            transform.localRotation = Quaternion.Euler(VerticalLook.Value, HorizontalLook.Value, 0);
            m_DesiredWorldRotation = transform.rotation;

            // Step 2: Decide how to handle bullet's rotation
            switch (BulletRotation)
            {
                case CouplingMode.Coupled:
                    // Force bullet to strafe mode = true if we want side input to be lateral
                    // or false if we want the bullet to rotate with camera? The sample sets strafe = true
                    // but your game might want strafe = false. Let's mimic the sample:
                    m_Bullet.Strafe = true;
                    RecenterBullet();  // forcibly match parent's yaw to this child's yaw
                    break;

                case CouplingMode.CoupledWhenMoving:
                    m_Bullet.Strafe = true;  // sample sets strafe = true
                    if (m_Bullet.IsMoving)
                        RecenterBullet(RotationDamping);
                    break;

                case CouplingMode.Decoupled:
                    // bullet strafe off if you want it to rotate by velocity only
                    m_Bullet.Strafe = false;
                    break;
            }

            // Let the input system handle recenters
            VerticalLook.UpdateRecentering(Time.deltaTime, VerticalLook.TrackValueChange());
            HorizontalLook.UpdateRecentering(Time.deltaTime, HorizontalLook.TrackValueChange());
        }

        /// <summary>
        /// Called by bullet controller after it updates velocity/position. 
        /// If in Decoupled mode, we maintain this child's world rotation,
        /// ignoring whatever rotation the bullet might have done.
        /// </summary>
        void PostUpdate(Vector3 vel, float speed)
        {
            if (BulletRotation == CouplingMode.Decoupled)
            {
                // The bullet may have rotated via velocity or strafe. 
                // We want to keep this child's world rotation the same as m_DesiredWorldRotation.
                transform.rotation = m_DesiredWorldRotation;

                // Then we recalculate the difference between bullet parent and me,
                // so the local HorizontalLook/VerticalLook axes remain correct.
                Quaternion delta = Quaternion.Inverse(m_BulletTransform.rotation) * m_DesiredWorldRotation;
                Vector3 eul = delta.eulerAngles;
                VerticalLook.Value = NormalizeAngle(eul.x);
                HorizontalLook.Value = NormalizeAngle(eul.y);
            }
        }

        /// <summary>
        /// Force the bullet's parent transform to match our child's yaw orientation, effectively
        /// making bullet face the same direction as the camera. The sample calls it RecenterPlayer().
        /// </summary>
        /// <param name="damping">If > 0, we do a softened rotation, else instant.</param>
        public void RecenterBullet(float damping = 0f)
        {
            if (m_BulletTransform == null)
                return;

            // This child's local rotation
            Vector3 rot = transform.localRotation.eulerAngles;
            // We only care about the Y axis difference 
            float delta = NormalizeAngle(rot.y);

            // apply some damping
            if (damping > 0f)
                delta = Damper.Damp(delta, damping, Time.deltaTime);

            // rotate the bullet parent by 'delta' around its own up
            m_BulletTransform.rotation = Quaternion.AngleAxis(delta, m_BulletTransform.up) * m_BulletTransform.rotation;

            // offset this child's local rotation by the opposite amount to preserve net orientation
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
