using System.Collections.Generic;
using _Game.Scripts.Bullet;
using UnityEngine;
using Unity.Cinemachine;

namespace _Game.Scripts.Cameras
{
    /// <summary>
    /// A custom camera manager that selects between an "Aim" camera and a "Free" camera.
    /// Instead of looking for SimplePlayerAimController, it looks for SimpleBulletAimController.
    /// 
    /// Steps:
    /// 1. AimMode axis > 0.5 => Switch to AimCamera, set bullet's CouplingMode = Coupled
    /// 2. AimMode axis <= 0.5 => Switch to FreeCamera, set bullet's CouplingMode = Decoupled
    /// 
    /// You can rename "AimMode" if you prefer something like "BulletTime" or "IsAiming".
    /// </summary>
    [ExecuteAlways]
    public class BulletAimCameraRig : CinemachineCameraManagerBase, IInputAxisOwner
    {
        [Tooltip("If this axis > 0.5, we activate AimCam. Else we pick FreeCam.")]
        public InputAxis AimMode = InputAxis.DefaultMomentary;

        [Tooltip("Camera used when not aiming (normal/free look).")]
        public CinemachineVirtualCameraBase FreeCam;

        [Tooltip("Camera used when aiming (close follow, third person aim, etc.).")]
        public CinemachineVirtualCameraBase AimCam;

        // Reference to the bullet's aim controller so we can set Coupled/Decoupled
        private SimpleBulletAimController m_BulletAim;

        private bool IsAimActive => AimMode.Value > 0.5f;

        /// <summary>Expose 'AimMode' to CinemachineInputAxisController or legacy input.</summary>
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref AimMode,
                Name = "Aim Mode", // or "BulletTime"
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
        }

        protected override void Start()
        {
            base.Start();

            // Find the bullet aim controller in the scene. 
            // Usually you'd have the bullet in the scene with a child "AimCore" that has SimpleBulletAimController
            // We'll do a naive approach: try to find it by searching the entire scene or
            // look on the cameras' Follow target if you prefer.
            if (AimCam != null && AimCam.Follow != null)
            {
                // If the bullet aim is on the AimCam.Follow or its children
                m_BulletAim = AimCam.Follow.GetComponentInChildren<SimpleBulletAimController>();
            }

            if (m_BulletAim == null)
            {
                Debug.LogError("BulletAimCameraRig: No valid SimpleBulletAimController found. " +
                               "Please assign a bullet with that script as the AimCam.Follow target.");
            }
            if (AimCam == null)
                Debug.LogError("BulletAimCameraRig: No AimCam assigned.");
            if (FreeCam == null)
                Debug.LogError("BulletAimCameraRig: No FreeCam assigned.");
        }

        /// <summary>
        /// CinemachineCameraManagerBase will call this to decide which camera is active.
        /// We'll pick AimCam if AimMode>0.5, else FreeCam. We'll also set the bullet aim's coupling.
        /// </summary>
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(
            Vector3 worldUp, float deltaTime)
        {
            var newCam = IsAimActive ? AimCam : FreeCam;

            if (m_BulletAim != null)
            {
                if (IsAimActive)
                {
                    // Force bullet to Coupled
                    m_BulletAim.BulletRotation = SimpleBulletAimController.CouplingMode.Coupled;
                }
                else
                {
                    // Force bullet to Decoupled (or CoupledWhenMoving if you prefer)
                    m_BulletAim.BulletRotation = SimpleBulletAimController.CouplingMode.Decoupled;
                }
            }

            return newCam;
        }
    }
}
