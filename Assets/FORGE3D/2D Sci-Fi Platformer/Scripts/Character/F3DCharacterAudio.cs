using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class F3DCharacterAudio : MonoBehaviour
{
    public enum SurfaceType
    {
        None,
        Sand,
        Metal,
        Barrel
    }

    public enum CharacterState
    {
        Idle,
        Run,
        Crouch,
        Land,
        Jump,
        DoubleJump
    }

    public SurfaceType Surface;

    // CHARACTER
    ///////////////////////////////////////////
    public AudioSource Voice;

    public AudioClip[] VoiceClipsGrunt;
    public Vector2 VoicePitchRandom;
    public Vector2 VoiceVolumeRandom;

    //
    private int _lastVoiceGruntIndex;

    // FOOTSTEPS
    /////////////////////////////////////////// 

    // Audio Sources
    public AudioSource Footsteps;

    public AudioSource FootstepsJump;
    public AudioSource FootstepsLand;

    // Sand Clips
    public AudioClip[] FootstepsSandClips;

    public AudioClip[] FootstepsSandLandClips;
    public AudioClip[] FootstepsSandCrouchClips;

    // 
    public Vector2 FootstepsPitchRandom;

    public Vector2 FootstepsVolumeRandom;
    public Vector2 FootstepsLandPitchRandom;
    public Vector2 FootstepsLandVolumeRandom;
    public Vector2 FootstepsCrouchPitchRandom;
    public Vector2 FootstepsCrouchVolumeRandom;

    // Metal Clips
    public AudioClip[] FootstepsMetalClips;

    public AudioClip[] FootstepsMetalLandClips;
    public AudioClip[] FootstepsMetalCrouchClips;

    // 
    public Vector2 FootstepsMetalPitchRandom;

    public Vector2 FootstepsMetalVolumeRandom;
    public Vector2 FootstepsMetalLandPitchRandom;
    public Vector2 FootstepsMetalLandVolumeRandom;
    public Vector2 FootstepsMetalCrouchPitchRandom;
    public Vector2 FootstepsMetalCrouchVolumeRandom;

    // Jump Clips
    public AudioClip[] FootstepsJumpClips;

    public AudioClip[] FootstepsDoubleJumpClips;

    //
    public Vector2 FootstepsJumpPitchRandom;

    public Vector2 FootstepsJumpVolumeRandom;
    public Vector2 FootstepsDoubleJumpPitchRandom;
    public Vector2 FootstepsDoubleJumpVolumeRandom;

    //
    private int _lastFootstepsIndex;

    private int _lastFootstepsJumpIndex;
    private int _lastFootstepsDoubleJumpIndex;
    private int _lastFootstepsLandIndex;
    private int _lastFootstepsCrouchIndex;

    //

    private void Awake()
    {
    }


    private AudioClip[] GetSurfaceClips(CharacterState state)
    {
        switch (Surface)
        {
            default:
            case SurfaceType.Sand:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsSandClips;
                    case CharacterState.Crouch:
                        return FootstepsSandCrouchClips;
                    case CharacterState.Land:
                        return FootstepsSandLandClips;
                    case CharacterState.Jump:
                        return FootstepsJumpClips;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpClips;
                    default:
                    case CharacterState.Idle:
                        return null;
                }
            case SurfaceType.Barrel:
            case SurfaceType.Metal:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsMetalClips;
                    case CharacterState.Crouch:
                        return FootstepsMetalCrouchClips;
                    case CharacterState.Land:
                        return FootstepsMetalLandClips;
                    case CharacterState.Jump:
                        return FootstepsJumpClips;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpClips;
                    default:
                    case CharacterState.Idle:
                        return null;
                }
        }
    }

    private Vector2 GetSurfaceVolume(CharacterState state)
    {
        switch (Surface)
        {
            default:
            case SurfaceType.Sand:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsVolumeRandom;
                    case CharacterState.Crouch:
                        return FootstepsCrouchVolumeRandom;
                    case CharacterState.Land:
                        return FootstepsLandVolumeRandom;
                    case CharacterState.Jump:
                        return FootstepsJumpVolumeRandom;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpVolumeRandom;
                    default:
                    case CharacterState.Idle:
                        return Vector2.one;
                }
            case SurfaceType.Barrel:
            case SurfaceType.Metal:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsMetalVolumeRandom;
                    case CharacterState.Crouch:
                        return FootstepsMetalCrouchVolumeRandom;
                    case CharacterState.Land:
                        return FootstepsMetalLandVolumeRandom;
                    case CharacterState.Jump:
                        return FootstepsJumpVolumeRandom;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpVolumeRandom;
                    default:
                    case CharacterState.Idle:
                        return Vector2.one;
                }
        }
    }

    private Vector2 GetSurfacePitch(CharacterState state)
    {
        switch (Surface)
        {
            default:
            case SurfaceType.Sand:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsPitchRandom;
                    case CharacterState.Crouch:
                        return FootstepsCrouchPitchRandom;
                    case CharacterState.Land:
                        return FootstepsLandPitchRandom;
                    case CharacterState.Jump:
                        return FootstepsJumpPitchRandom;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpPitchRandom;
                    default:
                    case CharacterState.Idle:
                        return Vector2.one;
                }
            case SurfaceType.Barrel:
            case SurfaceType.Metal:
                switch (state)
                {
                    case CharacterState.Run:
                        return FootstepsMetalPitchRandom;
                    case CharacterState.Crouch:
                        return FootstepsMetalCrouchPitchRandom;
                    case CharacterState.Land:
                        return FootstepsMetalLandPitchRandom;
                    case CharacterState.Jump:
                        return FootstepsJumpPitchRandom;
                    case CharacterState.DoubleJump:
                        return FootstepsDoubleJumpPitchRandom;
                    default:
                    case CharacterState.Idle:
                        return Vector2.one;
                }
        }
    }

    // Animation EVENT: Run
    public void OnRunAnimation()
    {
        var state = CharacterState.Run;
        var surfaceClips = GetSurfaceClips(state);
        _lastFootstepsIndex = F3DAudio.GetUniqueRandomClipIndex(surfaceClips.Length, _lastFootstepsIndex);
        F3DAudio.PlayOneShotRandom(Footsteps, surfaceClips[_lastFootstepsIndex], GetSurfaceVolume(state),
            GetSurfacePitch(state));
    }

    // Animation EVENT: Crouch
    public void OnCrouchMoveAnimation()
    {
        var state = CharacterState.Crouch;
        var surfaceClips = GetSurfaceClips(state);
        _lastFootstepsCrouchIndex =
            F3DAudio.GetUniqueRandomClipIndex(surfaceClips.Length, _lastFootstepsCrouchIndex);
        F3DAudio.PlayOneShotRandom(Footsteps, surfaceClips[_lastFootstepsCrouchIndex], GetSurfaceVolume(state),
            GetSurfacePitch(state));
    }

    // Jump
    public void OnJump()
    {
        var state = CharacterState.Jump;
        var surfaceClips = GetSurfaceClips(state);
        _lastFootstepsJumpIndex = F3DAudio.GetUniqueRandomClipIndex(surfaceClips.Length, _lastFootstepsJumpIndex);
        F3DAudio.PlayOneShotRandom(FootstepsJump, surfaceClips[_lastFootstepsJumpIndex], GetSurfaceVolume(state),
            GetSurfacePitch(state));

//        // Grunt
//        if (Voice == null || VoiceClipsGrunt == null || VoiceClipsGrunt.Length <= 0) return;
//        _lastVoiceGruntIndex = GetUniqueRandomClipIndex(VoiceClipsGrunt.Length, -1); // -1 cancels out the unique index
//        Voice.Stop();
//        PlayOneShotRandom(Voice, VoiceClipsGrunt[_lastVoiceGruntIndex], VoiceVolumeRandom, VoicePitchRandom);
    }

    // Double Jump
    public void OnDoubleJump()
    {
        OnJump();
        var state = CharacterState.DoubleJump;
        var surfaceClips = GetSurfaceClips(state);
        _lastFootstepsDoubleJumpIndex =
            F3DAudio.GetUniqueRandomClipIndex(surfaceClips.Length, _lastFootstepsDoubleJumpIndex);
        F3DAudio.PlayOneShotRandom(FootstepsJump, surfaceClips[_lastFootstepsDoubleJumpIndex], GetSurfaceVolume(state),
            GetSurfacePitch(state));
    }

    // Land
    public void OnLand()
    {
        var state = CharacterState.Land;
        var surfaceClips = GetSurfaceClips(state);
        _lastFootstepsLandIndex = F3DAudio.GetUniqueRandomClipIndex(surfaceClips.Length, _lastFootstepsLandIndex);
        F3DAudio.PlayOneShotRandom(FootstepsLand, surfaceClips[_lastFootstepsLandIndex], GetSurfaceVolume(state),
            GetSurfacePitch(state));
    }
}