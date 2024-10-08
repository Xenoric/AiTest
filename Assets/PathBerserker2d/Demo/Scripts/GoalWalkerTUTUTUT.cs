// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class GoalWalkerTUTUTUT : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;

// namespace PathBerserker2d
// {
//     /// <summary>
//     /// Make Agent walk to specified goal, if it isn't there already. 
//     /// </summary>
//     public class GoalWalker : MonoBehaviour
//     {
//         //=!!!НУЖНО=ДЛЯ=ПЕРЕМЕЩЕНИЯ!!!===================
//         [SerializeField]
//         public NavAgent navAgent;
//         [SerializeField]
//         Transform goal = null;
//         [SerializeField] Transform a;
//         //=!!!НУЖНО=ДЛЯ=ПЕРЕМЕЩЕНИЯ!!!===================

//         private bool dir = false; //false - left, true = right

//         //список всех врагов на уровне
//         private List<GameObject> enemies;

//         // Переменная для хранения префаба пули
//         [SerializeField]private GameObject bullet;
//         //private Vector2 bulletPos;

//         // Переменная для хранения индекса текущей пули в пуле пуль(йоу, окхххимирон-лэвл панчлайн)
//         private int bulletIndex = 0;
//         // Переменная для хранения значения...короче ёмкость магазина(есть ф-ция для изменения значения)
//         // Всякий функционал с перезарядкой надеюсь сами накатаете :b
//         private int maxBulletIndex = 7;
        

//         //пременная для хранения состояния бота(да, надо было перечисления, но неа :b)
//         [SerializeField] private int status = 0; // 0 - поиск, 1 - атака, 2 - смээээрть
//         private bool flag1 = true; // см. строку 47(дедпи47)
//         private bool stopFlag = true;

//         //"флаг" для задержки стрельбы
//         private bool shootFlag = true;
//         //пауза между выстрелами(есть ф-ция для изменения)
//         private float shootColdown = 0.5f;
//         //радиус, в котором мы стреляем по врагу
//         private float attackRadius = 5f;

//         // Что бы посмотреть, жив ты или не жив(если бот оказался вот, и не бот и не баг, а так...)
//         private bool deathFlag = true;

//         public List<GameObject> bulletsPool = null;


//         // ф-ция для настройки максимального кол-ва пуль в магазине
//         public void setMaxBulletIndex(int i){
//             maxBulletIndex = i;
//         }

//         // ф-ция для настройки колдауна между выстрелами
//         public void setShootColdown(float i){
//             shootColdown = i;
//         }

//         private void Start(){
//             // ищем по тегу всех врагов на лэйвэле
//             enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("player"));

//             // устраняем обладателя скрипта из списка, что бы он не суициднулся
//             enemies.Remove(this.gameObject);

//             //bulletPos = bullet.GetComponent<Transform>().position;

//             StartCoroutine(myUpdateCoroutine());

//             for(int i=0;i<10; ++i){
//                 GameObject bull = Instantiate(bullet, transform.position, Quaternion.identity, transform);
//                 bulletsPool.Add(bull);
//                 bull.SetActive(false);
//             }
//         }

//         IEnumerator myUpdateCoroutine(){
//             while(true){
//                 searchEnemy2(enemies);
//                 //if(goal != null){
//                     //goal = a;
//                 //}
//                 yield return new WaitForSeconds(0.1f);
//         }
//         }

//         void Update()
//         {   
//             // if(status == 0){
//             //     //Debug.Log("STATUS0");
//             //     a = searchEnemy(enemies)();
//             //     if(a != null)
//             //     {   //Reset();
//             //         goal = a;}
//             // }else if(status == 1 && deathFlag){
//             //     if(stopFlag){
//             //         navAgent.UpdatePath(this.transform.position);
//             //         stopFlag = false;
//             //     }
//             //     //Debug.Log("STATUS1");
//             //     goal = null;
//             //     if(flag1){
//             //         StartCoroutine(searchCoroutine());
//             //         flag1 = false;
//             //     }
//             //     if(shootFlag){
//             //         StartCoroutine(secondShootCoroutine());
//             //         if(a.GetComponent<Transform>().position.x > transform.position.x){
//             //             dir = true;
//             //         }else{
//             //             dir = false;
//             //         }
//             //         shootFlag = false;

//             //         //GameObject bul = Instantiate(bullet, transform.position, Quaternion.identity);
//             //         //bul.GetComponent<Transform>().position += new Vector3(a.GetComponent<Transform>().position.x, a.GetComponent<Transform>().position.y, 0) * Time.deltaTime;
//             //     }
//             // }else if(status == 2){
//             //     if(deathFlag){
//             //         navAgent.UpdatePath(this.transform.position);
//             //         deathFlag = false;
//             //         goal = null;
//             //     }
//             // }
            

//             // are we not close enough to our goal and not already moving to its position
//             if (Vector2.Distance(goal.position, navAgent.transform.position) > 0.5f && (navAgent.IsIdle || goal.hasChanged))
//             {
//                 goal.hasChanged = false;
//                 navAgent.UpdatePath(goal.position);
//             }
//         }

//         private void Reset()
//         {
//             navAgent = GetComponent<NavAgent>();
//         }


//         private void searchEnemy2(List<GameObject> enemyList){
//             float minDistance = Mathf.Infinity;
//             GameObject nearestEnemy = null;

//             foreach(GameObject enemy in enemyList){
//                 if(enemy != null){
//                     float dist = Vector2.Distance(transform.position, enemy.GetComponent<Transform>().position);
//                     if(dist < minDistance){
//                         minDistance = dist;
//                         nearestEnemy = enemy;
//                     }
//                 }
//             }

//             Transform nearPos = nearestEnemy.GetComponent<Transform>();

//             if(minDistance <= attackRadius){
//                 if(checkObstacle(this.transform.position, nearestEnemy)){
//                     goal = nearPos;
//                     }else{
//                         goal = null;
//                         navAgent.UpdatePath(this.transform.position);
//                         if(shootFlag){
//                             if(nearPos.position.x > transform.position.x){
//                                 dir = true;
//                             }else{
//                                 dir = false;
//                             }

//                             Debug.Log("WE NEED ATTACK!!!");
//                             StartCoroutine(attackCoroutine(nearestEnemy));
//                             shootFlag = false;
//                     }
//                 }
//             }else{
//                 goal = nearPos;
//             }

//             //return nearestEnemy.GetComponent<Transform>();
//         } 

//         IEnumerator attackCoroutine(GameObject enemy){
//         Debug.Log("ATTACK!");
//         Vector2 pos = transform.position;
//         if(dir){
//             pos.x += 2;
//         }else{
//             pos.x -= 2;
//         }
//         pos.y += 1;

//         GameObject bull = bulletsPool[bulletIndex];
//         if(bulletIndex+1 > maxBulletIndex){
//             // Здесь типа надо сделать перезарядку \0-0/
//             bulletIndex = 0;
//             ++bulletIndex;
//         }else{
//             ++bulletIndex;
//         }

//         bull.SetActive(true); bull.GetComponent<bulletScript>().setTarget(enemy);
//         bull.GetComponent<Transform>().position = pos;
        

//         //GameObject bull = Instantiate(bullet, pos, Quaternion.identity, transform);
//         //bull.GetComponent<bulletScript>().setTarget(searchEnemy(enemies));
//         //bul.GetComponent<bulletScript>().setPath(new Vector2(a.GetComponent<Transform>().position.x, a.GetComponent<Transform>().position.y));
//         shootFlag = true;
//         yield return new WaitForSeconds(shootColdown);
//         }

//         // моя ф-ция для поиска ближайшего врага(горжусь ей)
//      private Transform searchEnemy(List<GameObject> enemyList){
//         float minDistance = Mathf.Infinity;
//         GameObject nearestEnemy = null;

//         foreach(GameObject enemy in enemyList){
//             if(enemy != null){
//                 float dist = Vector2.Distance(transform.position, enemy.GetComponent<Transform>().position);
//                 if(dist < minDistance){
//                     minDistance = dist;
//                     nearestEnemy = enemy;
//                 }
//             }
            
//         }
//         if(minDistance <= attackRadius){
//             if(checkObstacle(this.transform.position, nearestEnemy)){
//                 //Debug.Log("препятствие!!!");
//                 status = 0;
//             }else{
//                 stopFlag = true;
//                 status = 1;
//             }
//         }else{
//             status = 0;
//         }

//         return nearestEnemy.GetComponent<Transform>();
//     }

//     //ф-ция для определения, есть ли препятствие между ботом и врагом
//     private bool checkObstacle(Vector2 a, GameObject nearestEnemy){
//         //создаём луч между ботом и ближайшим врагом
//         RaycastHit2D hit = Physics2D.Linecast(a, nearestEnemy.GetComponent<Transform>().position); 


//         // if(hit.collider.gameObject.CompareTag("bullet")){
//         //     return false;
//         // }

//         //проверяем, в кого попали
//         if(hit.collider != null && hit.collider.gameObject != this.gameObject && hit.collider.gameObject != nearestEnemy && hit.collider.gameObject.GetComponent<Transform>().position.y >= transform.position.y +1){
//             // Debug.Log(this.gameObject);
//              //Debug.Log(hit.collider.gameObject);
//             return true; // есть препятствие
//         }else{
//             return false; //неа, тут пусто
//             //Debug.Log("All is ok");
//         }
//     }

//     IEnumerator searchCoroutine(){
//         yield return new WaitForSeconds(1);
//         //Debug.Log("COROUTINE!!!");
//         a = searchEnemy(enemies).GetComponent<Transform>();
//         flag1 = true;
//     }

//     // IEnumerator shootCoroutine(){
//     //     if(a.GetComponent<Transform>().position.x > transform.position.x){
//     //         dir = true;
//     //     }else{
//     //         dir = false;
//     //     }

//     //     Bounds bounds = a.GetComponent<Collider2D>().bounds;
//     //     float rY = Random.Range(bounds.min.y, bounds.max.y);
//     //     float rX = Random.Range(bounds.min.x, bounds.max.x);
//     //     Vector2 point = new Vector2(/*bounds.center.x*/rX, rY);
//     //     //Debug.Log(point);
//     //     Debug.Log(a);
//     //     Vector2 pos = transform.position;
//     //     if(dir){
//     //         pos.x += 2;
//     //     }else{
//     //         pos.x -= 2;
//     //     }
//     //     pos.y += 1;
        
//     //     GameObject bul = Instantiate(bullet, pos, Quaternion.identity);
//     //     // bul.GetComponent<Transform>().Rotate(point);
//     //     // bul.GetComponent<Rigidbody2D>().velocity = new Vector2(7, 0);
//     //     bul.GetComponent<Rigidbody2D>().AddForce(point, ForceMode2D.Impulse);
//     //     if(Input.GetKey(KeyCode.E)){
//     //         //transform.position = point;
//     //         transform.Rotate(point);
//     //     }
//     //     yield return new WaitForSeconds(shootColdown);
//     //     shootFlag = true;
//     // }

//     // IEnumerator secondShootCoroutine(){
//     //     Vector2 pos = transform.position;
//     //     if(dir){
//     //         pos.x += 2;
//     //     }else{
//     //         pos.x -= 2;
//     //     }
//     //     pos.y += 1;

//     //     GameObject bull = Instantiate(bullet, pos, Quaternion.identity, transform);
//     //     bull.GetComponent<bulletScript>().setTarget(searchEnemy(enemies));
//     //     //bul.GetComponent<bulletScript>().setPath(new Vector2(a.GetComponent<Transform>().position.x, a.GetComponent<Transform>().position.y));
//     //     yield return new WaitForSeconds(shootColdown);
//     //     shootFlag = true;
//     // }

//     // private Vector2 GetRandomPointInCollider(Collider2D collider)
//     // {
//     //     // Получаем границы коллайдера
//     //     Bounds bounds = collider.bounds;
//     //     // Генерируем случайные координаты внутри границ
//     //     float randomX = Random.Range(bounds.min.x, bounds.max.x);
//     //     float randomY = Random.Range(bounds.min.y, bounds.max.y);
//     //     return new Vector2(randomX, randomY);
//     // }

//     public Vector2 returnBulletPath(){
//         Bounds bounds = a.GetComponent<Collider2D>().bounds;
//         float rY = Random.Range(bounds.min.y, bounds.max.y);
//         float rX = Random.Range(bounds.min.x, bounds.max.x);
//         Vector2 point = new Vector2(/*bounds.center.x*/rX, rY);
//         //return new Vector2(a.GetComponent<Transform>().position.x, a.GetComponent<Transform>().position.y);
//         return point;
//     }

//     // ф-ция для смерти
//     public void setDeath(){
//         status = 2;
//         deathFlag = true;
//     }

//     // ф-ция для жизни
//     public void setLife(){
//         status = 0;
//         deathFlag = true;
//     }

//     public void deleteEnemy(GameObject i){
//         enemies.Remove(i);
//     }

//     public void addEnemy(GameObject i){
//         enemies.Add(i);
//     }

//     }
// }
