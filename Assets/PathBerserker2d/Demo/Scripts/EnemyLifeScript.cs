using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyLifeScript : MonoBehaviour
{
    [SerializeField] private EnemiesListController ELC;
    public List<GameObject> enemiesList;

    private int _team = 1;

    private float _health = 100f;

    private void Start(){
        ELC = FindObjectOfType<EnemiesListController>();
            if(_team == 1){
                enemiesList = ELC.ReturnFirstTeam();
            }else{
                enemiesList = ELC.ReturnSecondTeam();
            }
    }

    public void DoDamage(float i){
        _health -= i;
        if(_health <= 0){
            this.gameObject.SetActive(false);
            ELC.RemoveFromTeam(1, this.gameObject);
        }
    }
}
