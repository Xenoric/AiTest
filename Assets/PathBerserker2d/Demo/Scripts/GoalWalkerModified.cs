using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d{
    public class GoalWalkerModified : MonoBehaviour
{

    private int status = 0; // 0 - поиск, 1 - атака, 2 - смэрть

    public List<GameObject> enemies;

    [SerializeField]
    public NavAgent navAgent;
    [SerializeField]
    Transform goal = null;
    private Transform buf;

    // Start is called before the first frame update
    void Start()
    {
        // ищем по тегу всех врагов на лэйвэле
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("player"));

        // устраняем обладателя скрипта из списка, что бы он не суициднулся
        enemies.Remove(this.gameObject);

        // можно искать по имени, если теги заняты, но это занимает больше ресурсов
        //enemies = new List<GameObject>(GameObject.Find(name));
    }

    void Update()
    {
        // are we not close enough to our goal and not already moving to its position
        if (Vector2.Distance(goal.position, navAgent.transform.position) > 0.5f && (navAgent.IsIdle || goal.hasChanged))
        {
            goal.hasChanged = false;
            navAgent.UpdatePath(goal.position);
        }

        buf = searchEnemy(enemies).GetComponent<Transform>();

        if(buf != null){
            goal = buf;
        }

        Debug.Log(status);
        // if(status == 0){
        //     goal = searchEnemy(enemies).GetComponent<Transform>();
        //     Debug.Log(searchEnemy(enemies));
        //     Debug.Log("Search");
        //     Debug.Log(goal);
        // }else if(status == 1){

        // }else if(status == 2){

        // }
    }

    private void Reset()
    {
        navAgent = GetComponent<NavAgent>();
    }

    // моя ф-ция для поиска ближайшего врага(горжусь ей)
     private GameObject searchEnemy(List<GameObject> enemyList){
        float minDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach(GameObject enemy in enemyList){
            if(enemy != null){
                float dist = Vector2.Distance(transform.position, enemy.GetComponent<Transform>().position);
                if(dist < minDistance){
                    minDistance = dist;
                    nearestEnemy = enemy;
                }
            }
            
        }
        return nearestEnemy;
    }
}

}

 