using UnityEngine;

namespace _Game.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        [Header("Animator References")]
        [Tooltip("Animator on this enemy (with default idle controller).")]
        public Animator animator;

        [Tooltip("Death animator controller (people-dead-pose).")]
        public RuntimeAnimatorController deathController;

        public void TriggerDeath()
        {
            // Swap animator controller to death animation
            if (animator != null && deathController != null)
            {
                animator.runtimeAnimatorController = deathController;
                animator.Play("dead-pose", 0, 0f);
            }
            else
            {
                Debug.LogWarning($"Enemy '{name}' has no assigned deathController or animator!");
            }
        }
    }
}