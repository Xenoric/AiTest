using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Scripts.Bot;
using Scripts.Bot.Grid;
using Grid = Scripts.Bot.Grid.Grid;

[RequireComponent(typeof(Rigidbody2D))]
public class BotMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float _baseSpeed = 70f;
    [SerializeField] private float _damperSpeed = 35f;
    [SerializeField] private float _flyForce = 50f;
    [SerializeField] private float _maxFlyHeight = 100f;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField] private float _landingForce = 20f;
    
    [Header("Combat Parameters")]
    [SerializeField] private float _optimalCombatDistance = 20f;
    [SerializeField] private float _minCombatDistance = 15f;
    [SerializeField] private float _teammateAvoidanceRadius = 5f;
    [SerializeField] private float _pathUpdateInterval = 0.5f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundedThreshold = 0.1f;
    [SerializeField] private float _slopeCheckDistance = 1f;
    [SerializeField] private float _maxSlopeAngle = 45f;
    
    [Header("Smoothing")]
    [SerializeField] private float _movementSmoothTime = 0.1f;
    [SerializeField] private float _avoidanceSmoothTime = 0.2f;
    
    [Header("References")]
    [SerializeField] private LayerMask _teammateLayer;
    [SerializeField] private Grid _grid;
    [SerializeField] private EnemiesPool _enemiesPool;
    
    private Rigidbody2D _rigidbody;
    private Transform _transform;
    private Vector2 _position;
    private Transform _enemy;
    private List<Node> _path;
    private Vector2 _moveDirection;
    private bool _isGrounded;
    private bool _shouldFly;
    private int _currentPathIndex;
    private int _scaleX = 1;
    private float _minCombatDistanceSqr;
    private float _optimalCombatDistanceSqr;
    private Vector2 _currentVelocity;
    private Vector2 _smoothedAvoidanceForce;
    private Vector2 _avoidanceVelocity;
    
    private List<Collider2D> _cachedNearbyColliders = new List<Collider2D>();
    private float _colliderCacheTime = 0.1f;
    private float _colliderCacheTimer;
    private float _enemyUpdateInterval = 0.5f;
    private float _enemyUpdateTimer;

    public float EnemyDistance => _enemy ? Vector2.Distance(_position, _enemy.position) : Mathf.Infinity;
    public Transform Enemy => _enemy;
    public int ScaleX => _scaleX;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _transform = transform;
        _scaleX = (int)_transform.localScale.x;
        _minCombatDistanceSqr = _minCombatDistance * _minCombatDistance;
        _optimalCombatDistanceSqr = _optimalCombatDistance * _optimalCombatDistance;
    }

    private void Start()
    {
        _enemy = _enemiesPool.GetClosest(_transform.position);
        StartCoroutine(UpdatePathRoutine());
    }

    private void FixedUpdate()
    {
        _position = _transform.position;
        UpdateGroundedState();
        UpdateEnemyTracking();
        HandleMovement();
        HandleTeammateAvoidance();
    }

    private void UpdateGroundedState()
    {
        _isGrounded = Physics2D.OverlapCircle(_position, _groundCheckDistance, _groundLayer);

        if (!_isGrounded)
        {
            _rigidbody.AddForce(Vector2.down * _landingForce);
        }
    }

    private void UpdateEnemyTracking()
    {
        _enemyUpdateTimer -= Time.fixedDeltaTime;
        if (_enemyUpdateTimer <= 0)
        {
            _enemy = _enemiesPool.GetClosest(_position);
            _enemyUpdateTimer = _enemyUpdateInterval;
        }

        UpdateFacingDirection();
    }

    private void UpdateFacingDirection()
    {
        if (_enemy)
        {
            _scaleX = (_enemy.position.x > _position.x) ? 1 : -1;
            Vector3 localScale = _transform.localScale;
            localScale.x = _scaleX * Mathf.Abs(localScale.x);
            _transform.localScale = localScale;
        }
    }

    private void HandleMovement()
    {
        if (_enemy == null) return;

        Vector2 toEnemy = (Vector2)_enemy.position - _position;
        float sqrDistanceToEnemy = toEnemy.sqrMagnitude;
        _shouldFly = ShouldFly();

        Vector2 targetVelocity;
        if (sqrDistanceToEnemy < _minCombatDistanceSqr)
        {
            _moveDirection = -toEnemy.normalized;
            targetVelocity = _moveDirection * _baseSpeed;
        }
        else
        {
            targetVelocity = FollowPath();
        }

        _currentVelocity = Vector2.SmoothDamp(_currentVelocity, targetVelocity, ref _avoidanceVelocity, _movementSmoothTime);
        _rigidbody.velocity = _currentVelocity;

        if (_shouldFly)
        {
            Fly();
        }
        else if (!_isGrounded)
        {
            _rigidbody.AddForce(Vector2.down * _landingForce);
        }
    }

    private void Fly()
    {
        if (_position.y < _maxFlyHeight)
        {
            _rigidbody.AddForce(Vector2.up * _flyForce);
        }
    }

    private bool ShouldFly()
    {
        if (_path == null || _currentPathIndex >= _path.Count) return false;

        Node targetNode = _path[_currentPathIndex];
        float heightDifference = targetNode.Position.y - _position.y;

        return heightDifference > _groundCheckDistance * 2;
    }

    private Vector2 FollowPath()
    {
        if (_path == null || _currentPathIndex >= _path.Count)
        {
            return _enemy != null ? ((Vector2)_enemy.position - _position).normalized * _baseSpeed : Vector2.zero;
        }

        Vector2 targetPosition = _path[_currentPathIndex].Position;
        Vector2 direction = (targetPosition - _position).normalized;

        if (Vector2.SqrMagnitude(_position - targetPosition) < 0.25f)
        {
            _currentPathIndex++;
        }

        return direction * _baseSpeed;
    }

    private void UpdateNearbyColliders()
    {
        _colliderCacheTimer -= Time.fixedDeltaTime;
        if (_colliderCacheTimer <= 0)
        {
            _cachedNearbyColliders.Clear();
            _cachedNearbyColliders.AddRange(Physics2D.OverlapCircleAll(_position, _teammateAvoidanceRadius, _teammateLayer));
            _colliderCacheTimer = _colliderCacheTime;
        }
    }

    private void HandleTeammateAvoidance()
    {
        UpdateNearbyColliders();
        Vector2 avoidanceForce = Vector2.zero;

        foreach (Collider2D collider in _cachedNearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;

            Bounds bounds = GetColliderBounds(collider);
            Vector2 closestPoint = bounds.ClosestPoint(_position);
            Vector2 avoidDirection = (_position - closestPoint).normalized;
            float distance = Vector2.Distance(_position, closestPoint);

            if (distance < _teammateAvoidanceRadius)
            {
                float avoidanceStrength = 1 - (distance / _teammateAvoidanceRadius);
                avoidanceForce += avoidDirection * avoidanceStrength;
            }
        }

        if (avoidanceForce.sqrMagnitude > 0)
        {
            _smoothedAvoidanceForce = Vector2.SmoothDamp(_smoothedAvoidanceForce, avoidanceForce.normalized * _damperSpeed, ref _avoidanceVelocity, _avoidanceSmoothTime);
            _rigidbody.AddForce(_smoothedAvoidanceForce);
        }
    }

    private Bounds GetColliderBounds(Collider2D collider)
    {
        return collider.bounds;
    }

    private void OnDisable()
    {
        // Удалить эту строку
        // _avoidanceJobHandle.Complete();
    }

    private IEnumerator UpdatePathRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_pathUpdateInterval);
        while (true)
        {
            if (_enemy != null)
            {
                _path = _grid.SetPoints(_position, _enemy.position);
                _currentPathIndex = 0;
            }
            yield return wait;
        }
    }
    
}