using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletController : MonoBehaviour
{
    [SerializeField] private GameObject bullet;
    private bool dir = false; //false - left, true = right

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E)){
            Vector2 pos = transform.position;
        if(dir){
            pos.x += 2;
        }else{
            pos.x -= 2;
        }
        pos.y += 1;

        Instantiate(bullet, pos, Quaternion.identity);
        }
    }
}
