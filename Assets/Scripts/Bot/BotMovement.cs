using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Scripts.Bot.Grid;
using UnityEngine;

namespace Scripts.Bot
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BotMovement : MonoBehaviour
    {
        public Rigidbody2D Rigidbody { get; private set; }
        public float EnemyDistance
        {
            get
            {
                if (_enemy)
                    return Vector3.Distance(transform.position, _enemy.position);
                return Mathf.Infinity;
            }
        }
        public int ScaleX { get; private set; }
        [Header("Parameters")]
        [SerializeField] private float _speed;
        [SerializeField] private float _damperSpeed;
        [SerializeField] private float _thrust;
        [SerializeField] private float _nodeDistance;
        [SerializeField, Range(0.25f, 3f)] private float _timeToUpdatePath;
        [SerializeField] private float _yAssumption;
        [SerializeField, Range(1, 7)] private int _pathNodexAssumption;
        [SerializeField] private float _avoidanceDistance;
        [SerializeField] private float _teammateAvoidanceDistance;
        [Space] 
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private LayerMask _teammateLayer;
        [SerializeField] private Grid.Grid _grid;
        [SerializeField] private Transform _enemy;
        [SerializeField] private EnemiesPool _enemiesPool;
        [SerializeField] private bool _debug;
        private int _startScaleX;
        public Transform Enemy => _enemy;
        private Vector3 _target;
        private List<Node> _path;
        private int _nodeIndex;

        private void GetNextNode()
        {
            if (_path == null)
                return;

            if (_nodeIndex + 1 >= _path.Count)
            {
                return;
            }

            _nodeIndex++;
            _target = _path[_nodeIndex].Position;
        }
        
        private void GetPath()
        {
            if (!_enemy)
                return;
            _path = _grid.SetPoints(transform.position, _enemy.position);
            //_entitiesTracer.Trace(this);
            if (_path == null || _path.Count == 0)
                return;
            _target = _path[0].Position;
            _nodeIndex = 0;
        }
        
        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody2D>();
            _startScaleX = (int)transform.localScale.x;
        }

        private void Start()
        {
            StartCoroutine(ActualizePath());
        }

        private void FixedUpdate()
        {
            CheckTeammate();
            if(_enemy)
                SetScale();
            if (!_enemy || _path != null && Vector3.Distance(transform.position, _enemy.position) <= _avoidanceDistance && _path.Count < _pathNodexAssumption)
            {
                if(_path != null)
                    Rigidbody.AddForce(new Vector2(-Rigidbody.velocity.normalized.x, 0) * _damperSpeed);
                return;
            }
            
            if(CheckIfReachedNode())
                GetNextNode();

            var move = (_target - transform.position).normalized;

            move.x *= _speed;
            move.y *= _thrust;
            Rigidbody.AddForce(move);
        }

        private void SetScale()
        {
            ScaleX = (_enemy.position - transform.position).x > 0 ? 1 : -1;
            ScaleX *= _startScaleX;
                
            transform.localScale = new Vector3(ScaleX, 1, 1);
        }

        private void CheckTeammate()
        {
            var teammate = Physics2D.OverlapCircle(transform.position, _teammateAvoidanceDistance, _teammateLayer);

            if (!teammate)
                return;

            var move = (transform.position - teammate.transform.position).normalized;
            Rigidbody.AddForce(new Vector2(move.x, 0) * (_speed * 2));
        }

        private float CheckRoof()
        {
            var hit = Physics2D.Raycast(transform.position, transform.up, Mathf.Infinity, _obstacleLayer);
            var distance = Vector3.Distance(transform.position, hit.point);
            var signY = Mathf.Sign((new Vector3(hit.point.x, hit.point.y) - transform.position).y);
            return distance * signY;
        }

        private bool CheckIfReachedNode()
        {
            var distance = _target - transform.position;
            var yAssumption = CheckRoof() < _yAssumption ? _nodeDistance : _yAssumption;
            var hasReached = Mathf.Abs(distance.x) <= _nodeDistance && Mathf.Abs(distance.y) <= yAssumption;
            return hasReached;
        }

        private IEnumerator ActualizePath()
        {
            while(true)
            {
                _enemy = _enemiesPool.GetClosest(transform.position);
                GetPath();
                
                yield return new WaitForSeconds(_timeToUpdatePath);
            }
        }

        private void OnDrawGizmos()
        {
            if (_path == null || _path.Count == 0 || !_debug)
                return;

            foreach (var node in _path)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(node.Position, 1.75f);
            }
        }
    }
}