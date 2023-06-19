using System.Runtime.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.Tilemaps;
using Unity.Mathematics;

public struct Node{
    public enum OnTop{
        enemy,target,cannon,obstacle,none,blocked
    }
    public int2 inMap;
    public float2 inWorld;
    public short potential;
    public OnTop nowType,previousType;
}


public class FlowField : MonoBehaviour{
    public static Color blockedColor,unblockedColor;
    static FlowField(){
        blockedColor=new Color(0.25f,0.25f,0.25f);
        unblockedColor=new Color(0.35f,0.35f,0.35f);
    }

    public bool showNode;
    public LayerMask obstacle;
    [Header("Tilemap related")]
    public Tilemap obstacleMap;
    public Tilemap backgroundMap;
    public Grid grid;
    [Header("GameObject")]
    public GameObject target;
    

    public NativeArray<Node> map;

    private Vector3Int mapSize;
    private Vector3 cellSize;
    private Queue<int2> queue=new Queue<int2>();
    private Stack<int> path=new Stack<int>();
    private Func<int,int,int> GetIndex;
    private bool[] visited;
    private int targetIndex;

    #if OOP
    void Awake(){
        InitializeMap();
    }
    #endif


    public void InitializeMap(){
        int i,j;
        Vector3 bottomLeft=grid.CellToWorld(obstacleMap.origin);
        Vector3Int bottomLeftInt=obstacleMap.origin;

        EnemyControl.targetControl=target.GetComponent<TargetControl>();

        cellSize=grid.cellSize;
        mapSize=obstacleMap.size;
        bottomLeft+=cellSize/2;

        GetIndex=(int i,int j)=>(
            (i)*mapSize.x+(j)
        );

        map=new NativeArray<Node>(mapSize.x*mapSize.y,Allocator.Persistent);
        visited=new bool[mapSize.x*mapSize.y];

        for(i=0;i<mapSize.y;i++){
            for(j=0;j<mapSize.x;j++){
                if(Physics2D.OverlapPoint(new Vector2(j*cellSize.x+bottomLeft.x,i*cellSize.y+bottomLeft.y),obstacle)){
                    map[GetIndex(i,j)]=new Node{
                        inMap=new int2(j,i),
                        inWorld=new float2(j*cellSize.x+bottomLeft.x,i*cellSize.y+bottomLeft.y),
                        potential=-1,
                        nowType=Node.OnTop.obstacle,
                        previousType=Node.OnTop.obstacle,
                    };
                }
                else{
                    map[GetIndex(i,j)]=new Node{
                        inMap=new int2(j,i),
                        inWorld=new float2(j*cellSize.x+bottomLeft.x,i*cellSize.y+bottomLeft.y),
                        potential=short.MaxValue,
                        nowType=Node.OnTop.none,
                        previousType=Node.OnTop.none,
                    };

                    Vector3Int pos=new Vector3Int(j+bottomLeftInt.x,i+bottomLeftInt.y,bottomLeftInt.z);
                    backgroundMap.SetTileFlags(pos,TileFlags.None);
                    backgroundMap.SetColor(pos,unblockedColor);
                }
            }
        }
        
        //SetPosition(obstacleMap.size,obstacleMap.origin,obstacleMap.origin+obstacleMap.size);

        Vector3Int alignedPosition=grid.WorldToCell(target.transform.position);
        target.transform.position=alignedPosition+cellSize/2;

        alignedPosition-=obstacleMap.origin;
        targetIndex=GetIndex(alignedPosition.y,alignedPosition.x);
        Node node=map[targetIndex];
        DisableCellFromPlaced(node,8);
        node.potential=0;
        node.previousType=Node.OnTop.target;
        node.nowType=Node.OnTop.target;
        map[targetIndex]=node;

        
        int2 front;
        int count;
        short step=0;
        Action<int,int> TryEnqueue=(i,j)=>{
            if(map[GetIndex(i,j)].potential==short.MaxValue){
                node=map[GetIndex(i,j)];
                node.potential=step;
                map[GetIndex(i,j)]=node;
                queue.Enqueue(new int2(j,i));
            }
        };

        queue.Enqueue(new int2(alignedPosition.x,alignedPosition.y));
        while((count=queue.Count)>0){
            for(step++;count>0;count--){
                front=queue.Dequeue();
                TryEnqueue(front.y+1,front.x);
                TryEnqueue(front.y-1,front.x);
                TryEnqueue(front.y,front.x+1);
                TryEnqueue(front.y,front.x-1);
            }
        }
    }


    public void DisableCellFromPlaced(Node source,in int limit){
        path.Clear();
        queue.Clear();
        int next=GetIndex(source.inMap.y,source.inMap.x);
        visited[next]=true;
        path.Push(next);

        Node node=map[next];
        node.previousType=Node.OnTop.blocked;
        if(node.nowType==Node.OnTop.none)node.nowType=Node.OnTop.blocked;
        map[next]=node;
        queue.Enqueue(source.inMap);

        DisableCellFromPlacedHelper(limit);
    }


    public void DisableCellFromPlaced(Node[] spawners,in int limit){
        path.Clear();
        queue.Clear();

        for(int i=0;i<spawners.Length;i++){
            int next=GetIndex(spawners[i].inMap.y,spawners[i].inMap.x);
            visited[next]=true;
            path.Push(next);

            Node node=map[next];
            node.previousType=Node.OnTop.blocked;
            node.nowType=Node.OnTop.blocked;
            map[next]=node;
            queue.Enqueue(spawners[i].inMap);
        }

        DisableCellFromPlacedHelper(limit);
    }


    private void DisableCellFromPlacedHelper(in int limit){
        Node node;
        int step,next,count;
        step=0;
        int2 front;
        Vector3Int bottomLeftInt=obstacleMap.origin;

        Action<int,int> TrySet=(x,y)=>{
            next=GetIndex(y,x);
            if(map[next].potential>0&&visited[next]==false){
                node=map[next];
                if(node.previousType==Node.OnTop.none){
                    node.previousType=Node.OnTop.blocked;
                }
                if(node.nowType==Node.OnTop.none){
                    node.nowType=Node.OnTop.blocked;
                }
                map[next]=node;
                path.Push(next);
                visited[next]=true;
                queue.Enqueue(new int2(x,y));
                backgroundMap.SetColor(new Vector3Int(x+bottomLeftInt.x,y+bottomLeftInt.y,bottomLeftInt.z),blockedColor);
            }
        };

        while((count=queue.Count)>0&&step<limit){
            for(step++;count>0;count--){
                front=queue.Dequeue();
                TrySet(front.x,front.y+1);
                TrySet(front.x,front.y-1);
                TrySet(front.x+1,front.y);
                TrySet(front.x-1,front.y);
            }
        }

        while(path.Count>0){
            visited[path.Pop()]=false;
        }
        queue.Clear();
    }


    public bool DetectEnemy(out Vector3 enemyPosition,int range,Node _start){
        Vector2 start=new Vector2(_start.inWorld.x,_start.inWorld.y);
        int2 front=_start.inMap;
        queue.Clear();
        queue.Enqueue(front);
        visited[GetIndex(front.y,front.x)]=true;
        path.Push(GetIndex(front.y,front.x));

        int count,step=0;
        bool find=false;
        
        while((count=queue.Count)>0&&step<range){
            for(step++;count>0;count--){
                front=queue.Dequeue();
                
                int next=GetIndex(front.y+1,front.x);
                if(map[next].potential>=0&&visited[next]==false){
                    if(map[next].nowType==Node.OnTop.enemy&&(Physics2D.Linecast(new Vector2(map[next].inWorld.x,map[next].inWorld.y),start,obstacle)==false)){
                        enemyPosition=new Vector3(map[next].inWorld.x,map[next].inWorld.y,transform.position.z);
                        find=true;
                        goto Finish;
                    }
                    
                    visited[next]=true;
                    path.Push(next);
                    queue.Enqueue(new int2(front.x,front.y+1));
                }

                next=GetIndex(front.y-1,front.x);
                if(map[next].potential>=0&&visited[next]==false){
                    if(map[next].nowType==Node.OnTop.enemy&&(Physics2D.Linecast(new Vector2(map[next].inWorld.x,map[next].inWorld.y),start,obstacle)==false)){
                        enemyPosition=new Vector3(map[next].inWorld.x,map[next].inWorld.y,transform.position.z);
                        find=true;
                        goto Finish;
                    }
                    
                    visited[next]=true;
                    path.Push(next);
                    queue.Enqueue(new int2(front.x,front.y-1));
                }

                next=GetIndex(front.y,front.x+1);
                if(map[next].potential>=0&&visited[next]==false){
                    if(map[next].nowType==Node.OnTop.enemy&&(Physics2D.Linecast(new Vector2(map[next].inWorld.x,map[next].inWorld.y),start,obstacle)==false)){
                        enemyPosition=new Vector3(map[next].inWorld.x,map[next].inWorld.y,transform.position.z);
                        find=true;
                        goto Finish;
                    }
                   
                    visited[next]=true;
                    path.Push(next);
                    queue.Enqueue(new int2(front.x+1,front.y));
                }

                next=GetIndex(front.y,front.x-1);
                if(map[next].potential>=0&&visited[next]==false){
                    if(map[next].nowType==Node.OnTop.enemy&&(Physics2D.Linecast(new Vector2(map[next].inWorld.x,map[next].inWorld.y),start,obstacle)==false)){
                        enemyPosition=new Vector3(map[next].inWorld.x,map[next].inWorld.y,transform.position.z);
                        find=true;
                        goto Finish;
                    }
                    
                    visited[next]=true;
                    path.Push(next);
                    queue.Enqueue(new int2(front.x-1,front.y));
                }
            }
        }
        enemyPosition=default(Vector3);

        Finish:
        while(path.Count>0){
            visited[path.Pop()]=false;
        }
        return find;
    }

    public Node GetNodeInMap(Vector3 position,Node.OnTop _type=Node.OnTop.none){
        Vector3Int cell=grid.WorldToCell(position)-obstacleMap.origin;
        int i=GetIndex(cell.y,cell.x);
        if(i>=map.Length){
            return map[0];
        }
        Node node=map[i];
        node.previousType=node.nowType;
        node.nowType=_type;
        map[i]=node;
        return node;
    }

    public Node GetNodeInMap(Vector3 position){
        Vector3Int cell=grid.WorldToCell(position)-obstacleMap.origin;
        int i=GetIndex(cell.y,cell.x);
        if(i>=map.Length){
            return map[0];
        }
        return map[i];
    }


    public void OnDrawGizmos(){
        if(showNode){
            Handles.BeginGUI();
            Handles.color=Color.white;
            int i,j;
            float z=transform.position.z;
            Vector3 pos;

            GUIStyle style=new GUIStyle();
            style.alignment=TextAnchor.MiddleCenter;
            style.normal.textColor=Color.white;
            for(i=0;i<mapSize.y;i++){
                for(j=0;j<mapSize.x;j++){
                    pos=new Vector3(map[GetIndex(i,j)].inWorld.x,map[GetIndex(i,j)].inWorld.y,z);
                    //Handles.DrawWireCube(pos,cellSize);
                    Handles.Label(pos,string.Format("{0}",map[GetIndex(i,j)].nowType),style);
                }
            }
            Handles.EndGUI();
        }
    }


    int[] choices=new int[4];
    public Node FlowAndSet(Node start){
        short potential=start.potential;
        int choiceLength=0,next=GetIndex(start.inMap.y+1,start.inMap.x),revert=4;

        if(map[next].nowType!=Node.OnTop.enemy&&map[next].nowType!=Node.OnTop.obstacle){
            if(map[next].potential<potential)
                choices[choiceLength++]=next;
            else
                choices[--revert]=next;
        }
        next=GetIndex(start.inMap.y-1,start.inMap.x);
        if(map[next].nowType!=Node.OnTop.enemy&&map[next].nowType!=Node.OnTop.obstacle){
            if(map[next].potential<potential)
                choices[choiceLength++]=next;
            else
                choices[--revert]=next;
        }
        next=GetIndex(start.inMap.y,start.inMap.x+1);
        if(map[next].nowType!=Node.OnTop.enemy&&map[next].nowType!=Node.OnTop.obstacle){
            if(map[next].potential<potential)
                choices[choiceLength++]=next;
            else
                choices[--revert]=next;
        }
        next=GetIndex(start.inMap.y,start.inMap.x-1);
        if(map[next].nowType!=Node.OnTop.enemy&&map[next].nowType!=Node.OnTop.obstacle){
            if(map[next].potential<potential)
                choices[choiceLength++]=next;
            else
                choices[--revert]=next;
        }

        if(revert==4&&choiceLength==0){
            return start;
        }

        int choice;
        if(choiceLength==0){
            choice=choices[UnityEngine.Random.Range(revert,4)];
        }
        else{
            choice=choices[UnityEngine.Random.Range(0,choiceLength)];
        }
        
        Node node;
        next=GetIndex(start.inMap.y,start.inMap.x);

        node=map[next];
        node.nowType=node.previousType;
        map[next]=node;

        node=map[choice];
        node.previousType=node.nowType;
        node.nowType=Node.OnTop.enemy;
        map[choice]=node;

        return node;
    }


    public void ResetNode(Node node){
        int index=GetIndex(node.inMap.y,node.inMap.x);
        node.nowType=node.previousType;
        map[index]=node;
    }


    public void UpdateNode(Node node){
        int i=GetIndex(node.inMap.y,node.inMap.x);
        map[i]=node;
    }


    void OnDestroy(){
        try{map.Dispose();}catch{}
    }
}
