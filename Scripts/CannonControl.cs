using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class CannonControl : MonoBehaviour{
    public const double GoldCannonRate=0.125,DiamondCannonRate=0.075,IronCannonRate=0.15;
    public enum CannonType{
        Iron,Gold,Diamond,
    }
    public Pool bulletPool;
    public FlowField flowField;

    public CannonType type;

    #if OOP
    private long tickZero,ticksPass;
    private long tickNeeded;
    private int range;
    private Node node;//cannon position on graph (grid based)

    void Start(){
        if(type==CannonType.Iron){
            tickNeeded=(long)(0.2*TimeSpan.TicksPerSecond);
            range=12;
        }
        else if(type==CannonType.Gold){
            tickNeeded=(long)(0.15*TimeSpan.TicksPerSecond);
            range=14;
        }
        else{
            tickNeeded=(long)(0.1*TimeSpan.TicksPerSecond);
            range=16;
        }
        tickZero=DateTime.Now.Ticks;
        node=flowField.GetNodeInMap(transform.position,Node.OnTop.cannon);
        transform.position=new Vector3(node.inWorld.x,node.inWorld.y,transform.position.z);
    }

    void Update(){
        ticksPass=DateTime.Now.Ticks-tickZero;
        if(ticksPass<tickNeeded)return;

        tickZero=DateTime.Now.Ticks;
        
        Vector3 enemyPosition;
        if(flowField.DetectEnemy(out enemyPosition,range,node)){
            enemyPosition.z=0;
            Vector3 up=(enemyPosition-transform.position).normalized;
            up.z=transform.position.z;
            transform.up=up;
            Shoot();
        }
    }

    private void Shoot(){
        GameObject bullet;
        if(bullet=bulletPool.Get()){
            bullet.transform.rotation=transform.rotation;
            bullet.transform.position=transform.position;
            bullet.GetComponent<Rigidbody2D>().AddForce(transform.up*1100);
        }
        else{
            Debug.Log("no ammo");
        }
    }
    #endif
}
