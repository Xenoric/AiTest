using UnityEngine;
using System.Collections;

public class F3DAudio : MonoBehaviour
{

    public Transform PlayerCharacter;
    public float VerticalOffset;
    public float ZOffset;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void LateUpdate () {

	    if (PlayerCharacter)
	    {
	        var pos = transform.position;
	        var charPos = PlayerCharacter.position;

	        pos.y = charPos.y + VerticalOffset;
	        pos.z = charPos.z + ZOffset;
	        transform.position = pos;
	    }
	}


    public static void PlayOneShotRandom(AudioSource source, AudioClip clip, Vector2 volume, Vector2 pitch)
    {
        if (source == null || clip == null) return;
        var volRand = Random.Range(volume.x, volume.y);
        var pitchRand = Random.Range(pitch.x, pitch.y);
        source.volume = volRand;
        source.pitch = pitchRand;
        source.PlayOneShot(clip);
    }

    public static int GetRandomClipIndex(int maxIndex)
    {
        if (maxIndex <= 1) return 0;
        var randomIndex = Random.Range(0, maxIndex);
        return randomIndex;
    }

    public static int GetUniqueRandomClipIndex(int maxIndex, int lastUnique)
    {
        if (maxIndex <= 1) return 0;
        var randomIndex = Random.Range(0, maxIndex);
        while (randomIndex == lastUnique)
            randomIndex = Random.Range(0, maxIndex);
        return randomIndex;
    }
}
