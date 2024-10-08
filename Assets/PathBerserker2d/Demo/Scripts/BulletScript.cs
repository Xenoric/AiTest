using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathBerserker2d
{
    public class BulletScript : MonoBehaviour
    {
        private Vector2 _endPoint = Vector2.zero;
        public float Speed;

        [SerializeField] private GameObject _targetObject; // Цель, в которую будет лететь пуля
        [SerializeField] private float _ySpreadMultiplier = 0.7f; // Настраиваемый множитель разброса по Y 0.7

        private Rigidbody2D _rb;

        private GameObject _shooter;
        private GoalWalker _shooterGW;

        [SerializeField] private PoolClass _pool;

        private bool _shootFlag = true;

        private float _damage = 20;

        private int _matchType;

        void Start()
        {
            Speed = 50f;
            // Выключаем пулю через 30 секунд
            //Invoke("DisableBullet", 1);


            _rb = GetComponent<Rigidbody2D>();
            

            _pool = FindObjectOfType<PoolClass>();



            if(_targetObject != null && _rb != null){
                    _endPoint = GetRandomPointAroundCenter(_targetObject);
                    Vector2 direction = (_endPoint - (Vector2)transform.position).normalized;
                    _rb.velocity = direction * Speed;
                    Debug.Log("пуля выпущена со скоростью: ");
                    Debug.Log(_rb.velocity);
            }
        }


        private void OnEnable(){
            StartCoroutine(ReturnCoroutine());
            _shootFlag = true;



            if(_targetObject != null && _rb != null){
                    _endPoint = GetRandomPointAroundCenter(_targetObject);
                    Vector2 direction = (_endPoint - (Vector2)transform.position).normalized;
                    _rb.velocity = direction * Speed;
                    Debug.Log("пуля выпущена со скоростью: ");
                    Debug.Log(_rb.velocity);
            }

            //_rb.AddForce(direction * Speed, ForceMode2D.Impulse);
        }

        IEnumerator ReturnCoroutine(){
            yield return new WaitForSeconds(1);
            _pool.ReturnObject(this.gameObject);
        }

        // Метод для получения точки с случайным смещением по оси Y от центра
        Vector2 GetRandomPointAroundCenter(GameObject _targetObject)
        {
            // Получаем границы коллайдера
            Collider2D collider = _targetObject.GetComponent<Collider2D>();
            Bounds bounds = collider.bounds;

            // Оставляем X на центре коллайдера
            float randomX = bounds.center.x;

            // Прибавляем случайное значение по оси Y
            float randomY = bounds.center.y + Random.Range(-_ySpreadMultiplier, _ySpreadMultiplier);

            // Возвращаем точку с случайным смещением по оси Y
            return new Vector2(randomX, randomY);
        }

        // Метод для выключения пули
        void DisableBullet()
        {
            gameObject.SetActive(false); // Выключаем пулю
        }

        private void OnCollisionEnter2D(Collision2D other){
            if(other.gameObject.CompareTag("player") || other.gameObject.CompareTag("enemy")){
                if(_matchType == 2){

                 if(other.gameObject.GetComponent<GoalWalker>() != null){
                     if(other.gameObject.GetComponent<GoalWalker>().ReturnTeam() != _shooterGW.ReturnTeam()){
                         //other.gameObject.SetActive(false);
                         //_shooter.GetComponent<GoalWalker>().deleteEnemy(other.gameObject);
                         other.gameObject.GetComponent<GoalWalker>().DoDamage(_damage);
                         //this.gameObject.SetActive(false);
                         //PoolClass _pool = FindObjectOfType<PoolClass>();
                         _pool.ReturnObject(this.gameObject);
                         _shootFlag = true;
                    }

                 }else{
                    // сюда запишите имя скрипта игрока(где его хп должны быть)(вместо GoalWalker)
                    if(other.gameObject.GetComponent<GoalWalker>().ReturnTeam() != _shooterGW.ReturnTeam()){
                        // сюда запишите имя скрипта игрока(где его хп должны быть)(вместо GoalWalker)
                        other.gameObject.GetComponent<GoalWalker>().DoDamage(_damage);
                        _pool.ReturnObject(this.gameObject);
                         _shootFlag = true;
                    }
                }
            }else{
                if(other.gameObject != _shooter.gameObject){
                    if(other.gameObject.GetComponent<GoalWalker>() != null){
                         other.gameObject.GetComponent<GoalWalker>().DoDamage(_damage);
                         _pool.ReturnObject(this.gameObject);
                         _shootFlag = true;
                 }else{
                        // сюда запишите имя скрипта игрока(где его хп должны быть)(вместо GoalWalker)
                        other.gameObject.GetComponent<GoalWalker>().DoDamage(_damage);
                        _pool.ReturnObject(this.gameObject);
                         _shootFlag = true;                }
                }
            }
            }
                

                
                // }
                // if(other.gameObject != _shooter){
                //         //other.gameObject.SetActive(false);
                //         //_shooter.GetComponent<GoalWalker>().deleteEnemy(other.gameObject);
                //         other.gameObject.GetComponent<GoalWalker>().DoDamage(_damage);
                //         //this.gameObject.SetActive(false);
                //         //PoolClass _pool = FindObjectOfType<PoolClass>();
                //         _pool.ReturnObject(this.gameObject);
                //         _shootFlag = true;

                // }
                
            }

        public void SetTarget(GameObject i){
            _targetObject = i;
        }

        public void SetShooter(GameObject i, int mt){
            _shooter = i;
            _shooterGW = _shooter.GetComponent<GoalWalker>();
            _matchType = mt;
        }

    }
}