using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace _Game.Scripts.Cameras
{
    /// <summary>
    /// A custom Camera Manager that chooses between two child Cinemachine cameras:
    /// - A normal "third-person" camera
    /// - A "bullet" camera
    /// based on a BulletTime input axis (0 or 1).
    /// </summary>
    [ExecuteAlways]
    public class BulletCameraRig : CinemachineCameraManagerBase, IInputAxisOwner
    {
        [Tooltip("Set this axis to 1.0f when in bullet time, 0.0f otherwise.")]
        public InputAxis BulletTime = InputAxis.DefaultMomentary;

        [Tooltip("Camera used in normal third-person play.")]
        public CinemachineVirtualCameraBase ThirdPersonCam;

        [Tooltip("Camera used when bullet-time is active and the user is steering the bullet.")]
        public CinemachineVirtualCameraBase BulletCam;

        bool IsBulletTime => BulletTime.Value > 0.5f;

        /// <summary>
        /// Report the available input axes to CinemachineInputAxisController
        /// so it knows to drive 'BulletTime' if you want to read it from
        /// the new or legacy Input System.
        /// </summary>
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref BulletTime,
                Name = "BulletTimeToggle" // The name you'd reference in the Input System
            });
        }

        protected override void Start()
        {
            base.Start();

            // If you haven't assigned them in the inspector, try to find them
            // among the child cameras. This is optional convenience logic.
            if (ThirdPersonCam == null || BulletCam == null)
            {
                foreach (var c in ChildCameras)
                {
                    if (ThirdPersonCam == null) ThirdPersonCam = c;
                    else if (BulletCam == null) BulletCam = c;
                }
            }
        }

        /// <summary>
        /// This is where we pick which camera is active. 
        /// If BulletTime axis is > 0.5, pick BulletCam, else pick ThirdPersonCam.
        /// </summary>
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(
            Vector3 worldUp, float deltaTime)
        {
            if (!IsBulletTime)
                return ThirdPersonCam;

            return BulletCam;
        }
    }
}
