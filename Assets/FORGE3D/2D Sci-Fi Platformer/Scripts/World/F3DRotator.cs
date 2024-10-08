using UnityEngine;
using System.Collections;

public class F3DRotator : MonoBehaviour
{

    public Vector3 AxisAngle;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
	{

	    transform.rotation *= Quaternion.Euler(AxisAngle * Time.deltaTime);
	}
}
