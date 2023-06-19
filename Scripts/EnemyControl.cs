using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyControl : MonoBehaviour{
    public const int MinHP=3,MaxHP=8;
    public const float MinSpeed=2f,MaxSpeed=7.5f;
    public static TargetControl targetControl;
    public static Color[] HPcolors;
    
    static EnemyControl(){
        HPcolors=new Color[MaxHP];
        int i;
        for(i=0;i<MaxHP;i++){
            float r=(i+1)/5f;
            float g=(MaxHP-i-1)/5f;
            HPcolors[i]=new Color(r,g,0f);
        }
    }


    public Pool enemyPool;
    public FlowField flowField;

    public SpriteRenderer render;//change the color of triangle when hp drop

    #if OOP
    private Node node;//current position on the graph (grid based)
    private int HP;
    private float speed;

    public void SetStatus(int _HP,float _speed,Node _node){
        HP=_HP;
        speed=_speed;
        node=_node;
        render.color=HPcolors[_HP];
    }

    private void Update(){
        if(Mathf.Approximately(transform.position.x,node.inWorld.x)&&Mathf.Approximately(transform.position.y,node.inWorld.y)){
            if(node.potential==0){
                targetControl.BeingHit();
                ReturnPool();
            }
            else{
                node=flowField.FlowAndSet(node);
            }
        }
        transform.position=Vector2.MoveTowards(transform.position,new Vector2(node.inWorld.x,node.inWorld.y),speed*Time.deltaTime);
    }


    public void DecreaseHP(){
        HP--;
        if(HP<0){
            ReturnPool();
        }
        else{
            render.color=HPcolors[HP];
        }
    }

    private void ReturnPool(){
        flowField.ResetNode(node);
        enemyPool.Return(gameObject);
    }
    #endif
}
