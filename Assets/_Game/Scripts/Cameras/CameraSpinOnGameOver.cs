using UnityEngine;

namespace _Game.Scripts.Cameras
{
    public class CameraSpinOnGameOver : MonoBehaviour
    {
        [Tooltip("What the camera should orbit around.")]
        public Transform pivot;

        [Tooltip("Degrees per second around the pivot.")]
        public float rotationSpeed = 45f;

        private void Update()
        {
            if (!pivot) return;
            transform.RotateAround(pivot.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}