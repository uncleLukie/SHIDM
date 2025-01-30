using UnityEngine;


namespace _Game.Scripts.Environment
{
    [RequireComponent(typeof(Rigidbody))]
    public class TumbleweedController : MonoBehaviour
    {
        public float windStrength = 5f;
        public Vector3 windDirection = Vector3.right;
        public float rotationSpeedMultiplier = 2f;
        public float bounceForce = 2f;
        public float maxWindVariation = 2f;

        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = 1f; 
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.2f;
        }

        void FixedUpdate()
        {
            ApplyWindForce();
            AlignRotationWithMovement();
        }

        void ApplyWindForce()
        {
            Vector3 randomWind = windDirection.normalized *
                                 (windStrength + Random.Range(-maxWindVariation, maxWindVariation));
            rb.AddForce(randomWind, ForceMode.Force);
        }

        void AlignRotationWithMovement()
        {
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                Vector3 rotationAxis = Vector3.Cross(Vector3.up, rb.linearVelocity.normalized);
                float rollSpeed = rb.linearVelocity.magnitude * rotationSpeedMultiplier;
                rb.AddTorque(rotationAxis * rollSpeed, ForceMode.Force);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Length > 0)
            {
                Vector3 bounceDir = collision.contacts[0].normal;
                rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);
            }
        }
    }
}