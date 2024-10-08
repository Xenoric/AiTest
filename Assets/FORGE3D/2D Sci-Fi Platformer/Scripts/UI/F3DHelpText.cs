using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F3DHelpText : MonoBehaviour {

    public Transform Logo, HelpPanel;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
        if(Input.GetKeyDown(KeyCode.H))
        {
            Logo.gameObject.SetActive(!Logo.gameObject.activeSelf);
            HelpPanel.gameObject.SetActive(!HelpPanel.gameObject.activeSelf);
        }
        else if(Input.anyKeyDown)
        {
            Logo.gameObject.SetActive(false);
            HelpPanel.gameObject.SetActive(false);
        }
	}
}
