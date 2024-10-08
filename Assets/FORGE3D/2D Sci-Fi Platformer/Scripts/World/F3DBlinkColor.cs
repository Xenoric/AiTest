using System;
using UnityEngine;
using System.Collections;

public class F3DBlinkColor : MonoBehaviour
{

    public Color ColorA, ColorB;
    public float Rate;

    //
    private SpriteRenderer _sprite;

    private float _time;

    void Awake()
	{
	    _sprite = GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update ()
	{
	    if (!_sprite) return;
	    _time += Time.deltaTime;
	    _sprite.color = Color.Lerp(ColorA, ColorB, Mathf.Abs(Mathf.Sin(_time * Rate)));

	}
}
