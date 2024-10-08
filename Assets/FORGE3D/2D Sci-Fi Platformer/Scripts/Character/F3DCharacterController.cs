using UnityEngine;
using System.Collections;

public class F3DCharacterController : MonoBehaviour
{
    // Components
    public Animator Character;

    public Transform SideCheck;
    public Transform WeaponSocket;
    public Transform ShadowL;
    public Transform ShadowR;
    public SpriteRenderer Shadow;
    public float ShadowCheckDist;
    public float ShadowMinAngle;
    public float ShadowDrawDist;

    // Settings
    public LayerMask Ground;

    public LayerMask ShadowLayerMask;
    public float MaxVelocityX;
    public float MaxVelocityY;
    public float MoveForce = 365f;
    public float MaxSpeed = 1f;
    public float CrouchSpeed = 0.75f;
    public float MaxSpeedBackwards = 0.5f;
    public float MaxSpeedFade = 1f;
    public float JumpForce = 1000f;
    public float DoubleJumpForce;
    public float BreakForce = 0.05f;
    public float GroundCheckCircleSize;
    public float SideCheckDist;
    public float AimTime;

    //
    private Rigidbody2D _rb2D;

    private F3DPlatform _platform;
    private F3DWeaponController _weaponController;
    private F3DCharacter _character;
    private F3DCharacterAudio _audio;

    //
    private bool _facingRight = true;

    private float _hClamp;
    private bool _jump;
    private bool _doubleJump;
    private bool _crouch;
    private bool _grounded;
    private bool _lastGroundedState;
    private bool _sideObstacle;
    private float _speed;
    private float _horizontal;
    private float _horizontalSignLast;

    //
    private Vector3 _shadowBaseScale;

    // DEBUG GIZMOS
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, GroundCheckCircleSize);
    }

    // Use this for initialization
    private void Awake()
    {
        _character = GetComponent<F3DCharacter>();
        _weaponController = GetComponent<F3DWeaponController>();
        _rb2D = GetComponent<Rigidbody2D>();
        _audio = GetComponent<F3DCharacterAudio>();
        _speed = MaxSpeed;
        if (Shadow != null)
            _shadowBaseScale = Shadow.transform.localScale;
    }

    private float _lastShadowDrawDist;
    private Color _lastShadowAlpha;

    private void DrawShadow()
    {
        if (Shadow == null) return;
        if (ShadowL == null) return;
        if (ShadowR == null) return;
        if (_character.Health <= 0)
            return;

        // Linecast each side
        var _left = Physics2D.Linecast(ShadowL.position, ShadowL.position + Vector3.down * ShadowCheckDist,
            ShadowLayerMask);
        var _right = Physics2D.Linecast(ShadowR.position, ShadowR.position + Vector3.down * ShadowCheckDist,
            ShadowLayerMask);

     

        // Move the shadow
        var midPos = Physics2D.Linecast(transform.position, transform.position + Vector3.down * ShadowDrawDist,
            ShadowLayerMask);
        var dist = Mathf.Clamp01(midPos.distance * (1f / (ShadowDrawDist)));

      

        // Flying?
        if (!midPos)
        {
            Shadow.transform.localScale = Vector3.zero;
            _lastShadowDrawDist = 1;
            return;
        }
        var shadowColor = Shadow.color;

        //
        if ((_left && !_right) || (!_left && _right))
        {
            _lastShadowDrawDist = Mathf.Lerp(_lastShadowDrawDist, 1f, Time.deltaTime * 10);
            _lastShadowAlpha = Color.Lerp(_lastShadowAlpha, Color.clear, Time.deltaTime * 10);
            shadowColor = _lastShadowAlpha;
        }
        else
        {
            shadowColor = Color.white;
            shadowColor.a = 1 - dist;
            _lastShadowAlpha = shadowColor;
            _lastShadowDrawDist = dist;
        }

        //
        Shadow.color = shadowColor;
        Shadow.transform.localScale = Vector3.Lerp(_shadowBaseScale, Vector3.zero, _lastShadowDrawDist);
        Shadow.transform.position = midPos.point;

        // Check angle or stop drawing
        if (Vector2.Dot(midPos.normal, Vector3.up) >= ShadowMinAngle)
            Shadow.transform.rotation = Quaternion.LookRotation(Vector3.forward, midPos.normal);
        else
        {
            Shadow.transform.rotation = Quaternion.identity;
            Shadow.transform.localScale = Vector3.zero;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        // Debug Teleport
        if (Input.GetKeyDown(KeyCode.T))
        {
            var cursorWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            cursorWorldPoint.z = transform.position.z;
            transform.position = cursorWorldPoint;
        }

        // Check and apply Player Input
        _horizontal = Input.GetAxis("Horizontal");
        if (_horizontal > 0)
            _hClamp = 1f;
        else if (_horizontal < 0)
            _hClamp = -1f;
        else
            _hClamp = 0;
        Character.SetFloat("Horizontal", Mathf.Abs(_hClamp));
        _weaponController.SetFloat("Horizontal", Mathf.Abs(_hClamp));

      

        // Check Grounded
        var groundedCollider = Physics2D.OverlapCircle(transform.position, GroundCheckCircleSize, Ground);

        // Check active platform and handle the reference
        if (groundedCollider)
        {
            var onPlatform = groundedCollider.transform.gameObject.layer == LayerMask.NameToLayer("Platform");
            if (onPlatform)
                _platform = groundedCollider.GetComponent<F3DPlatform>();
        }
        else
            _platform = null;

        //
        _grounded = groundedCollider;
        if (_lastGroundedState != _grounded && _grounded)
        {
            _jump = false;
            _doubleJump = false;

            // Set the appropriate surface type in the character audio controller
            if (groundedCollider.CompareTag("Sand"))
                _audio.Surface = F3DCharacterAudio.SurfaceType.Sand;
            else if (groundedCollider.CompareTag("Metal"))
                _audio.Surface = F3DCharacterAudio.SurfaceType.Metal;
            else if (groundedCollider.CompareTag("Barrel"))
                _audio.Surface = F3DCharacterAudio.SurfaceType.Barrel;
            else
                _audio.Surface = F3DCharacterAudio.SurfaceType.None;

            // Play landing sound
            _audio.OnLand();
        }
        _lastGroundedState = _grounded;

        //
        Debug.DrawLine(transform.position, transform.position - Vector3.up * GroundCheckCircleSize);

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            if (!_jump && !_doubleJump && _grounded)
            {
                _rb2D.AddForce(new Vector2(0f, JumpForce), ForceMode2D.Impulse);
                _jump = true;
                _grounded = false;

                //
                _audio.OnJump();
            }
            else if (_jump && !_doubleJump)
            {
                _rb2D.AddForce(new Vector2(0f, DoubleJumpForce), ForceMode2D.Impulse);
                _doubleJump = true;
                _jump = false;

                //
                _audio.OnDoubleJump();
            }
        }

        // Crouch 
        // Exit crouch state on button up
        if (Input.GetButtonUp("Crouch"))
            _crouch = false;

        // Enter crouch on button hold, no jump
        if (Input.GetButton("Crouch") && !_jump && !_doubleJump && !_crouch)
            _crouch = true;

        // Jump cancels any current crouch state
        if (_jump || _doubleJump)
            _crouch = false;

        // Check Side Obstacle
        _sideObstacle = Physics2D.Linecast(SideCheck.position, SideCheck.position + Vector3.left * SideCheckDist,
            Ground);
        _sideObstacle = _sideObstacle || Physics2D.Linecast(SideCheck.position,
                            SideCheck.position + Vector3.right * SideCheckDist, Ground);

        // Set Crouch flag 
        Character.SetBool("Crouch", _crouch);
        _weaponController.SetBool("Crouch", _crouch);

        // Set Grounded flag
        Character.SetBool("Grounded", _grounded);
        _weaponController.SetBool("Grounded", _grounded);

        // Set Side Obstacle flag
        Character.SetBool("SideObstacle", _sideObstacle);
        _weaponController.SetBool("SideObstacle", _sideObstacle);

   

        ////////////////////////////////// FIRING
        if (WeaponSocket == null) return;

        // Get Mouse to World Position
        var aimPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        aimPos.z = 0;

        // Look direction
        var dir = (aimPos - WeaponSocket.position).normalized;
        dir.z = 0;

        // Weapon socket to FX Socket offset
        var currentWeapon = _weaponController.GetCurrentWeapon();
        if (currentWeapon.Type == F3DWeaponController.WeaponType.Melee)
        {
            WeaponSocket.rotation = Quaternion.identity;
        }
        else
        {
            var offset = currentWeapon.FXSocket.position - WeaponSocket.position;
            offset.z = 0;
            var localOffset = WeaponSocket.InverseTransformVector(offset);
            localOffset.x = 0;
            localOffset.z = 0;
            Debug.DrawLine(Vector3.zero, localOffset * transform.lossyScale.x, Color.yellow);

            //  Debug.DrawLine(WeaponSocket.position, currentWeapon.FXSocket.position, Color.yellow);
            var worldOffset = WeaponSocket.TransformVector(localOffset) - WeaponSocket.right * 5 * Mathf.Sign(dir.x);
            var weaponDir = (aimPos - (WeaponSocket.position + worldOffset)).normalized;
            var socketRotation = Quaternion.LookRotation(Vector3.forward,
                Mathf.Sign(dir.x) * Vector3.Cross(Vector3.forward, weaponDir));
            WeaponSocket.rotation = Quaternion.Lerp(WeaponSocket.rotation, socketRotation, Time.deltaTime * AimTime);

            // Lock Weapon Socket Angle
            var rot = WeaponSocket.rotation;
            const float z = 0.35f;
            if (_facingRight && WeaponSocket.rotation.z < -z)
            {
                rot.z = -z;
                WeaponSocket.rotation = rot;
            }
            else if (!_facingRight && WeaponSocket.rotation.z > z)
            {
                rot.z = z;
                WeaponSocket.rotation = rot;
            }

          
        }

        // Flip
        if (dir.x > 0 && !_facingRight)
            Flip();
        else if (dir.x < 0 && _facingRight)
            Flip();

        // Draw Velocity
        Debug.DrawLine(currentWeapon.FXSocket.position, aimPos, Color.blue);

        // Check Facing
        if (_hClamp > 0) _horizontalSignLast = 1f;
        else if (_hClamp < 0) _horizontalSignLast = -1f;
        var facingSing = (_facingRight ? 1f : -1f) * _horizontalSignLast;

        // Dampen Speed on Moving Backwards and Crouch
        var speedDamp = _crouch ? CrouchSpeed : MaxSpeed;
        speedDamp = facingSing >= 0 ? speedDamp : speedDamp * MaxSpeedBackwards;
        _speed = Mathf.Lerp(_speed, speedDamp, Time.deltaTime * MaxSpeedFade);

        // Set Animator Vars
        Character.SetFloat("facingRight", facingSing);
        _weaponController.SetFloat("facingRight", facingSing);

        //
        var platformVelocity = _rb2D.velocity;
        if (_platform != null)
        {
            platformVelocity = _rb2D.velocity;
            platformVelocity.x = platformVelocity.x - _platform.Velocity.x;
        }
        _weaponController.SetFloat("Speed", Mathf.Abs(platformVelocity.x));
        Character.SetFloat("Speed", Mathf.Abs(platformVelocity.x));
        _weaponController.SetFloat("vSpeed", platformVelocity.y);
        Character.SetFloat("vSpeed", platformVelocity.y);

        //
        DrawShadow();
    }

    private void FixedUpdate()
    {
        var platformVelocity = Vector2.zero;
        if (_platform != null)
            platformVelocity = _platform.Velocity;
        var velocityClamp = _rb2D.velocity;
        _rb2D.AddForce(Vector2.right * _hClamp * MoveForce, ForceMode2D.Force);
        velocityClamp.x = Mathf.Clamp(_rb2D.velocity.x, -MaxVelocityX * _speed + platformVelocity.x,
            MaxVelocityX * _speed + platformVelocity.x);
        velocityClamp.y = Mathf.Clamp(_rb2D.velocity.y, -MaxVelocityY, MaxVelocityY);

        _rb2D.velocity = velocityClamp;

        // Break
        var breakForce = Vector2.zero;
        if (_grounded && _hClamp > -0.25f && _hClamp < 0.25f)
        {
            breakForce = (_platform
                ? -BreakForce * (_rb2D.velocity - _platform.Velocity)
                : BreakForce * -_rb2D.velocity);
            breakForce.y = 0;
        }
     
        _rb2D.AddForce(breakForce, ForceMode2D.Force);

    
    }

    private void Flip()
    {
        _facingRight = !_facingRight;
        var theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}