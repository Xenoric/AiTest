using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class F3DPulse : F3DGenericProjectile
{
    public float MaxLength;
    public float Tiling;
    public LayerMask LineCastMask;

    public float FadeRate;
    //
    private LineRenderer _lineRenderer;

    private Material _lineMaterial;
    private Vector3[] _linePoints = new Vector3[2];
    private bool _alreadyHit;

    public override void Awake()
    {
        base.Awake();
        _lineRenderer = GetComponent<LineRenderer>();
        _lineMaterial = _lineRenderer.material;        
        _lineRenderer.positionCount = 2;

        // Randomize Line Texture Offset
        var texOffset = _lineMaterial.mainTextureOffset;
        texOffset.x -= Random.Range(-1f, 1f);
        _lineMaterial.mainTextureOffset = texOffset;
                
        _lineRenderer.startColor = Color.white;
        _lineRenderer.endColor = Color.white;
    }

    private void Start()
    {
        F3DSpawner.Despawn(transform, DelayDespawn);
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        SetLinePoints();
    }

    private Color _lerpColor = Color.white;

    private void SetLinePoints()
    {


        if (_alreadyHit)
        {
            _lineRenderer.startColor = _lerpColor;
            _lineRenderer.endColor = _lerpColor;
        }

        if (_lineRenderer == null) return;
        if (_lineMaterial == null) return;

        // Set the initial line point
        _linePoints[0] = transform.position;
        _linePoints[0].z = 0;

        // Direction
        var direction = Mathf.Sign(transform.parent.lossyScale.x);

        // Linecast
        var farPoint = transform.position + transform.right * MaxLength * direction;
        var lineHit = Physics2D.Linecast(transform.position, farPoint, LineCastMask);
        if (lineHit.collider != null)
        {
            // Set end point
            _linePoints[1] = lineHit.point;
            _linePoints[1].z = 0;

//            // Align the midpoint
//            _linePoints[1] = transform.position +
//                             (new Vector3(lineHit.point.x, lineHit.point.y, 0) - transform.position) * 0.5f;
//            _linePoints[1].z = 0;

            // Damage and Hit Effects
            if (Hit && !_alreadyHit)
            {
                DealDamage(5, WeaponType, lineHit.transform, Hit, HitLifeTime, lineHit.point, lineHit.normal);
                _alreadyHit = true;

                // Shift the AudioSource attached to this pulse to the lineEnd position
                if (Audio.transform != this.transform)
                    Audio.transform.position = lineHit.point;

                // Play hit sound
                F3DWeaponAudio.OnProjectileImpact(Audio, AudioInfo);
              
               
            }
        }
        else // Line hits nothing
        {
            _alreadyHit = true;

            // Midpoint
            _linePoints[1] = farPoint;// + (farPoint - transform.position) * 0.3f;
            _linePoints[1].z = 0;

            //            // Endpoint offset along to fade out smoothly
            //            _linePoints[2] = farPoint;// + (farPoint - transform.position) * 0.5f;
            //            _linePoints[2].z = 0;

            _lineRenderer.startColor = _lerpColor;
            _lineRenderer.endColor = Color.clear;
        }
        _lineRenderer.SetPositions(_linePoints);

        // Kepp tiling over distance
        var texScale = _lineMaterial.mainTextureScale;
        texScale.x = Tiling * Vector3.Distance(_linePoints[0], _linePoints[1]);
        _lineMaterial.mainTextureScale = texScale;

        // Animate Offset
        var texOffset = _lineMaterial.mainTextureOffset;
        texOffset.x -= Time.deltaTime * 12;
        _lineMaterial.mainTextureOffset = texOffset;

        _lerpColor = Color.Lerp(_lerpColor, Color.clear, Time.deltaTime * FadeRate);
    }
}