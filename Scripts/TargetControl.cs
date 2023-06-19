using UnityEngine;
using TMPro;

public class TargetControl : MonoBehaviour{
    public TextMeshProUGUI text;
    private int hit=0;

    public void BeingHit(){
        hit++;
        //Debug.Log(hit);
        //text.text=hit.ToString();
    }
}