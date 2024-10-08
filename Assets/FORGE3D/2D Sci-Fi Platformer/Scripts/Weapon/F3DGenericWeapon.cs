using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class F3DGenericWeapon : MonoBehaviour
{
    [Header("General")] public Animator Animator;
    public Transform Bone;
    public Transform PowerUp;

    // Hands
    public SpriteRenderer LeftHand;

    public SpriteRenderer RightHand;
    public int LeftHandId;
    public int RightHandId;

    // Weapon 
    [Header("Weapon")] public F3DWeaponController.WeaponType Type;

    public Mode FireMode;
    public bool AnimationFireEvent;
    public bool AnimationReadyEvent;
    public bool AnimationSpawnShellEvent;
    public float SpinUpRate;
    public float SpinDownRate;
    public float SpinUpDelayOnEarlyStop;
    public float LoopDelayOnEarlyStop;
    public float FireRate;
    public bool UseFireRateCurve;
    public AnimationCurve FireRateCurve;
    public bool AnimatorFireRateMult;
    public bool FireQueue;

    // Checks
    public float ProjectileCloseRange;

    // Sockets
    [Header("Sockets")] public Transform FXSocket;

    public Transform ShellSocket;

    // Prefabs
    [Header("Prefabs")] public Transform MuzzleFlash;

    public Transform Projectile;
    public Transform Shell;
    public Transform Smoke;
    public Transform BarrelSpark;

    // Muzzle Flash
    [Header("Muzzle Flash")] public Vector2 MuzzleFlashOffset;

    public Vector2 MuzzleFlashLifeTime = new Vector2(0.1f, 0.15f);
    public bool MuzzleFlashRandomFlip = true;
    public float MuzzleFlashZ = -1;

    // Shell
    [Header("Shell")] public Vector2 ShellForce = new Vector2(5f, 10f);

    public Vector2 ShellTorque = new Vector2(-2f, 2f);

    // Projectile 
    [Header("Projectile")] public Vector2 ProjectileForce;

    public LayerMask ProjectileHitLayerMask;
    public float ProjectileDelay;
    public Vector2 ProjectileOffset;
    public Vector2 ProjectileRotation = new Vector2(-0.05f, 0.05f);
    public Vector2 ProjectileLifeTime = new Vector2(0.1f, 0.15f);
    public Vector2 ProjectileBaseScale = new Vector2(1f, 2f);
    public Vector2 ProjectileScaleX = new Vector2(0f, 0.5f);

    public enum Mode
    {
        Single,
        Auto,
        Loop
    }

    public enum LoopState
    {
        None,
        Auto,
        Start,
        Loop,
        End,
        EarlyStop
    }

    private LoopState _state;
    private float _stateTimer;
    private float _loopTimer;
    private float _earlyStopTimer;
    private float _fireRateCurrentDelay;
    private bool _shotQueued;
    private float _fireRate;

    // Sound
    public F3DWeaponAudio.WeaponAudioInfo AudioInfo;

    //
    protected float _dir;

    protected float _fireTimer;

    // Colliders cache
    protected Collider2D[] _colliders;

    protected F3DWeaponAudio _weaponAudio;
    private float _lastDir;

    // Attached weapon effects
    private List<Transform> _barrelEffects = new List<Transform>();
    private List<Transform> _smokeEffects = new List<Transform>();

    public virtual void Awake()
    {
        Animator = GetComponent<Animator>();
        _colliders = transform.root.GetComponentsInChildren<Collider2D>();
        _weaponAudio = GetComponentInParent<F3DWeaponAudio>();
        _fireTimer = _fireRate;
    }

    public void OnEnable()
    {
        if (Bone != null)
        {
            Animator.enabled = false;
            Bone.rotation = Quaternion.identity;
            Bone.localRotation = Quaternion.identity;
            Animator.enabled = true;
        }
        _state = LoopState.None;
        _stateTimer = 0f;
        _loopTimer = 0f;
        _fireTimer = FireRate;

        //
        if (FireMode == Mode.Auto && UseFireRateCurve)
            _fireRate = FireRateCurve.Evaluate(0f);
        else
            _fireRate = FireRate;
    }

    public void OnDisable()
    {
        if (Bone != null)
        {
            Animator.enabled = false;
            Bone.rotation = Quaternion.identity;
            Bone.localRotation = Quaternion.identity;
        }
        if (FireMode == Mode.Loop)
        {
            _state = LoopState.None;
            _stateTimer = 0f;
            _loopTimer = 0f;
            _fireTimer = 0f;
            _fireRate = 0f;
            _weaponAudio.OnLoopEnd(AudioInfo, _stateTimer);
            AudioInfo.State = LoopState.None;
        }
    }

    // Use this for initialization
    private void Start() { }

    // Update is called once per frame
    private void Update()
    {
        if (FireMode == Mode.Loop)
        {
            switch (_state)
            {
                case LoopState.None:
                    _loopTimer = 0;
                    break;
                case LoopState.Start:
                case LoopState.EarlyStop:
                    if (_state == LoopState.Start && _stateTimer >= 1f)
                    {
                        _earlyStopTimer = 0;
                        _state = LoopState.Loop;
                        _stateTimer = 1f;
                        Animator.SetFloat("SpinUpRate", _stateTimer);
                    }
                    else if (_state == LoopState.EarlyStop && _stateTimer >= 1f &&
                             _earlyStopTimer >= LoopDelayOnEarlyStop)
                    {
                        _state = LoopState.End;
                        _earlyStopTimer = 0;
                    }
                    else if (_state == LoopState.EarlyStop && _stateTimer < 1f &&
                             _earlyStopTimer >= SpinUpDelayOnEarlyStop)
                    {
                        _state = LoopState.End;
                        _earlyStopTimer = 0;
                    }
                    else
                    {
                        _earlyStopTimer += Time.deltaTime;
                        _stateTimer += Time.deltaTime * SpinUpRate;
                        _stateTimer = Mathf.Clamp01(_stateTimer);
                        Animator.SetFloat("SpinUpRate", _stateTimer);
                        if (_state != LoopState.EarlyStop)
                            _weaponAudio.OnLoopStart(AudioInfo, _stateTimer);
                    }
                    break;
                case LoopState.Loop:
                    _earlyStopTimer = 0;
                    Animator.SetBool("Fire", true);
                    _weaponAudio.OnLoop(AudioInfo, _loopTimer);
                    _loopTimer += Time.deltaTime * SpinUpRate;
                    _loopTimer = Mathf.Clamp01(_loopTimer);
                    break;
                case LoopState.End:
                    Animator.SetBool("Fire", false);
                    _earlyStopTimer = 0;
                    _loopTimer = Mathf.Max(0, _loopTimer - Time.deltaTime * SpinUpRate);

                    //
                    if (_stateTimer <= 0)
                    {
                        _state = LoopState.None;
                        _stateTimer = 0;
                        Animator.SetFloat("SpinUpRate", _stateTimer);
                        Animator.SetBool("SpinUp", false);
                    }
                    else
                    {
                        _stateTimer -= Time.deltaTime * SpinDownRate;
                        _stateTimer = Mathf.Clamp01(_stateTimer);
                        Animator.SetFloat("SpinUpRate", _stateTimer);
                    }

                    //
                    if (_state != LoopState.EarlyStop)
                        _weaponAudio.OnLoopEnd(AudioInfo, _stateTimer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (FireMode == Mode.Auto)
        {
            if (_state == LoopState.Auto && _fireTimer >= _fireRate)
            {
                _fireTimer = 0f;
                OnFire();
            }
            if (_state == LoopState.Auto && UseFireRateCurve)
            {
                if(AnimatorFireRateMult)
                    Animator.SetFloat("FireRateMult", Mathf.Clamp(_stateTimer, 0f, 1.6f));
                _stateTimer += Time.deltaTime;
                _fireRate = Mathf.Max(0, FireRateCurve.Evaluate(_stateTimer));
            }
        }

        // Fire Timer
        _fireTimer += Time.deltaTime;
        _fireTimer = Mathf.Clamp(_fireTimer, 0, _fireRate);

        if (FireQueue && _shotQueued && _fireTimer >= _fireRate)
        {
            Fire();
            _shotQueued = false;
        }
      
       // F3DDebug.LogFloat("fireTimer", _fireTimer);
      //  F3DDebug.LogFloat("stateTimer", _stateTimer);
       // F3DDebug.LogFloat("fireRate", _fireRate);
    }

    private void LateUpdate()
    {
        DragBarrelEffects();

        // Direction
        _lastDir = Mathf.Sign(FXSocket.parent.lossyScale.x);
    }


    // ANIMATION EVENTS
    public virtual void OnAnimationReadyEvent()
    {
        if (!AnimationReadyEvent) return;
    }

    
    protected virtual void OnAnimationFireEvent()
    {
        if (!AnimationFireEvent) return;
        OnFire();
    }

    protected virtual void OnAnimationSpawnShellEvent()
    {
        if (!AnimationSpawnShellEvent) return;
        SpawnShell();
    }

    // INPUT EVENTS
    public virtual void Fire()
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        // 
        if (FireMode == Mode.Loop)
        {
            if (_state == LoopState.EarlyStop && _stateTimer >= 0.99f)
                _state = LoopState.Loop;
            else
            {
                _state = LoopState.Start;
                Animator.SetBool("SpinUp", true);
            }
        }
        else
        {
            if (_fireTimer < _fireRate)
            {
                if (FireQueue) _shotQueued = true;
                return;
            }
            _fireTimer = 0f;
            if (FireMode == Mode.Auto)
                _state = LoopState.Auto;

            // Trigger shot animator
            Animator.SetTrigger("FireTrigger");
            Animator.SetBool("Fire", true);
            if (!AnimationFireEvent)
                OnFire();
        }
    }

  
    public virtual void Stop()
    {
        if (FireMode == Mode.Loop)
        {
            if (_stateTimer < 1f && _earlyStopTimer >= SpinUpDelayOnEarlyStop)
            {
                _state = LoopState.End;
                Animator.SetBool("SpinUp", false);
            }
            else if (_stateTimer >= 0.99f && _earlyStopTimer >= LoopDelayOnEarlyStop)
            {
                _state = LoopState.End;
                Animator.SetBool("SpinUp", false);
            }
            else
            {
                _state = LoopState.EarlyStop;
            }
        }
        else
        {
            if (FireMode == Mode.Auto)
            {
                _state = LoopState.None;
                _stateTimer = 0f;
              //  _fireTimer = 0f;
            }

            // Stop shot animation
            if (!Animator.isInitialized) return;
            Animator.SetBool("Fire", false);
        }
    }

    // WEAPON
    protected virtual void OnFire()
    {
        if (Type == F3DWeaponController.WeaponType.Knife) return;

        // Shell
        SpawnShell();

        // Muzzle Flash
        SpawnMuzzleFlash();

        // Spawn Projectile/Beam
        if (this.Type == F3DWeaponController.WeaponType.Beam)
            SpawnBeam(Projectile);
        else
            SpawnProjectile(Projectile);
        SpawnSmoke();
        SpawnBarrelSpark();

        // Play Audio
        if (FireMode != Mode.Loop)
            _weaponAudio.OnFire(AudioInfo);
    }

    protected void SpawnShell()
    {
        if (Type == F3DWeaponController.WeaponType.Shotgun)
            _weaponAudio.OnReload(AudioInfo);
        if (Shell == null) return;

        // Pos
        var pos = ShellSocket.position;
        pos.z = -2;

        // Rotation
        var rot = ShellSocket.rotation *
                  Quaternion.Euler(0, 0, Random.Range(-5, 5));

        // Spawn 
        var shell = F3DSpawner.Spawn(Shell, pos, rot, null);
        var shellRb = shell.GetComponent<Rigidbody2D>();
        shellRb.AddForce((Vector2) ShellSocket.up * Random.Range(ShellForce.x, ShellForce.y), ForceMode2D.Impulse);
        shellRb.AddTorque(Random.Range(ShellTorque.x, ShellTorque.y), ForceMode2D.Force);

        // Despawn
        F3DSpawner.Despawn(shell, 1.5f);
    }

    protected void SpawnMuzzleFlash()
    {
        if (MuzzleFlash == null) return;

        // Direction
        _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);

        // Pos
        var pos = FXSocket.position;
        pos += MuzzleFlashOffset.x * FXSocket.right * _dir;
        pos += MuzzleFlashOffset.y * FXSocket.up;
        pos.z = MuzzleFlashZ;

        // Rotation
        var rot = FXSocket.rotation;
        if (MuzzleFlashRandomFlip && Random.Range(-1f, 1f) < 0)
            rot *= Quaternion.Euler(180, 0, 0);
        if (_dir < 0)
            rot *= Quaternion.Euler(0, 180, 0);

        //var rotation = FXSocket.rotation *Quaternion.Euler(0, 0, Random.Range(MuzzleFlashRotation.x, MuzzleFlashRotation.y));

        // Lifetime
        var lifeTime = Random.Range(MuzzleFlashLifeTime.x, MuzzleFlashLifeTime.y);

        // Spawn 
        var muzzleFlash = F3DSpawner.Spawn(MuzzleFlash, pos, rot, FXSocket);

        //        // Scale
        //        var scale = muzzleFlash.localScale;//* Random.Range(MuzzleFlashBaseScale.x, MuzzleFlashBaseScale.y);
        //        scale.x *= _dir;
        //   scale.x += Random.Range(MuzzleFlashScaleX.x, MuzzleFlashScaleX.y);

        //  scale.z = Random.Range(-1f, 1f) > 0 ? scale.z : -scale.z;

        //  muzzleFlash.localScale = scale;
        _barrelEffects.Add(muzzleFlash);

        // Despawn
        F3DSpawner.Despawn(muzzleFlash, lifeTime);
    }

    protected void SpawnProjectile(Transform projectilePrefab)
    {
        // Direction
        _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);

        // Run close distance check from the bone pivot to the FXSocket twice long

        //
        Debug.DrawLine(FXSocket.position - FXSocket.right * _dir,
            FXSocket.position + FXSocket.right * 2 * _dir, Color.red, 0.5f);

        //
        var closeCheckHit = Physics2D.LinecastAll(FXSocket.position - FXSocket.right * _dir,
            FXSocket.position + FXSocket.right * ProjectileCloseRange * _dir, ProjectileHitLayerMask);

        // Close range hit - Spawn the impact and play hit sound without spawning the projectile
        if (closeCheckHit != null && closeCheckHit.Length > 0)
        {
            var projObject = projectilePrefab.GetComponent<F3DGenericProjectile>();

            // Ignore own colliders
            for (var i = 0; i < closeCheckHit.Length; i++)
            {
                var selfHit = false;
                for (var j = 0; j < _colliders.Length; j++)
                    if (closeCheckHit[i].collider == _colliders[j])
                    {
                        selfHit = true;
                        break;
                    }
                if (selfHit)
                    continue;
                F3DGenericProjectile.DealDamage(5, Type, closeCheckHit[i].transform, projObject.Hit,
                    projObject.HitLifeTime,
                    closeCheckHit[i].point, closeCheckHit[i].normal);

                // Play close impact through the attached soundSource
                F3DWeaponAudio.OnProjectileImpact(_weaponAudio.ProjectileHitClose, AudioInfo);
                return;
            }
        }

        // Keep the initial position, rotaion of the FXSocket so the projectile is launched in the correct _dir
        // Random Offset

        // Position
        var position = FXSocket.position + FXSocket.right * ProjectileOffset.x * _dir;
        position.z = 0;

        // Rotation
        var rotation = FXSocket.rotation;
        if (_dir < 0)
            rotation *= Quaternion.Euler(0, 0, 180);
        rotation *= Quaternion.Euler(0, 0, Random.Range(ProjectileRotation.x, ProjectileRotation.y));

        // Spawn Delayed
        StartCoroutine(SpawnProjectileDelayed(projectilePrefab, position, rotation));
    }

    private IEnumerator SpawnProjectileDelayed(Transform projectilePrefab, Vector2 position, Quaternion rotation)
    {
        if (!projectilePrefab) yield break;
        if (ProjectileDelay > 0)
            yield return new WaitForSeconds(ProjectileDelay);

        // Lifetime
        var lifeTime = Random.Range(ProjectileLifeTime.x, ProjectileLifeTime.y);

        // Spawn
        var projectile = F3DSpawner.Spawn(projectilePrefab, position, rotation, null);

      

        // Set Weapon Type
        var projectileObject = projectile.GetComponent<F3DGenericProjectile>();
        projectileObject.WeaponType = Type;

        // Set AudioInfo
        projectileObject.AudioInfo = AudioInfo;

        // Scale
        var scale = projectile.localScale * Random.Range(ProjectileBaseScale.x, ProjectileBaseScale.y);
        projectile.localScale = scale;
        var projRb = projectile.GetComponent<Rigidbody2D>();
        var collider = projectile.GetComponent<Collider2D>();

        // Ignore Self
        for (var j = 0; j < _colliders.Length; j++)
            Physics2D.IgnoreCollision(collider, _colliders[j]);

        // Launch  
        var forceRandom = Random.Range(ProjectileForce.x, ProjectileForce.y);
        projRb.AddForce(projectile.right * forceRandom, ForceMode2D.Force);

        // Despawn on lifetime
        F3DSpawner.Despawn(projectile, lifeTime);
    }

    protected void SpawnBeam(Transform projectilePrefab)
    {
        if (!projectilePrefab) return;

        // Pos
        var pos = FXSocket.position;
        pos.z = 0;

        // Rotation
        var rot = FXSocket.rotation;

        // Spawn
        var projectile = F3DSpawner.Spawn(projectilePrefab, pos, rot, FXSocket);

        // Set Weapon Type
        var projectileObject = projectile.GetComponent<F3DPulse>();
        projectileObject.WeaponType = Type;
        projectileObject.AudioInfo = AudioInfo;
    }

    protected void SpawnSmoke()
    {
        if (!Smoke) return;
        var lifeTime = Random.Range(3, 3);

        // Pos
        var pos = FXSocket.position;
        pos.z = 0;

        // Rotation
        var rot = FXSocket.rotation;
        var smoke = F3DSpawner.Spawn(Smoke, pos, rot, null);
        var direction = Mathf.Sign(FXSocket.parent.lossyScale.x);
        var dirScale = smoke.localScale;
        dirScale.x *= direction;
        smoke.localScale = dirScale;
        _smokeEffects.Add(smoke);
        F3DSpawner.Despawn(smoke, lifeTime);
    }

  


    protected void SpawnBarrelSpark()
    {
        if (!BarrelSpark) return;
        var lifeTime = Random.Range(3, 3);

        // Pos
        var pos = FXSocket.position;
        pos.z = 0;

        // Rotation
        var rot = FXSocket.rotation;
        var barrelSpark = F3DSpawner.Spawn(BarrelSpark, pos, rot, null);
        _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);
        var dirScale = barrelSpark.localScale;
        dirScale.x *= _dir;
        barrelSpark.localScale = dirScale;
        F3DSpawner.Despawn(barrelSpark, lifeTime);
    }

    private void DragBarrelEffects()
    {
        // Muzzle Flash
        for (var i = _barrelEffects.Count - 1; i >= 0; i--)
        {
            if (_barrelEffects[i] == null)
            {
                _barrelEffects.RemoveAt(i);
                continue;
            }
            var direction = Mathf.Sign(FXSocket.parent.lossyScale.x);

//            // Pos
//            var position = FXSocket.position;
//            position += MuzzleFlashOffset.x * FXSocket.right * _dir;
//            position.z = MuzzleFlashZ;
//            _barrelEffects[i].position = position;
//
//            // Direction switch
            if (_lastDir != direction)
                _barrelEffects[i].rotation = Quaternion.LookRotation(Vector3.forward, FXSocket.up * direction);
        }

        // Smoke
        for (var i = _smokeEffects.Count - 1; i >= 0; i--)
        {
            if (_smokeEffects[i] == null)
            {
                _smokeEffects.RemoveAt(i);
                continue;
            }
            _smokeEffects[i].position = FXSocket.position;
        }
    }

}