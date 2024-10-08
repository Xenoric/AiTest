using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using LoopState = F3DGenericWeapon.LoopState;
using Random = UnityEngine.Random;

public class F3DWeaponAudio : MonoBehaviour
{
    public AudioSource Weapon;
    public AudioSource WeaponLoop;
    public AudioSource WeaponReload;
    public AudioSource WeaponSpin;
    public AudioSource ProjectileHitClose;

    [Serializable]
    public class WeaponAudioInfo
    {
        public AudioMixerGroup MixerGroup;

        // Single shot
        public AudioClip[] Shot;


        public int ShotLastUniqiueIndex;
        public Vector2 ShotVolume;
        public Vector2 ShotPitch;

        public LoopState State;
        public AnimationCurve SpinPitchStartCurve;
        public AnimationCurve SpinPitchEndCurve;
        public AnimationCurve LoopPitchCurve;
        public AudioClip Loop;
        public AudioClip LoopEnd;
        public float LoopVolume;
        public float LoopPitch;
        public Vector2 LoopPitchRandomRange;
        public float LoopPitchOffsetRate;
        public float LoopPitchOffsetTimer { get; set; }
        public float LoopPitchOffsetLerp { get; set; }
        public float LoopPitchOffset { get; set; }
        public AudioClip SpinLoop;
        public AudioClip SpinStart;
        public AudioClip SpinEnd;
        public float SpinVolume;
        public float SpinPitch;


        // Reload
        public AudioClip[] Reload;
        public AudioMixerGroup ReloadMixerGroup;

        public int ReloadLastUniqiueIndex;
        public Vector2 ReloadVolume;
        public Vector2 ReloadPitch;

        public AudioClip[] Hit;

        public Vector2 HitVolume;
        public Vector2 HitPitch;
        public AudioMixerGroup HitMixerGroup;
    }

    // Stop all audio
    public void StopAllWeaponSounds()
    {
        Weapon.Stop();
        WeaponLoop.Stop();
        WeaponReload.Stop();
        WeaponSpin.Stop();
    }

    // WEAPON

    // Fire single round
    public void OnFire(WeaponAudioInfo audioInfo)
    {
        if (audioInfo.Shot == null || audioInfo.Shot.Length < 1) return;
        Weapon.outputAudioMixerGroup = audioInfo.MixerGroup;
        audioInfo.ShotLastUniqiueIndex =
            F3DAudio.GetUniqueRandomClipIndex(audioInfo.Shot.Length, audioInfo.ShotLastUniqiueIndex);
        F3DAudio.PlayOneShotRandom(Weapon, audioInfo.Shot[audioInfo.ShotLastUniqiueIndex],
            audioInfo.ShotVolume, audioInfo.ShotPitch);
    }

    // Loop sequence start
    public void OnLoopStart(WeaponAudioInfo audioInfo, float time)
    {
        if (audioInfo.State == LoopState.Start)
        {
            WeaponSpin.pitch = audioInfo.SpinPitch * audioInfo.SpinPitchStartCurve.Evaluate(time);
            return;
        }

        audioInfo.State = LoopState.Start;

        //
        WeaponSpin.Stop();
        WeaponSpin.clip = audioInfo.SpinStart;
        WeaponSpin.loop = false;
        WeaponSpin.volume = audioInfo.SpinVolume;
        WeaponSpin.pitch = audioInfo.SpinPitch * audioInfo.SpinPitchStartCurve.Evaluate(time);
        WeaponSpin.outputAudioMixerGroup = audioInfo.MixerGroup;
        WeaponSpin.Play();
    }

    // Loop sequence
    public void OnLoop(WeaponAudioInfo audioInfo, float time)
    {
        if (audioInfo.State == LoopState.Loop)
        {
            audioInfo.LoopPitchOffsetTimer += Time.deltaTime;
            if (audioInfo.LoopPitchOffsetTimer >= 1f)
            {
                audioInfo.LoopPitchOffsetTimer = 0;
                audioInfo.LoopPitchOffset = Random.Range(audioInfo.LoopPitchRandomRange.x,
                    audioInfo.LoopPitchRandomRange.y);
            }

            audioInfo.LoopPitchOffsetLerp = Mathf.Lerp(audioInfo.LoopPitchOffsetLerp, audioInfo.LoopPitchOffset,
                Time.deltaTime * audioInfo.LoopPitchOffsetRate);

            WeaponLoop.pitch = audioInfo.LoopPitch * audioInfo.LoopPitchCurve.Evaluate(time) +
                               audioInfo.LoopPitchOffsetLerp;
            return;
        }

        audioInfo.State = LoopState.Loop;

        // Check for empty clips
        if (audioInfo.SpinLoop == null)
            return;

        // Spin
        WeaponSpin.Stop();
        WeaponSpin.clip = audioInfo.SpinLoop;
        WeaponSpin.loop = true;
        WeaponSpin.volume = audioInfo.SpinVolume;
        WeaponSpin.pitch = audioInfo.SpinPitch;
        WeaponSpin.outputAudioMixerGroup = audioInfo.MixerGroup;
        WeaponSpin.Play();

        // Check for empty clips
        if (audioInfo.Loop == null)
            return;

        // Weapon
        WeaponLoop.Stop();
        WeaponLoop.clip = audioInfo.Loop;
        WeaponLoop.outputAudioMixerGroup = audioInfo.MixerGroup;
        WeaponLoop.loop = true;
        WeaponLoop.volume = audioInfo.LoopVolume;
        WeaponLoop.pitch = audioInfo.LoopPitch;

        WeaponLoop.timeSamples = Random.Range(0, audioInfo.Loop.samples);
        WeaponLoop.Play();
    }

    // Loop Sequence Stop
    public void OnLoopEnd(WeaponAudioInfo audioInfo, float time)
    {
        if (audioInfo.State == LoopState.End)
        {
            WeaponSpin.pitch = audioInfo.SpinPitch * audioInfo.SpinPitchEndCurve.Evaluate(time);
            return;
        }

        // Stop firing
        if (audioInfo.State == LoopState.Loop)
        {
            WeaponLoop.Stop();
            WeaponLoop.clip = null;
            WeaponLoop.outputAudioMixerGroup = audioInfo.MixerGroup;
            WeaponLoop.loop = false;
            WeaponLoop.volume = audioInfo.LoopVolume;
            WeaponLoop.pitch = audioInfo.LoopPitch;
            if (audioInfo.LoopEnd != null)
                WeaponLoop.PlayOneShot(audioInfo.LoopEnd);
        }

        //
        audioInfo.State = LoopState.End;

        // Stop spin
        WeaponSpin.Stop();
        WeaponSpin.clip = audioInfo.SpinEnd;
        WeaponSpin.loop = false;
        WeaponSpin.outputAudioMixerGroup = audioInfo.MixerGroup;
        WeaponSpin.volume = audioInfo.SpinVolume;
        WeaponSpin.pitch = audioInfo.SpinPitch * audioInfo.SpinPitchEndCurve.Evaluate(time);
        WeaponSpin.Play();
    }

    // Reload
    public void OnReload(WeaponAudioInfo audioInfo)
    {
        WeaponReload.outputAudioMixerGroup = audioInfo.ReloadMixerGroup;
        audioInfo.ReloadLastUniqiueIndex =
            F3DAudio.GetUniqueRandomClipIndex(audioInfo.Reload.Length, audioInfo.ReloadLastUniqiueIndex);
        F3DAudio.PlayOneShotRandom(WeaponReload, audioInfo.Reload[audioInfo.ReloadLastUniqiueIndex],
            audioInfo.ReloadVolume, audioInfo.ReloadPitch);
    }

    // IMPACTS
    public static void OnProjectileImpact(AudioSource audioSource, WeaponAudioInfo audioInfo)
    {
        if (audioInfo.Hit == null || audioInfo.Hit.Length < 1) return;
        var random = F3DAudio.GetRandomClipIndex(audioInfo.Hit.Length);
        var hit = audioInfo.Hit[random];
        audioSource.outputAudioMixerGroup = audioInfo.HitMixerGroup;
        F3DAudio.PlayOneShotRandom(audioSource, hit, audioInfo.HitVolume, audioInfo.HitPitch);
    }
}