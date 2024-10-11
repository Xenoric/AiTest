using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolClass : MonoBehaviour
{
    // префаб для объекта пула
    [SerializeField] private GameObject _objectPrefab;
    // размер пула
    [SerializeField] private int _poolSize = 10;
    // собственно, сам пул 0-0
    [SerializeField] private List<GameObject> _pool;

    private int _poolIndex = 0;


    // Start is called before the first frame update
    void Start()
    {
        _pool = new List<GameObject>();
        for(int i=0; i<_poolSize; ++i){
            GameObject _obj = Instantiate(_objectPrefab);
            _obj.SetActive(false);
            _pool.Add(_obj);
        }
    }

    public GameObject GetObject(){
        foreach(var _obj in _pool){
            if(!_obj.activeInHierarchy){
                _obj.SetActive(true);
                return _obj;
            }
        }


        Debug.Log("Пул исчерпан!");
        return null;
    }

    // public GameObject GetObject(){
    //     if(!_pool[_poolIndex].activeInHierarchy){
    //         GameObject obj = _pool[_poolIndex];
    //         ++_poolIndex;
    //         if(_poolIndex+1 > _poolSize){
    //             _poolIndex = 0;
    //         }
    //         obj.SetActive(true);
    //         return obj;
    //     }

    //     return
    //     // for(int i=0; i<_poolSize; ++i){
    //     //     if(_pool[i].enabled == false){
    //     //     }
    //     // }
    // }



    public void ReturnObject(GameObject _obj){
        _obj.SetActive(false);
    }

    


}
