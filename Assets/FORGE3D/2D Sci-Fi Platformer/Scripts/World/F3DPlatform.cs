using UnityEngine;
using System.Collections;

public class F3DPlatform : MonoBehaviour
{
    public Transform[] Waypoints;
    public float Speed;
    public float ArrivalDistance;
    public float DelayTime;
    private Vector3[] _waypoints;

    //
    private int _currentWaypoint;

    private Rigidbody2D _rBody2d;

    private void Awake()
    {
        _rBody2d = GetComponent<Rigidbody2D>();

        // Stores initial waypoint transform position since its a child of the platform
        if (Waypoints == null || Waypoints.Length <= 1) return;
        _waypoints = new Vector3[Waypoints.Length];
        for (var i = 0; i < Waypoints.Length; i++)
            _waypoints[i] = Waypoints[i].position;
    }

    private bool _delayed;
    public Vector2 Velocity;

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Idle until flag resets
        if (_delayed) return;

        // Check distance and switch to the next waypoint
        if (_waypoints == null || _waypoints.Length <= 1) return;
        if (Vector3.Distance(transform.position, _waypoints[_currentWaypoint]) < ArrivalDistance)
        {
            _currentWaypoint++;
            if (_currentWaypoint > _waypoints.Length - 1)
                _currentWaypoint = 0;

            // Delay
            if (DelayTime > 0)
            {
                StartCoroutine(Delay(DelayTime));
                return;
            }
        }

        //
        var curWaypoint = new Vector2(_waypoints[_currentWaypoint].x, _waypoints[_currentWaypoint].y);
        Velocity = Vector2.ClampMagnitude(curWaypoint - _rBody2d.position, Speed);
        _rBody2d.MovePosition(_rBody2d.position + Velocity * Time.deltaTime);
    }

    private IEnumerator Delay(float delay)
    {
        _delayed = true;
        Velocity = Vector2.zero;
        yield return new WaitForSeconds(delay);
        _delayed = false;
    }
}