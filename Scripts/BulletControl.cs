using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletControl:MonoBehaviour{
    #if OOP
    public Pool bulletPool;
    #endif

    /*
    private void OnTriggerEnter2D(Collider2D other){
        if(other.gameObject.CompareTag("Enemy")){
            #if OOP
            other.gameObject.GetComponent<EnemyControl>().DecreaseHP();
            #else
            EntitiesSystem.instance.HitEnemy(other.gameObject);
            #endif
        }
        #if OOP
        bulletPool.Return(gameObject);
        #else
        EntitiesSystem.instance.bulletPool.Return(gameObject);
        #endif
    }
    */

    private void OnTriggerEnter2D(Collider2D other){
        EntitiesSystem.instance.bulletPool.Return(gameObject);
    }
}
