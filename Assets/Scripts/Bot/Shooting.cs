using UnityEngine;

namespace Scripts.Bot
{
    public class Shooting : MonoBehaviour
    {
        [Header("Parameters")]
        [SerializeField] private float _shootingDistance;
        [SerializeField] private float _shotPeriod;
        [SerializeField] private float _focusSpeed;
        [SerializeField] private LayerMask _player;
        private float _timeToShoot;
        [Space]
        [SerializeField] private BotMovement _botMovement;
        [SerializeField] private Bullet _bullet;
        [SerializeField] private Transform _gun;
        [SerializeField] private Transform _shotPosition;

        private void Shoot()
        {
            if(!_botMovement.Enemy)
                return;
            
            var hit = Physics2D.Raycast(_shotPosition.position,
                (_botMovement.Enemy.position - _shotPosition.position).normalized,
                Mathf.Infinity);
            
            if(!hit || hit.collider.gameObject.transform != _botMovement.Enemy)
                return;
            
            var instancedBullet = Instantiate(_bullet, _shotPosition.position, Quaternion.identity);
            instancedBullet.transform.up = _shotPosition.right * _botMovement.ScaleX;
            instancedBullet.Launch(_botMovement.gameObject);
            _timeToShoot = _shotPeriod;
        }

        private void FocusTarget()
        {
            if (!_botMovement.Enemy)
                return;
            Vector3 direction = _botMovement.Enemy.position - _gun.transform.position;
            direction *= _botMovement.ScaleX;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var rotation = _gun.transform.rotation;
            _gun.transform.rotation = Quaternion.Lerp(rotation, 
                Quaternion.Euler(Vector3.forward * angle), 
                _focusSpeed);
        }
        
        private void Update()
        {
            if (_timeToShoot <= 0)
                return;

            _timeToShoot -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            FocusTarget();
            
            if (_botMovement.EnemyDistance > _shootingDistance || _timeToShoot > 0)
                return;
            
            Shoot();
        }
    }
}