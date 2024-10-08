using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class F3DWire : MonoBehaviour
{
    // Segments path before smoothing
    public Transform[] WireSegments;

    public float PathSmoothRate;

    // Wire Layer
    public int Layer;

    // Rigidbody 
    public float Drag;

    public float AngularDrag;
    public float Mass;
    public float GravityScale;
    public bool EnableCollisions;
    public float ColliderRadius;

    // Anchor offset
    public Vector2 AnchorOffset;

    // Limits
    public bool UseLimits;

    public float LowLimit;
    public float HighLimit;

    //
    private bool _wire;

    private const string _jointName = "Joint_";
    private LineRenderer _line;
    private Vector3[] _curve;
    private Vector3[] _wirePosition;
    private GameObject[] _wireJoint;
    public string WireSortingLayer;
    public int WireSortingIndex;

    private void Awake()
    {
        _line = gameObject.GetComponent<LineRenderer>();
        _line.sortingLayerID = SortingLayer.NameToID(WireSortingLayer);
        _line.sortingOrder = WireSortingIndex;
        BuildWire();
    }

    // Update
    private void Update()
    {
        if (_wire && Input.GetKeyDown("u"))
            DestroyWire();
        if (!_wire && Input.GetKeyDown("i"))
            BuildWire();
    }

    // LateUpdate
    private void LateUpdate()
    {
        if (_wire)
        {
            for (var i = 0; i < _curve.Length; i++)
                _line.SetPosition(i, _wireJoint[i].transform.position);
            _line.enabled = true;
        }
        else
            _line.enabled = false;
    }

    // 
    private void BuildWire()
    {
        // Build smooth path
        var points = new List<Vector3> {transform.position};
        points.AddRange(WireSegments.Select(t => t.position));
        _curve = MakeSmoothCurve(points.ToArray(), PathSmoothRate);

        // Init arrays
        _line.positionCount = _curve.Length;
        _wirePosition = new Vector3[_curve.Length];
        _wireJoint = new GameObject[_curve.Length];

        // Build joints from curve
        for (var i = 0; i < _curve.Length; i++)
        {
            _wirePosition[i] = _curve[i];
            CreateJoint(i);
        }
        _wire = true;
    }

    //
    private void CreateJoint(int n)
    {
        _wireJoint[n] = new GameObject(_jointName + n) {layer = Layer};
        _wireJoint[n].transform.parent = transform;

        // Collider
        var col = _wireJoint[n].AddComponent<CircleCollider2D>();
        col.radius = ColliderRadius;

        // Rigidbody
        var rigid = _wireJoint[n].AddComponent<Rigidbody2D>();
        rigid.interpolation = RigidbodyInterpolation2D.None;
        rigid.gravityScale = GravityScale;
        rigid.drag = Drag;
        rigid.mass = Mass;

        //
        var joint = _wireJoint[n].AddComponent<HingeJoint2D>();

        // Limits
        var limits = joint.limits;
        limits.min = LowLimit;
        limits.max = HighLimit;
        joint.limits = limits;
        joint.useLimits = UseLimits;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = AnchorOffset;
        joint.connectedAnchor = -AnchorOffset;
        joint.enableCollision = EnableCollisions;

        // 
        _wireJoint[n].transform.position = _wirePosition[n];

        // Add connected targets
        if (n == 0)
        {
            // Add new Starting position under the base transform
            var wireStart = new GameObject("WireStart") {layer = Layer};
            wireStart.transform.position = transform.position;
            wireStart.transform.rotation = transform.rotation;
            wireStart.transform.parent = transform;
            var rb = wireStart.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.gravityScale = GravityScale;
            rb.drag = Drag;
            rb.mass = Mass;

            // First joint will connect to the new start object
            joint.connectedBody = rb;

            // 
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = Vector3.zero;
        }
        else if (n == _curve.Length - 1)
        {
            joint.connectedBody = _wireJoint[Mathf.Max(0, n - 1)].GetComponent<Rigidbody2D>();

            // Add second end connecting joint
            endJoint = _wireJoint[n].AddComponent<HingeJoint2D>();
            endJoint.limits = limits;
            endJoint.useLimits = UseLimits;
            endJoint.autoConfigureConnectedAnchor = false;
            endJoint.anchor = Vector3.zero;
            endJoint.connectedAnchor = Vector3.zero;
            endJoint.connectedBody = WireSegments[WireSegments.Length - 1].GetComponent<Rigidbody2D>();
            endJoint.enableCollision = EnableCollisions;
        }
        else
        {
            joint.connectedBody = _wireJoint[Mathf.Max(0, n - 1)].GetComponent<Rigidbody2D>();
        }
    }

    private HingeJoint2D endJoint;

    //
    private void DestroyWire()
    {
        _wire = false;
        for (var i = 0; i < _wireJoint.Length; i++)
            Destroy(_wireJoint[i]);
        Destroy(endJoint);
        _wirePosition = new Vector3[0];
        _wireJoint = new GameObject[0];
    }

    //
    private static Vector3[] MakeSmoothCurve(ICollection<Vector3> arrayToCurve, float smoothness)
    {
        if (smoothness < 1.0f) smoothness = 1.0f;
        var pointsLength = arrayToCurve.Count;
        var curvedLength = pointsLength * Mathf.RoundToInt(smoothness) - 1;
        var curvedPoints = new List<Vector3>(curvedLength);
        for (var pointInTimeOnCurve = 0; pointInTimeOnCurve < curvedLength + 1; pointInTimeOnCurve++)
        {
            var t = Mathf.InverseLerp(0, curvedLength, pointInTimeOnCurve);
            var points = new List<Vector3>(arrayToCurve);
            for (var j = pointsLength - 1; j > 0; j--)
            {
                for (var i = 0; i < j; i++)
                {
                    points[i] = (1 - t) * points[i] + t * points[i + 1];
                }
            }
            curvedPoints.Add(points[0]);
        }
        return (curvedPoints.ToArray());
    }
}