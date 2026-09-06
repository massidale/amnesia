using UnityEngine;

namespace AmnesiaUnity
{
    public sealed class Respiro1987 : MonoBehaviour
    {
        Transform busto; Vector3 origine;
        void Start() { busto=transform.Find("busto"); if(busto) origine=busto.localScale; }
        void Update() { if(busto) busto.localScale=new Vector3(origine.x,origine.y*(1+Mathf.Sin(Time.time*1.6f+transform.position.x)*.008f),origine.z); }
    }
}
