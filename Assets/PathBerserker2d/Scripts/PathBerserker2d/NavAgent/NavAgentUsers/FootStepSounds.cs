using System;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Play foot steps depending on the NavTags at the agents current position.
    /// </summary>
    public class FootStepSounds : MonoBehaviour
    {
        public AudioClip[] FootStepSoundClips
        {
            get => footstepSounds;
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (value.Length != PathBerserker2dSettings.NavTags.Length)
                    throw new ArgumentException($"FootStepSoundClips needs to be an array of length equal to the amount of NavTags ({PathBerserker2dSettings.NavTags.Length}).");

                footstepSounds = value;
            }
        }

        [SerializeField]
        public AudioSource audioSource = null;

        [SerializeField]
        public NavAgent agent = null;

        /// <summary>
        /// Delay between playing of footstep sounds.
        /// </summary>
        [SerializeField]
        public float footStepDelay = 1f;

        /// <summary>
        /// Used when no NavTag specific footStep was found, or if the current segment has no NavTag.
        /// </summary>
        [SerializeField]
        public AudioClip defaultFootstep = null;

        /// <summary>
        /// Footsteps to use for each NavTag.
        /// </summary>
        [SerializeField]
        AudioClip[] footstepSounds = null;

        private float lastFootStepTime;

        void Update()
        {
            // time to play next step? Is agent moving on segment?
            if (Time.time - lastFootStepTime >= footStepDelay && agent.IsMovingOnSegment)
            {
                lastFootStepTime = Time.time;
                int navTagV = agent.CurrentNavTagVector;

                AudioClip chosenClip = defaultFootstep;
                // chose the first step sound with matching NavTag
                for (int i = 0; i < footstepSounds.Length; i++)
                {
                    if ((navTagV & (1 << i)) != 0)
                    {
                        chosenClip = footstepSounds[i];
                        break;
                    }
                }
                audioSource.PlayOneShot(chosenClip);
            }
        }

        private void OnValidate()
        {
            if (footstepSounds == null)
            {
                footstepSounds = new AudioClip[PathBerserker2dSettings.NavTags.Length];
            }
            if (footstepSounds.Length != PathBerserker2dSettings.NavTags.Length)
            {
                System.Array.Resize(ref footstepSounds, PathBerserker2dSettings.NavTags.Length);
            }
        }

        private void Reset()
        {
            agent = GetComponent<NavAgent>();
        }
    }
}
