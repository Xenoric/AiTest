﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathBerserker2d
{
    public class GoalWalker : MonoBehaviour
    {
        [SerializeField] private NavAgent _navAgent;
        [SerializeField] private Transform _currentGoal;
        [SerializeField] private Transform _enemyTransform;
        private bool _isFacingRight = false; // false - left, true = right

        [SerializeField] private EnemiesListController ELC;
        public List<GameObject> enemiesList;
        [SerializeField] private int _myTeam = 1; // Private backing field
        [SerializeField] private float _health = 100f;
        private float _damage = 20f;

        [SerializeField] private GameObject _bulletPrefab;
        [SerializeField] private int _currentStatus = 0;
        private bool _isSearching = true;
        private bool _isStopped = true;
        private bool _canShoot = true;
        private bool _canEditPath = true;
        private float _shootCooldown = 0.5f;
        [SerializeField] private float _attackRadius = 10f;
        private bool _isAlive = true;

        private bool _listFlag = true;

        // пул
        [SerializeField] private PoolClass _bulletPool;

        private Vector3 pos;

        public void SetShootCooldown(float cooldown) => _shootCooldown = cooldown;

        private void Start()
        {
            // enemiesList = new List<GameObject>(GameObject.FindGameObjectsWithTag("player"));
            // enemiesList.Remove(this.gameObject);
            // ELC = FindObjectOfType<EnemiesListController>();
            // if(_myTeam == 1){
            //     enemiesList = new List<GameObject>(ELC.ReturnFirstTeam());
            // }else{
            //     enemiesList = new List<GameObject>(ELC.ReturnSecondTeam());
            // }
            //enemiesList.Remove(this.gameObject);
        }

        private void Update()
        {   
            if(_listFlag){
                ELC = FindObjectOfType<EnemiesListController>();
                if(ELC.ReturnMatchType() == 2){
                    if(_myTeam == 1){
                    enemiesList = new List<GameObject>(ELC.ReturnSecondTeam());
                }else{
                    enemiesList = new List<GameObject>(ELC.ReturnFirstTeam());
                }
            }else{
                _myTeam = 1;
                enemiesList = new List<GameObject>(ELC.ReturnFirstTeam());
            }
                
                enemiesList.Remove(this.gameObject);
            }else if(enemiesList.Count != 0 && _listFlag){
                _listFlag = false;
            }
            

            if (_currentStatus == 0)
            {
                _enemyTransform = FindNearestEnemy(enemiesList)?.transform;
                if (_enemyTransform != null) 
                    _currentGoal = _enemyTransform;
            }
            else if (_currentStatus == 1 && _isAlive)
            {

                _navAgent.UpdatePath(transform.position);
                _currentGoal = this.transform;
                if (_isSearching){
                    StartCoroutine(SearchCoroutine());
                    _isSearching = false;
                } 
                if (_canShoot)
                {
                    StartCoroutine(ShootCoroutine2());
                    _isFacingRight = _enemyTransform.position.x > transform.position.x;
                    _canShoot = false;
                }
            }
            else if (_currentStatus == 2)
            {
                if (_isAlive)
                {
                    _isAlive = false;
                    _currentGoal = null;
                    _enemyTransform = null;
                    _navAgent.UpdatePath(transform.position);
                }
            }else if(_currentStatus == 3){
                // _currentGoal = this.gameObject.transform;
                // _currentGoal.position = pos;
                _navAgent.UpdatePath(pos);

                if (_isSearching){
                    StartCoroutine(SearchCoroutine());
                    _isSearching = false;
                } 
            }

            if(_canEditPath){
                if (_currentGoal != null && Vector2.Distance(_currentGoal.position, _navAgent.transform.position) > 0.5f && (_navAgent.IsIdle || _currentGoal.hasChanged))
                {
                    _currentGoal.hasChanged = false;
                    _navAgent.UpdatePath(_currentGoal.position);

                    _canEditPath = false;
                    StartCoroutine(CanEditPathCoroutine());
                }
            }

            
        }

        IEnumerator CanEditPathCoroutine(){
            yield return new WaitForSeconds(0.17f);
            _canEditPath = true;
        }

        private GameObject FindNearestEnemy(List<GameObject> enemyList)
        {
            if(_currentStatus != 2){
                float minDistance = Mathf.Infinity;
            GameObject nearestEnemy = null;

            foreach (GameObject enemy in enemyList)
            {
                if (enemy != null)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < minDistance)
                    {
                        if(distance <= 3){
                            //if(transform.position == enemy.transform.position){
                            if(enemy.GetComponent<GoalWalker>() != null){
                                enemy.GetComponent<GoalWalker>().GoTo(false);
                                GoTo(true);
                            }else{
                                bool Boolean  = (Random.value > 0.5f);
                                GoTo(Boolean);
                                //minDistance = distance;
                                //nearestEnemy = enemy; 
                            }
                        }else{
                           minDistance = distance;
                            nearestEnemy = enemy; 
                        }
                    }
                }
            }
            if(_health <= 0){
                _currentStatus = 2;
                return nearestEnemy;
            }

            if(minDistance <= 3){
                _currentStatus = 0;
                //nearestEnemy.transform.position = new Vector3(nearestEnemy.transform.position.x + 3, nearestEnemy.transform.position.y, nearestEnemy.transform.position.z);
                //_enemyTransform.position = new Vector3(_enemyTransform.position.x + 3, _enemyTransform.position.y, _enemyTransform.position.z);
                int i = _isFacingRight ? 5 : -5;
                Vector3 pos = new Vector3(_enemyTransform.position.x + i, _enemyTransform.position.y, _enemyTransform.position.z);
                _navAgent.UpdatePath(pos);
                //_currentGoal = pos;
                _currentStatus = 3;
                return nearestEnemy;
            }

            if (minDistance <= _attackRadius && minDistance > 3 && !HasObstacleBetween(transform.position, nearestEnemy))
            {
                _isStopped = true;
                _currentStatus = 1;
            }
            else
            {
                _currentStatus = 0;
            }

            return nearestEnemy;
        }else{
            return null;
        }
            
        }

        private bool HasObstacleBetween(Vector2 position, GameObject nearestEnemy)
    {
        // Выполняем линию от текущей позиции до позиции врага
        RaycastHit2D hit = Physics2D.Linecast(position, nearestEnemy.transform.position);
    
        // Проверяем, попала ли линия в что-то
        if (hit.collider != null)
        {
            // Убедимся, что попавший объект не является текущим объектом или ближайшим врагом
            return hit.collider.gameObject != this.gameObject && hit.collider.gameObject != nearestEnemy;
        }
    
        // Если нет попадания, значит, препятствий нет
        return false;
    }

    // public void StopBot(){
    //     StartCoroutine(StopCoroutine());
    // }

    private void GoTo(bool a){
        StartCoroutine(ClearStatusCoroutine());
        int i = a ? 5 : -5;
        pos = new Vector3(transform.position.x + i, transform.position.y, transform.position.z);
        _navAgent.UpdatePath(pos);
    }

    IEnumerator ClearStatusCoroutine(){
        _currentStatus = 999;
        yield return new WaitForSeconds(0.5f);
        _currentStatus = 0;
    }

    // IEnumerator StopCoroutine(){
    //     _currentGoal = null;
    //     _navAgent.UpdatePath(transform.position);
    //     _currentStatus = 999;
    //     yield return new WaitForSeconds(0.5f);
    //     _currentStatus = 0;
    // }

        IEnumerator SearchCoroutine()
        {
            yield return new WaitForSeconds(1);
            _enemyTransform = FindNearestEnemy(enemiesList)?.transform;
            _isSearching = true;
        }


        // IEnumerator ShootCoroutine()
        // {
        //     Vector2 bulletSpawnPosition = transform.position;
        //     bulletSpawnPosition.x += _isFacingRight ? 2 : -2;
        //     bulletSpawnPosition.y += 1;

        //     GameObject bulletInstance = Instantiate(_bulletPrefab, bulletSpawnPosition, Quaternion.identity, transform);
        //     bulletInstance.GetComponent<BulletScript>().SetTarget(FindNearestEnemy(enemiesList));
        //     yield return new WaitForSeconds(_shootCooldown);
        //     _canShoot = true;
        // }

        IEnumerator ShootCoroutine2()
        {
            Vector2 bulletSpawnPosition = transform.position;
            bulletSpawnPosition.x += _isFacingRight ? 2 : -2;
            bulletSpawnPosition.y += 1;

            GameObject _bullet = _bulletPool.GetObject();
            if(_bullet != null){
                _bullet.GetComponent<BulletScript>().SetTarget(FindNearestEnemy(enemiesList));
                _bullet.transform.position = bulletSpawnPosition;
                //_bullet.transform.rotation = transform.rotation;
                _bullet.GetComponent<BulletScript>().SetShooter(this.gameObject, ELC.ReturnMatchType());
                //_bullet.GetComponent<BulletScript>().Shoot();
            }
            yield return new WaitForSeconds(_shootCooldown);
            _canShoot = true;
        }


        public Vector2 GetBulletPath()
        {
            Bounds enemyBounds = _enemyTransform?.GetComponent<Collider2D>()?.bounds ?? new Bounds();
            return new Vector2(Random.Range(enemyBounds.min.x, enemyBounds.max.x), Random.Range(enemyBounds.min.y, enemyBounds.max.y));
        }


            public void deleteEnemy(GameObject i){
                enemiesList.Remove(i);
        }

        public void addEnemy(GameObject i){
            enemiesList.Add(i);
        }

        public void DoDamage(float i){
            _health -= i;
            if(_health <= 0){
                ELC.RemoveFromTeam(_myTeam, this.gameObject);
                _currentStatus = 2;
                _listFlag = true;
                GetComponent<Collider2D>().enabled = false;        
            }  
        }

        public int ReturnTeam(){
            return _myTeam;
        }

        public void DoDamageToMe(float _damage){
            _health -= _damage;
            if(_health <= 0){
                ELC.RemoveFromTeam(_myTeam, this.gameObject);
                _listFlag = true;
            }
        }

    }
}