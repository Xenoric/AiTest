using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class F3DProjectile : F3DGenericProjectile
{
    private int _counter;
    private TrailRenderer _trailRenderer;
    private Material _trailMaterial;

    // 
    public float RicochetChance;

    public float TrailFadeTime;
    public Vector2 TrailLifeTime;
    public Vector2 TrailEndWidth;

    public override void Awake()
    {
        base.Awake();

        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        if (_trailRenderer)
        {
            // Randomize Trail
            _trailMaterial = _trailRenderer.material;
            var texOffset = _trailMaterial.mainTextureOffset;
            texOffset.x -= Random.Range(-25f, 25f);
            _trailMaterial.mainTextureOffset = texOffset;
            _trailRenderer.time = Random.Range(TrailLifeTime.x, TrailLifeTime.y);
            _trailRenderer.endWidth = Random.Range(TrailEndWidth.x, TrailEndWidth.y);

            // URP Check
            if(_trailMaterial.HasProperty("_Color"))
                _trailColor = _trailMaterial.GetColor("_Color");
            else
                _trailColor = _trailMaterial.GetColor("_TintColor");

            _trailColor *= Random.Range(0.5f, 1f);
            TrailFadeTime = Random.Range(1f, 2f);
        }
    }

    // Use this for initialization
    private void Start()
    {
    }

    private void Update()
    {
        if (_trailRenderer)
        {
            // Fade Trail
            _trailColor = Color.Lerp(_trailColor, Color.clear, Time.deltaTime * TrailFadeTime);
            _trailMaterial.SetColor("_TintColor", _trailColor);
        }
    }

    private Color _trailColor;

    // Update is called once per frame
    private void FixedUpdate()
    {
        // Skip FixedUpdate ticks before showing the sprite to avoid passing through objects
//        if (_counter > SkipFixedFrames && !_sprite.enabled)
//            _sprite.enabled = true;
//        else
//            _counter++;
    }
}