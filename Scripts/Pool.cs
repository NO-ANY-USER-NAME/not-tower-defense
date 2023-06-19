using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour{
    public GameObject obj;
    public int poolSize;

    private List<GameObject> pool;
    int top;

    #if OOP
    void Awake(){
        CreatePool();
    }
    #endif

    public void CreatePool(){
        pool=new List<GameObject>(poolSize);
        top=poolSize;

        obj.transform.SetParent(transform);
        obj.SetActive(false);
        pool.Add(obj);

        int i;

        for(i=1;i<poolSize;i++){
            GameObject newObj=Instantiate(obj,transform);
            newObj.SetActive(false);
            pool.Add(newObj);
        }
    }

    public GameObject Get(){
        if(top<=0){
            return null;
        }
        else{
            top--;
            GameObject obj=pool[top];
            pool.RemoveAt(top);
            obj.SetActive(true);
            return obj;
        }
    }

    public void Return(GameObject obj){
        obj.SetActive(false);
        pool.Add(obj);
        top++;
    }
}
