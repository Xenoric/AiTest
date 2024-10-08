using UnityEngine;
using System.Collections;

public class F3DCharacter : MonoBehaviour
{
    public int Health;
    private F3DCharacterController _controller;
    private int _hitTriggerCounter;
    private float _hitTriggerTimer;
    private Rigidbody2D _rBody;
    private F3DWeaponController _weaponController;
    private bool _isDead;

    // Use this for initialization
    void Awake()
    {
        _rBody = GetComponent<Rigidbody2D>();
        _controller = GetComponent<F3DCharacterController>();
        _weaponController = GetComponent<F3DWeaponController>();
    }

    public void OnDamage(int damageAmount)
    {
        if (_controller == null) return;
        if (_isDead) return;

        // Substract incoming damage
        if (Health > 0)
            Health -= damageAmount;

        // Dead Already?
        if (Health <= 0)
        {
            Health = 0;
            _isDead = true;

            // Player Death sequence
            _controller.Character.SetBool(Random.Range(-1f, 1f) > 0 ? "DeathFront" : "DeathBack", true);

            // Dead dont do shit
            _controller.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Dead");
            _rBody.drag = 2f;

//            for (int i = 0; i < _colliders.Length; i++)
//                _colliders[i].enabled = false;
            _weaponController.Drop();

            // Disable blob shadow under the character
            if (_controller.Shadow)
                _controller.Shadow.enabled = false;

            //
            return;
        }

        // Play Hit Animation and limit hit animation triggering 
        if (_hitTriggerCounter < 1)
            _controller.Character.SetTrigger("Hit");
        _hitTriggerCounter++;
    }

    private void LateUpdate()
    {
        // Dead... Quit trying
        if (_isDead) return;
    //    if (Input.GetKeyDown(KeyCode.K))
    //        OnDamage(1000);

        // Handle Hit Trigger timer
        if (_hitTriggerTimer > 0.5f) // <- Hit timer
        {
            _hitTriggerCounter = 0;
            _hitTriggerTimer = 0;
        }
        _hitTriggerTimer += Time.deltaTime;
    }
}