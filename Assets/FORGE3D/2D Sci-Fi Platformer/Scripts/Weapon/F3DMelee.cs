using UnityEngine;
using System.Collections;

public class F3DMelee : F3DGenericWeapon
{
    public F3DMeleeTrigger MeleeTrigger;
    private Collider2D _meleeTriggerCollider2D;
    public ParticleSystem MeleeTrail;

    public AudioSource HitAudio;

    public override void Awake()
    {
        base.Awake();
        if (MeleeTrigger != null)
        {
            // Disable trigger collider util the animation is triggered
            _meleeTriggerCollider2D = MeleeTrigger.GetComponent<Collider2D>();
            _meleeTriggerCollider2D.enabled = false;
        }
    }

    private int _stateSlashId = UnityEngine.Animator.StringToHash("Slash");
    private int _stateStabId = UnityEngine.Animator.StringToHash("Stab");

    public override void Fire()
    {
        // Need MeleeTrigger Component for Melee Weapons
        if (MeleeTrigger == null) return;

        // Check before firing
        if (!Animator.isInitialized) return;
        if (_fireTimer < FireRate) return;

        // Set Fire Timer vars
        _fireTimer = 0;

        // Randomize between slash and stab animations
        var isStab = Random.Range(-10f, 10f) < 0;

        // Manually Play the animation to avoid random desync with the melee trail
        Animator.Play(isStab ? _stateStabId : _stateSlashId);

        // Activate Melee Trail
        if (MeleeTrail && !isStab)
        {
            MeleeTrail.Simulate(0f, false);
            MeleeTrail.Clear();
            MeleeTrail.Play();
        }

        // Toggle Trigger Collider for a short period of time uppon stab/slash animation
        _meleeTriggerCollider2D.enabled = true;
        StartCoroutine(DisableMeleeTrigger(FireRate));
    }

    public override void Stop() { }

    private IEnumerator DisableMeleeTrigger(float fireRate)
    {
        yield return new WaitForSeconds(fireRate);

        // Disable
        _meleeTriggerCollider2D.enabled = false;
    }

    public void OnMeleeHit(Collider2D other)
    {
        F3DGenericProjectile.DealDamage(5, Type, other.transform, BarrelSpark, 1f,
            other.bounds.ClosestPoint(FXSocket.position), FXSocket.up);
    }
}