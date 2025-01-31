using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using _Game.Scripts.Bullet;

namespace _Game.Scripts.Cameras
{
    [ExecuteAlways]
    public class BulletAimCameraRig : CinemachineCameraManagerBase, IInputAxisOwner
    {
        [Tooltip("Input axis used to determine whether bullet time is active.  "
            + "If BulletTime.Value > 0.5, we enable the aim camera.")]
        public InputAxis BulletTime = InputAxis.DefaultMomentary;
        
        private CinemachineVirtualCameraBase _aimCamera;
        private CinemachineVirtualCameraBase _freeCamera;
        
        private SimpleBulletAimController _bulletAimController;
        
        private bool IsBulletTimeActive => BulletTime.Value > 0.5f;
        
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref BulletTime,
                Name = "BulletTime",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
        }

        protected override void Start()
        {
            base.Start();
            
            for (int i = 0; i < ChildCameras.Count; i++)
            {
                var cam = ChildCameras[i];
                if (!cam.isActiveAndEnabled)
                    continue;

                if (_aimCamera == null
                    && cam.TryGetComponent(out CinemachineThirdPersonAim aimComp) 
                    && aimComp.NoiseCancellation)
                {
                    _aimCamera = cam;
                    
                    var bullet = _aimCamera.Follow;
                    if (bullet != null)
                    {
                        _bulletAimController = bullet.GetComponentInChildren<SimpleBulletAimController>();
                    }
                }
                else if (_freeCamera == null)
                {
                    _freeCamera = cam;
                }
            }
            
            if (_aimCamera == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid 'Aim' camera found "
                             + "(no CinemachineThirdPersonAim with NoiseCancellation among children)");
            if (_bulletAimController == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid SimpleBulletAimController found "
                             + "in the Aim camera's Follow hierarchy");
            if (_freeCamera == null)
                Debug.LogError($"{nameof(BulletAimCameraRig)}: No valid 'Free' camera found among children");
        }
        
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            var chosenCam = IsBulletTimeActive ? _aimCamera : _freeCamera;
            
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
