using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using _Game.Scripts.Bullet;  // Make sure this references your own namespace for SimpleBulletAimController

namespace _Game.Scripts.Cameras
{
    /// <summary>
    /// A camera manager, similar to the Cinemachine sample's AimCameraRig, but adapted to:
    ///   - Search for SimpleBulletAimController instead of SimplePlayerAimController
    ///   - Switch cameras based on a "BulletTime" input axis
    ///   - When bullet time is active, we pick the "Aim" camera; otherwise we pick the "Free" camera
    ///   - Coupling the bullet's rotation with the camera in bullet time
    /// </summary>
    [ExecuteAlways]
    public class BulletAimCameraRig : CinemachineCameraManagerBase, IInputAxisOwner
    {
        [Tooltip("Input axis used to determine whether bullet time is active.  "
            + "If BulletTime.Value > 0.5, we enable the aim camera.")]
        public InputAxis BulletTime = InputAxis.DefaultMomentary;

        // We'll discover these children automatically at Start() by scanning ChildCameras.
        private CinemachineVirtualCameraBase _aimCamera;
        private CinemachineVirtualCameraBase _freeCamera;

        // Reference to the bullet's aim script, so we can set Coupled or Decoupled rotation
        private SimpleBulletAimController _bulletAimController;

        /// <summary>
        /// If BulletTime axis > 0.5, we consider bullet time active => use aim camera
        /// </summary>
        private bool IsBulletTimeActive => BulletTime.Value > 0.5f;

        /// <summary>
        /// Expose 'BulletTime' axis for CinemachineInputAxisController or other input system
        /// </summary>
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref BulletTime,
                Name = "BulletTime", // The name to match in your input system
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
        }

        protected override void Start()
        {
            base.Start();

            // Scan ChildCameras to find:
            //  - One that has CinemachineThirdPersonAim with NoiseCancellation => aim camera
            //  - One that doesn't => free camera
            // Then also find a SimpleBulletAimController in the aim camera's Follow target
            for (int i = 0; i < ChildCameras.Count; i++)
            {
                var cam = ChildCameras[i];
                if (!cam.isActiveAndEnabled)
                    continue;

                if (_aimCamera == null
                    && cam.TryGetComponent(out CinemachineThirdPersonAim aimComp) 
                    && aimComp.NoiseCancellation)
                {
                    // Found our aim camera
                    _aimCamera = cam;

                    // See if it has a Follow target with a SimpleBulletAimController child
                    var bullet = _aimCamera.Follow;
                    if (bullet != null)
                    {
                        _bulletAimController = bullet.GetComponentInChildren<SimpleBulletAimController>();
                    }
                }
                else if (_freeCamera == null)
                {
                    // By elimination, this must be the free camera
                    _freeCamera = cam;
                }
            }

            // Check if we found everything
            if (_aimCamera == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid 'Aim' camera found "
                             + "(no CinemachineThirdPersonAim with NoiseCancellation among children)");
            if (_bulletAimController == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid SimpleBulletAimController found "
                             + "in the Aim camera's Follow hierarchy");
            if (_freeCamera == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid 'Free' camera found among children");
        }

        /// <summary>
        /// Called by CinemachineCameraManagerBase each frame to choose which child camera is active.
        /// If bullet time is active => use the aim camera; else use free camera.
        /// Also toggles the bullet aim script's CouplingMode accordingly.
        /// </summary>
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            var chosenCam = IsBulletTimeActive ? _aimCamera : _freeCamera;

            // If we have a bullet aim script, set CouplingMode
            // so that when bullet time is on, the bullet is "coupled" to the camera
            // otherwise it's decoupled.
            if (_bulletAimController != null)
            {
                _bulletAimController.BulletRotation = IsBulletTimeActive
                    ? SimpleBulletAimController.CouplingMode.Coupled
                    : SimpleBulletAimController.CouplingMode.Decoupled;
            }

            return chosenCam;
        }
    }
}
