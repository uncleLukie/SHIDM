using UnityEngine;

namespace _Game.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public int health = 1;

        public void TakeDamage(int damage)
        {
            health -= damage;
            if (health <= 0)
            {
                // trigger death logic, animations, etc.
                Destroy(gameObject);
            }
        }
    }
}