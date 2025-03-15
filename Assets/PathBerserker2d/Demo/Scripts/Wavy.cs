using UnityEngine;

/// <summary>
/// Rotates object with sin to simulate waves.
/// </summary>
public class Wavy : MonoBehaviour
{
    [SerializeField]
    float speed = 0.1f;
    [SerializeField]
    float maxRotation = 20f;


    private void Update()
    {
        float z = Mathf.Sin(Time.time * speed) * maxRotation;

        Vector3 rot = transform.rotation.eulerAngles;
        rot.z = z;
        transform.rotation = Quaternion.Euler(rot);
    }
}
