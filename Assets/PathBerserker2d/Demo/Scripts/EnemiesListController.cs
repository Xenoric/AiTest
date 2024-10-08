using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemiesListController : MonoBehaviour
{   
    [SerializeField] private List<GameObject> _firstTeam = new List<GameObject>();
    [SerializeField] private List<GameObject> _secondTeam = new List<GameObject>();
    private int _matchType;

    void Start()
    {   
        // Вы должны это перенести на кнопку создания матча
        StartDeathMatch();
        //StartTeamDM();
    }

    public void StartDeathMatch(){
        _firstTeam = new List<GameObject>(GameObject.FindGameObjectsWithTag("player"));
        _firstTeam.AddRange(new List<GameObject>(GameObject.FindGameObjectsWithTag("enemy")));

        _matchType = 1;
    }

    public void StartTeamDM(){
        _firstTeam = new List<GameObject>(GameObject.FindGameObjectsWithTag("player"));
        _secondTeam = new List<GameObject>(GameObject.FindGameObjectsWithTag("enemy"));

        _matchType = 2;
    }

    public List<GameObject> ReturnFirstTeam(){
        return _firstTeam;
    }

    public List<GameObject> ReturnSecondTeam(){
        return _secondTeam;
    }

    public void RemoveFromTeam(int team, GameObject i){
        if(team == 1){
            _firstTeam.Remove(i);
        }else{
            _secondTeam.Remove(i);
        }
    }

    public int ReturnMatchType(){
        return _matchType;
    }

}
