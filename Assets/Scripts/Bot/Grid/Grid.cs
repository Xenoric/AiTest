using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Bot.Grid
{
    public class Grid : MonoBehaviour
    {
        [Header("Parameters")] 
        [SerializeField] private LayerMask _obstacle;
        [SerializeField] private Transform _leftBottomAnchor;
        [SerializeField] private Transform _rightTopAnchor;
        [SerializeField, Range(0.1f, 5)] private float _nodeSize;
        [SerializeField] private Node[,] _nodes;
        [SerializeField] private float _nodePrecision;
        [SerializeField] private EntitiesTracer _entitiesTracer;
        [SerializeField] private bool _debug;
        public EntitiesTracer EntitiesTracer => _entitiesTracer;
        public List<Node> Path { get; set; }
        private Pathfinding _pathfinding;

        public Vector2 Size => (_rightTopAnchor.position - _leftBottomAnchor.position) / _nodeSize;

        public List<Node> GetNeighbours(Node node)
        {
            var neigbours = new List<Node>();
            var nodePosition = new Vector2(node.GridX, node.GridY);
            
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                var position = new Vector2(x, y);
                var neighbourPosition = nodePosition + position;

                if (!CheckInBounds(neighbourPosition))
                    continue;
                var neighbourNode = _nodes[(int)neighbourPosition.x, (int)neighbourPosition.y];
                if (neighbourNode == null)
                    continue;
                neigbours.Add(neighbourNode);
            }

            return neigbours;
        }

        public List<Node> SetPoints(Vector2 startPosition, Vector2 targetPosition)
        {
            var startNode = TryConvertWorldPointToNode(startPosition);
            var targetNode = TryConvertWorldPointToNode(targetPosition);
            
            var path = _pathfinding.FindPath(startNode, targetNode);

            Path = path;
            return path;
        }

        public Vector2Int WorldToGridPoint(Vector3 worldPoint)
        {
            var gridScale = (worldPoint - _leftBottomAnchor.position) / _nodeSize;
            return new Vector2Int(Mathf.RoundToInt(gridScale.x), Mathf.RoundToInt(gridScale.y));
        }

        public Node TryConvertWorldPointToNode(Vector3 worldPoint)
        {
            var gridPosition = WorldToGridPoint(worldPoint);

            if (!CheckInBounds(gridPosition))
                return null;

            return _nodes[gridPosition.x, gridPosition.y];
        }

        private bool CheckInBounds(Vector2 position) => position is { x: >= 0, y: >= 0 } &&
                                                        position.x < _nodes.GetLength(0) &&
                                                        position.y < _nodes.GetLength(1);

        private void Awake()
        {
            _pathfinding = new Pathfinding(this);

            var doubledNodeSize = Mathf.Pow(_nodeSize, 2);
            var size = (_rightTopAnchor.position - _leftBottomAnchor.position) * _nodeSize;
            _nodes = new Node[(int)(size.x / doubledNodeSize) + 1, (int)(size.y / doubledNodeSize) + 1];
            
            Debug.Log($"{_nodes.GetLength(0)}  {_nodes.GetLength(1)}");
            /*for (float x = _leftBottomAnchor.position.x; x < _rightTopAnchor.position.x; x += _nodeSize)
            for (float y = _leftBottomAnchor.position.y; y < _rightTopAnchor.position.y; y += _nodeSize)*/
            for (int x = 0; x < size.x / doubledNodeSize; x++)
            for (int y = 0; y < size.y / doubledNodeSize; y++)
            {
                var elementPosition = new Vector3(x, y, 0);
                var nodePosition = _leftBottomAnchor.position + elementPosition * _nodeSize;
                
                _nodes[x, y] = 
                    new Node(!Physics2D.OverlapCircle(nodePosition, _nodeSize / _nodePrecision, _obstacle),
                        nodePosition, x, y);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_debug)
                return;
            if (_nodes == null)
                return;
            foreach (var node in _nodes)
            {
                Gizmos.color = node.IsWalkable ? Color.green : Color.red;
                Gizmos.DrawSphere(node.Position, _nodeSize / 2);
            }
            
            /*if (Path == null)
                return;
            foreach (var node in Path)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(node.Position, _nodeSize / 2);
            }*/
        }
    }
}