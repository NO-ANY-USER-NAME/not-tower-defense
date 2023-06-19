#define PLACE_CANNON_ON_PLAY
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using UnityEngine;
using TMPro;

using Random=UnityEngine.Random;

public partial class EntitiesSystem : MonoBehaviour{
    public static EntitiesSystem instance;
    #pragma warning disable CS0108
    public Camera camera;
    #pragma warning restore CS0108
    [Header("Pool")]
    public Pool bulletPool;
    public Pool enemyPool;
    public Pool ironCannonPool,goldCannonPool,diamondCannonPool;
    [Header("UI")]
    public TextMeshProUGUI coinsHave;
    public GameObject menuHolder;
    [Header("Cannon Number Text")]
    public TextMeshProUGUI ironNumberText;
    public TextMeshProUGUI goldNumberText;
    public TextMeshProUGUI diamondNumberText;
    

    public struct Spawners{
        public long[] tickZeros;
        public long[] ticksNeededs;
        public byte[] counters;
        public byte[] chances;
        public Node[] nodes;
        public Transform[] transforms;
        public int count;
    };

    public struct Enemies{
        public Transform[] transfroms;
        public SpriteRenderer[] renderers;
        public Node[] nodes;
        public float[] speeds;
        public int[] HPs;
        public int[] coins;
        public int count;
        public Dictionary<GameObject,int> getIndex;
    };

    public struct Cannons{
        public long[] tickZeros;
        public long[] ticksNeededs;
        public byte[] ranges;
        public Node[] nodes;
        public Transform[] transforms;
        public int count;
    };

    private FlowField flowField;
    private Spawners spawners;
    private Enemies enemies;
    private Cannons cannons;


    private const int LossPenalty=30;
    private const int IronPrice=100,GoldPrice=200,DiamondPrice=350;
    private int playerCoins;
    private bool gameActive;

    private int ironCannonNumber,goldCannonNumber,diamondCannonNumber;
    private int ironCannonTotal,goldCannonTotal,diamondCannonTotal;
    
    void Awake(){
        #if OOP
        return;
        #endif
        instance=this;
        flowField=GameObject.FindGameObjectWithTag("Grid").GetComponent<FlowField>();
        flowField.InitializeMap();

        gameActive=false;
        ironCannonNumber=0;
        diamondCannonNumber=0;
        goldCannonNumber=0;
        ironCannonTotal=0;
        diamondCannonTotal=0;
        goldCannonTotal=0;
        playerCoins=1000;

        long tickNow=DateTime.Now.Ticks;
        InitializeSpawners(tickNow);
        InitializeEnemies();
        InitializeCannons(tickNow);


        flowField.DisableCellFromPlaced(spawners.nodes,10);

        enemyPool.CreatePool();
        bulletPool.CreatePool();
        ironCannonPool.CreatePool();
        goldCannonPool.CreatePool();
        diamondCannonPool.CreatePool();

        coinsHave.text=playerCoins.ToString();
        ironNumberText.text="Iron:0";
        goldNumberText.text="Gold:0";
        diamondNumberText.text="Diamond:0";
    }


    void OnEnable(){
        System.GC.Collect();
    }


    private void InitializeSpawners(long tickNow){
        GameObject[] objs=GameObject.FindGameObjectsWithTag("Spawner");
        int length=objs.Length;
        
        spawners=new Spawners{
            tickZeros=new long[length],
            ticksNeededs=new long[length],
            chances=new byte[length],
            counters=new byte[length],
            transforms=new Transform[length],
            nodes=new Node[length],
            count=length,
        };

        for(int i=0;i<length;i++){
            SpawnerControl obj=objs[i].GetComponent<SpawnerControl>();
            spawners.tickZeros[i]=tickNow;
            spawners.ticksNeededs[i]=(long)(TimeSpan.TicksPerSecond*obj.time);
            spawners.chances[i]=(byte)obj.chance;
            spawners.counters[i]=0;
            spawners.transforms[i]=obj.transform;
            spawners.nodes[i]=flowField.GetNodeInMap(obj.transform.position);
            obj.transform.position=new Vector3(spawners.nodes[i].inWorld.x,spawners.nodes[i].inWorld.y,obj.transform.position.z);
        }
    }

    private void InitializeEnemies(){
        int length=enemyPool.poolSize;

        enemies=new Enemies{
            speeds=new float[length],
            nodes=new Node[length],
            HPs=new int[length],
            coins=new int[length],
            transfroms=new Transform[length],
            renderers=new SpriteRenderer[length],
            count=0,
            getIndex=new Dictionary<GameObject,int>(length*2),
        };

    }
    
    
    private void InitializeCannons(long tickNow){
        #if PLACE_CANNON_ON_PLAY
        int length=ironCannonPool.poolSize+goldCannonPool.poolSize+diamondCannonPool.poolSize;

        cannons=new Cannons{
            tickZeros=new long[length],
            ticksNeededs=new long[length],
            ranges=new byte[length],
            nodes=new Node[length],
            transforms=new Transform[length],
            count=0,
        };

        #else
        GameObject[] objs=GameObject.FindGameObjectsWithTag("Cannon");
        int length=objs.Length;

        cannons=new Cannons{
            tickZeros=new long[length],
            ticksNeededs=new long[length],
            ranges=new byte[length],
            nodes=new Node[length],
            transforms=new Transform[length],
            count=0,
        };

        for(int i=0;i<length;i++){
            CannonControl obj=objs[i].GetComponent<CannonControl>();
            AddCannon(obj.transform,obj.type,tickNow);
        }
        #endif
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddCannon(Transform tf,CannonControl.CannonType type,long tickNow){
        int count=cannons.count;
        if(type==CannonControl.CannonType.Iron){
            cannons.ticksNeededs[count]=(long)(CannonControl.IronCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=12;
        }
        else if(type==CannonControl.CannonType.Gold){
            cannons.ticksNeededs[count]=(long)(CannonControl.GoldCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=14;
        }
        else{
            cannons.ticksNeededs[count]=(long)(CannonControl.DiamondCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=16;
        }
    
        Node node=flowField.GetNodeInMap(tf.position,Node.OnTop.cannon);
        tf.position=new Vector3(node.inWorld.x,node.inWorld.y,tf.position.z);
        cannons.transforms[count]=tf;
        cannons.nodes[count]=node;
        cannons.tickZeros[count]=tickNow;

        cannons.count=count+1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddCannon(Transform tf,CannonControl.CannonType type,long tickNow,Node node){
        int count=cannons.count;
        if(type==CannonControl.CannonType.Iron){
            cannons.ticksNeededs[count]=(long)(CannonControl.IronCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=12;
        }
        else if(type==CannonControl.CannonType.Gold){
            cannons.ticksNeededs[count]=(long)(CannonControl.GoldCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=14;
        }
        else{
            cannons.ticksNeededs[count]=(long)(CannonControl.DiamondCannonRate*TimeSpan.TicksPerSecond);
            cannons.ranges[count]=16;
        }
    
        cannons.transforms[count]=tf;
        cannons.nodes[count]=node;
        cannons.tickZeros[count]=tickNow;

        cannons.count=count+1;
    }


    void Update(){
        #if OOP
        return;
        #endif
        if(Input.GetKeyDown(KeyCode.Escape)){
            menuHolder.SetActive(gameActive);
            gameActive=!gameActive;
            Time.timeScale=gameActive?1:0;
        }
        if(gameActive==false){
            return;
        }

        long ticksPassed=DateTime.Now.Ticks;
        SpawnerLogic(ticksPassed);
        EnemyLogic();
        CannonLogic(ticksPassed);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnerLogic(long tickPassed){
        int i=0;
        Action SpawnEnemy=()=>{
            GameObject newEnemy;
            if(newEnemy=enemyPool.Get()){
                spawners.counters[i]=0;
                newEnemy.transform.position=spawners.transforms[i].position;
                int HP=UnityEngine.Random.Range(EnemyControl.MinHP,EnemyControl.MaxHP);
                float speed=UnityEngine.Random.Range(EnemyControl.MinSpeed,EnemyControl.MaxSpeed);
                int count=enemies.count;

                enemies.transfroms[count]=newEnemy.transform;
                enemies.renderers[count]=newEnemy.GetComponent<SpriteRenderer>();
                enemies.HPs[count]=HP;
                enemies.speeds[count]=speed;
                enemies.nodes[count]=spawners.nodes[i];
                enemies.renderers[count].color=EnemyControl.HPcolors[HP];
                enemies.coins[count]=HP+2;

                enemies.getIndex.Add(newEnemy,count);
                enemies.count=count+1;
            }

        };


        for(;i<spawners.count;i++){
            if(tickPassed-spawners.tickZeros[i]<spawners.ticksNeededs[i]){
                continue;
            }
            spawners.tickZeros[i]=tickPassed;
            
            if(spawners.counters[i]>=4){
                SpawnEnemy();
            }
            else{
                if(Random.Range(0,10)<spawners.chances[i]){
                    SpawnEnemy();
                }
                else{
                    spawners.counters[i]++;
                }
            }
        }

    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnemyLogic(){
        for(int i=0;i<enemies.count;i++){
            Transform tf=enemies.transfroms[i];
            Node node=enemies.nodes[i];
            
            if(Mathf.Approximately(tf.position.x,node.inWorld.x)&&Mathf.Approximately(tf.position.y,node.inWorld.y)){
                if(node.potential==0){
                    HitTarget(enemies.HPs[i]*LossPenalty);
                    RemoveFromEnemies(tf.gameObject);
                }
                else{
                    enemies.nodes[i]=flowField.FlowAndSet(node);
                }
            }
            tf.position=Vector2.MoveTowards(tf.position,new Vector2(enemies.nodes[i].inWorld.x,enemies.nodes[i].inWorld.y),enemies.speeds[i]*Time.deltaTime);
        }
    }


    private void HitTarget(int amount){
        playerCoins-=amount;
        if(playerCoins<0){
            UpdatePlayerCoinsText();
        }
    }


    public void HitEnemy(GameObject enemy){
        int index;
        if(enemies.getIndex.TryGetValue(enemy,out index)){
            int HP=enemies.HPs[index];
            if(--HP<0){
                playerCoins+=enemies.coins[index];
                UpdatePlayerCoinsText();
                RemoveFromEnemies(enemy,index);
            }
            else{
                enemies.renderers[index].color=EnemyControl.HPcolors[HP];
                enemies.HPs[index]=HP;
            }
        }
    }


    private void UpdatePlayerCoinsText(){
        coinsHave.text=playerCoins.ToString();
    }


    public void RemoveFromEnemies(GameObject enemy,int index=-1){
        if(index<0&&enemies.getIndex.TryGetValue(enemy,out index)==false)return;
        int count=enemies.count-1;

        flowField.ResetNode(enemies.nodes[index]);

        if(index!=count){
            Transform tf=enemies.transfroms[count];
            enemies.getIndex[tf.gameObject]=index;
            enemies.transfroms[index]=tf;
            enemies.HPs[index]=enemies.HPs[count];
            enemies.speeds[index]=enemies.speeds[count];
            enemies.nodes[index]=enemies.nodes[count];
            enemies.coins[index]=enemies.coins[count];
        }
        
        enemies.getIndex.Remove(enemy);
        enemies.count=count;
        enemyPool.Return(enemy);
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CannonLogic(long tickPassed){
        for(int i=0;i<cannons.count;i++){
            if(tickPassed-cannons.tickZeros[i]<cannons.ticksNeededs[i]){
                continue;
            }
            cannons.tickZeros[i]=tickPassed;

            Vector3 enemyPosition;
            if(flowField.DetectEnemy(out enemyPosition,cannons.ranges[i],cannons.nodes[i])){
                enemyPosition.z=0;
                Vector3 up=(enemyPosition-cannons.transforms[i].position).normalized;
                up.z=cannons.transforms[i].position.z;
                cannons.transforms[i].up=up;

                GameObject bullet;
                if(bullet=bulletPool.Get()){
                    bullet.transform.rotation=cannons.transforms[i].rotation;
                    bullet.transform.position=cannons.transforms[i].position;
                    bullet.GetComponent<Rigidbody2D>().AddForce(cannons.transforms[i].up*1100);
                }
            }
        }
    }


}


public partial class EntitiesSystem{
    GameObject desiredCannon;
    IEnumerator wait=new WaitForSecondsRealtime(0.01f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckCoinsAmple(in int needed,in int cannonNumber,in int cannonNumberLimit,string cannonType){
        if(playerCoins<needed){
            Debug.Log("no enough coins");
            return false;
        }
        if(cannonNumber>=cannonNumberLimit){
            Debug.Log($"you cannon have more than {cannonNumberLimit} {cannonType} cannons");
            return false;
        }
        playerCoins-=needed;
        coinsHave.text=playerCoins.ToString();
        return true;
    }

    public void BuyIronButton(){
        if(CheckCoinsAmple(IronPrice,ironCannonTotal,ironCannonPool.poolSize,"iron")){
            ironCannonTotal++;
            ironCannonNumber++;
            ironNumberText.text=$"Iron:{ironCannonNumber}";
        }
    }

    public void BuyGoldButton(){
        if(CheckCoinsAmple(GoldPrice,goldCannonTotal,goldCannonPool.poolSize,"gold")){
            goldCannonTotal++;
            goldCannonNumber++;
            goldNumberText.text=$"Gold:{goldCannonNumber}";
        }
    }

    public void BuyDiamondButton(){
        if(CheckCoinsAmple(DiamondPrice,diamondCannonTotal,diamondCannonPool.poolSize,"diamond")){
            diamondCannonTotal++;
            diamondCannonNumber++;
            diamondNumberText.text=$"Diamond:{diamondCannonNumber}";
        }
    }

    public void PlaceIronButton(){
        if(ironCannonNumber<=0){
            Debug.Log("no iron cannon");
            return;
        }
        menuHolder.SetActive(false);
        StartCoroutine(DetectPosition(ironCannonPool.Get(),CannonControl.CannonType.Iron));
    }

    public void PlaceGoldButton(){
        if(goldCannonNumber<=0){
            Debug.Log("no gold cannon");
            return;
        }
        menuHolder.SetActive(false);
        StartCoroutine(DetectPosition(goldCannonPool.Get(),CannonControl.CannonType.Gold));
    }


    public void PlaceDiamondButton(){
        if(diamondCannonNumber<=0){
            Debug.Log("no diamond cannon");
            return;
        }
        menuHolder.SetActive(false);
        StartCoroutine(DetectPosition(diamondCannonPool.Get(),CannonControl.CannonType.Diamond));
    }


    IEnumerator DetectPosition(GameObject cannon,CannonControl.CannonType type){
        float z=cannon.transform.position.z;
        bool decided=false;
        Node node=default(Node);

        while(true){
            if(Input.GetKey(KeyCode.F)&&decided){
                if(type==CannonControl.CannonType.Iron){
                    ironCannonNumber--;
                    ironNumberText.text=$"Iron:{ironCannonNumber}";
                }
                else if(type==CannonControl.CannonType.Gold){
                    goldCannonNumber--;
                    goldNumberText.text=$"Gron:{goldCannonNumber}";
                }
                else{
                    diamondCannonNumber--;
                    diamondNumberText.text=$"Diamond:{diamondCannonNumber}";
                }

                node.nowType=Node.OnTop.cannon;
                node.previousType=Node.OnTop.cannon;
                AddCannon(cannon.transform,type,DateTime.Now.Ticks,node);
                flowField.DisableCellFromPlaced(node,5);
                flowField.UpdateNode(node);
                break;
            }
            else if(Input.GetKey(KeyCode.R)){
                if(type==CannonControl.CannonType.Iron){
                    ironCannonPool.Return(cannon);
                }
                else if(type==CannonControl.CannonType.Gold){
                    goldCannonPool.Return(cannon);
                }
                else{
                    diamondCannonPool.Return(cannon);
                }
                break;
            }
            else if(Input.GetKeyDown(KeyCode.Mouse0)){
                Debug.Log(Input.mousePosition);
                node=flowField.GetNodeInMap(camera.ScreenToWorldPoint(Input.mousePosition));
                if(node.potential>0&&(node.nowType==Node.OnTop.none||node.nowType==Node.OnTop.enemy)){
                    cannon.transform.position=new Vector3(node.inWorld.x,node.inWorld.y,z);
                    decided=true;
                }
            }

            yield return wait;
        }
        menuHolder.SetActive(true);
        yield break;
    }
}


