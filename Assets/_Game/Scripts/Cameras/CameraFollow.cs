using UnityEngine;

namespace _Game.Scripts.Cameras
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("References")]
        public Transform target;

        [Header("Settings")]
        [Tooltip("Local space offset from the bullet. Since bullet's forward is -X, " +
                 "we use +X here to be 'behind' the bullet.")]
        public Vector3 localOffset = new Vector3(5f, 2.5f, 0f);

        [Tooltip("Smoothly interpolate camera position at this speed.")]
        public float followSpeed = 2f;

        [Tooltip("How far ahead in local space the camera should look. " +
                 "If bullet's forward is -X, a negative X here looks 'in front.'")]
        public float lookAheadDistance = -5f;

        private void LateUpdate()
        {
            if (!target) return;
            
            Vector3 desiredPosition = target.TransformPoint(localOffset);
            
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );
            
            Vector3 lookTarget = target.TransformPoint(new Vector3(lookAheadDistance, 0f, 0f));

            transform.LookAt(lookTarget);
        }
    }
}