using UnityEngine;
using System.Collections;

public class F3DMeleeTrigger : MonoBehaviour
{
    public F3DMelee MeleeWeapon;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (MeleeWeapon == null) return;
        MeleeWeapon.OnMeleeHit(other);
    }
//    private void OnTriggerStay2D(Collider2D other)
//    {
//        if (MeleeWeapon == null) return;
//        MeleeWeapon.OnMeleeHit(other);
//    }

//    private void OnTriggerExit2D(Collider2D other)
//    {
//        if (MeleeWeapon == null) return;
//        MeleeWeapon.OnMeleeHit(other);
//    }
}