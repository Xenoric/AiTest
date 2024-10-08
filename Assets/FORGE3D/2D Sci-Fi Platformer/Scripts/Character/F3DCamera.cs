using System;
using UnityEngine;
using System.Collections;

public class F3DCamera : MonoBehaviour
{
    public Transform Character;
    public float LagRate;
    public float VerticalOffset;
    public float MinHeigth;

    public Vector2 AdvanceVelocity;
    public float AdvanceSmooth;
    
    //
    private Rigidbody2D _characterRb2D;
    private Vector3 _cameraDesiredPos;
    private Vector3 _charVelocitySmoothed;

    // PARALLAX 
    [Serializable]
    public class Parallax
    {
        public Transform Object;
        public Vector2 Rate;
    }

    public Parallax[] ParallaxObjects;

    //
    private float lastPos;

    // Use this for initialization
    private void Awake()
    {
        Application.targetFrameRate = 50;
        if (Character == null) return;
        _characterRb2D = Character.GetComponent<Rigidbody2D>();
       

        // Hard follow
        _cameraDesiredPos = Character.position;
        _cameraDesiredPos.z = -10;
//        _cameraDesiredPos.y += VerticalOffset;
        _cameraDesiredPos.y = 0;
        var pos = _cameraDesiredPos;
        if (pos.y < MinHeigth)
            pos.y = MinHeigth;
        transform.position = pos;
      
    }

    private void Start() { }
   

    // Update is called once per frame
    private void LateUpdate()
    {

        if (Character == null) return;

        // Follow character
        var posDelta = Follow();

        // Parallax
        if (ParallaxObjects != null && ParallaxObjects.Length > 0)
            for (var i = 0; i < ParallaxObjects.Length; i++)
            {
                var obj = ParallaxObjects[i];

                var _posDelta = posDelta;
                _posDelta.x *= obj.Rate.x;
                _posDelta.y *= obj.Rate.y;
                obj.Object.position -= _posDelta;
            }
    }



    private Vector3 Follow()
    {
        var charVel = new Vector3(_characterRb2D.velocity.x * AdvanceVelocity.x, _characterRb2D.velocity.y * AdvanceVelocity.y, 0);
        _charVelocitySmoothed = Vector2.Lerp(_charVelocitySmoothed, charVel, Time.deltaTime * AdvanceSmooth);

        _cameraDesiredPos = Character.position + _charVelocitySmoothed;
        _cameraDesiredPos.z = -10;
        _cameraDesiredPos.y += VerticalOffset;
        var pos = Vector3.Lerp(transform.position, _cameraDesiredPos, Time.deltaTime * LagRate);
        if (pos.y < MinHeigth)
            pos.y = MinHeigth;
        var posDelta = transform.position - pos;
        posDelta.z = 0;
        transform.position = pos;
        return posDelta;
    }

  
}