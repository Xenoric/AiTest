using UnityEngine;
using System.Collections;

public class F3DShotgun : F3DGenericWeapon
{
    public Transform SparkFragment;
    public int ProjectilesPerRound;
    public int SparksPerRound;

    protected override void OnAnimationFireEvent()
    {
        if (!AnimationFireEvent) return;
        OnFire();
    }


    protected override void OnFire()
    {
        
        // Muzzle Flash
        SpawnMuzzleFlash();
        if (Projectile)
            for (var i = 0; i < ProjectilesPerRound; i++)
                SpawnProjectile(Projectile);
        if (SparkFragment)
            for (var i = 0; i < SparksPerRound; i++)
                SpawnProjectile(SparkFragment);
        SpawnSmoke();
        SpawnBarrelSpark();

        // Play Audio
        _weaponAudio.OnFire(AudioInfo);
    }
}