using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private readonly Collider2D[] _teammateResults = new Collider2D[10];
    private JobHandle _avoidanceJobHandle;
    private Vector2 _currentVelocity;
    private Vector2 _smoothedAvoidanceForce;
    private Vector2 _avoidanceVelocity;
    
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

    private async void Start()
    {
        await UpdatePathRoutineAsync();
    }

    private void OnDisable()
    {
        _avoidanceJobHandle.Complete();
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
        RaycastHit2D hit = Physics2D.Raycast(_position, Vector2.down, _groundCheckDistance, _groundLayer);
        _isGrounded = hit.collider != null;

        if (!_isGrounded && hit.collider != null)
        {
            _rigidbody.AddForce(Vector2.down * _landingForce);
        }
    }

    private void UpdateEnemyTracking()
    {
        if (_enemy == null)
        {
            _enemy = _enemiesPool.GetClosest(_position);
            return;
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
        else if (sqrDistanceToEnemy > _optimalCombatDistanceSqr)
        {
            targetVelocity = FollowPath();
        }
        else
        {
            targetVelocity = Vector2.zero;
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
        if (_path == null || _currentPathIndex >= _path.Count) return Vector2.zero;

        Vector2 targetPosition = _path[_currentPathIndex].Position;
        Vector2 direction = (targetPosition - _position).normalized;

        if (Vector2.SqrMagnitude(_position - targetPosition) < 0.25f)
        {
            _currentPathIndex++;
        }

        return direction * _baseSpeed;
    }

    private void HandleTeammateAvoidance()
    {
        int numColliders = Physics2D.OverlapCircleNonAlloc(_position, 
            _teammateAvoidanceRadius, _teammateResults, _teammateLayer);

        AvoidanceJob job = new AvoidanceJob
        {
            Position = new float3(_position.x, _position.y, 0),
            TeammatePositions = new NativeArray<float3>(numColliders, Allocator.TempJob),
            AvoidanceRadius = _teammateAvoidanceRadius,
            AvoidanceForce = new NativeArray<float3>(1, Allocator.TempJob)
        };

        for (int i = 0; i < numColliders; i++)
        {
            if (_teammateResults[i].gameObject == gameObject) continue;
            Vector2 teammatePos = _teammateResults[i].transform.position;
            job.TeammatePositions[i] = new float3(teammatePos.x, teammatePos.y, 0);
        }

        _avoidanceJobHandle = job.Schedule();
        _avoidanceJobHandle.Complete();

        float3 avoidanceForce3 = job.AvoidanceForce[0];
        Vector2 avoidanceForce = new Vector2(avoidanceForce3.x, avoidanceForce3.y);

        if (avoidanceForce.sqrMagnitude > 0)
        {
            _smoothedAvoidanceForce = Vector2.SmoothDamp(_smoothedAvoidanceForce, avoidanceForce.normalized * _damperSpeed, ref _avoidanceVelocity, _avoidanceSmoothTime);
            _rigidbody.AddForce(_smoothedAvoidanceForce);
        }

        job.TeammatePositions.Dispose();
        job.AvoidanceForce.Dispose();
    }

    [BurstCompile]
    private struct AvoidanceJob : IJob
    {
        public float3 Position;
        public NativeArray<float3> TeammatePositions;
        public float AvoidanceRadius;
        public NativeArray<float3> AvoidanceForce;

        public void Execute()
        {
            float3 totalForce = float3.zero;

            for (int i = 0; i < TeammatePositions.Length; i++)
            {
                float3 toTeammate = Position - TeammatePositions[i];
                float sqrDistance = math.lengthsq(toTeammate);
                if (sqrDistance > 0 && sqrDistance < AvoidanceRadius * AvoidanceRadius)
                {
                    float3 avoidDirection = math.normalize(toTeammate);
                    float avoidanceStrength = 1 - (math.sqrt(sqrDistance) / AvoidanceRadius);
                    totalForce += avoidDirection * avoidanceStrength;
                }
            }

            AvoidanceForce[0] = totalForce;
        }
    }

    private async Task UpdatePathRoutineAsync()
    {
        while (true)
        {
            await Task.Delay((int)(_pathUpdateInterval * 1000));
            UpdatePath();
        }
    }

    private void UpdatePath()
    {
        if (_enemy == null) return;

        _path = _grid.SetPoints(_position, _enemy.position);
        _currentPathIndex = 0;
    }
}