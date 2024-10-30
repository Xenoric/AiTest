using UnityEngine;

namespace Scripts
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int _health;

        public void Damage(int damage)
        {
            _health -= damage;

            if (_health <= 0)
                Destroy(gameObject);
        }
    }
}