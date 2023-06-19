using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerControl : MonoBehaviour{
    public Pool enemyPool;
    public FlowField flowField;

    [Range(0.1f,2f)]public float time;//time interval for trying to spawn enemy
    [Range(1,10)]public int chance;

    #if OOP
    private int counter=0;//the spawner must spawn a enemy after several trial fail to spawn
    private long tickZero,ticksPass;
    private long tickNeeded;
    private Node node;//position on graph

    void Start(){
        node=flowField.GetNodeInMap(transform.position);
        transform.position=new Vector3(node.inWorld.x,node.inWorld.y,transform.position.z);
        tickNeeded=(long)(TimeSpan.TicksPerSecond*time);
        tickZero=DateTime.Now.Ticks;
    }

    void Update(){
        ticksPass=DateTime.Now.Ticks-tickZero;
        if(ticksPass<tickNeeded)return;

        tickZero=DateTime.Now.Ticks;
        if(counter>=4){
            Spawn();
        }
        else{
            if(UnityEngine.Random.Range(0,10)<chance){
                Spawn();
            }
            else{
                counter++;
            }
        }
    }

    private void Spawn(){
        GameObject newEnemy;
        if(newEnemy=enemyPool.Get()){
            counter=0;
            newEnemy.transform.position=transform.position;
            newEnemy.GetComponent<EnemyControl>().SetStatus(UnityEngine.Random.Range(EnemyControl.MinHP,EnemyControl.MaxHP),UnityEngine.Random.Range(EnemyControl.MinSpeed,EnemyControl.MaxSpeed),node);
        }
    }
    #endif
}
