using System.Collections;
using System.Collections.Generic;
using UnityEngine;


interface ITeamable{
    int MyTeam {get; set;}

    int ReturnTeam();
}

interface IDamagable{
    float Health {set;}
    float Damage {get; set; }

    void DoDamageToMe(float _damage);
}