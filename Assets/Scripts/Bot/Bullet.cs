using System;
using UnityEngine;

namespace Scripts.Bot
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private int _damage;
        [SerializeField] private float _impulse;
        private GameObject _parent;
        private Rigidbody2D _rigidbody;

        private void Awake() => _rigidbody = GetComponent<Rigidbody2D>();

        public void Launch(GameObject parent)
        {
            _rigidbody.AddForce(_impulse * transform.up, ForceMode2D.Impulse);
            _parent = parent;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.TryGetComponent(out Health health))
            {
                if(other.gameObject != _parent)
                    health.Damage(_damage);
            }
            
            Destroy(gameObject);
        }
        /*private void OnTriggerEnter2D(Collision2D other)
        {
            if (other.gameObject.TryGetComponent(out Health health))
            {
                if(other.gameObject != _parent)
                    health.Damage(_damage);
            }
            
            Destroy(gameObject);
        }*/
    }
}