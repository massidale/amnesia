using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    public sealed class EtichettaMappa : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Text label;
        bool hovered;
        float amount;
        void Awake() { label=GetComponent<Text>(); }
        public void OnPointerEnter(PointerEventData data) { hovered=true; }
        public void OnPointerExit(PointerEventData data) { hovered=false; }
        void OnDisable() { hovered=false;amount=0;transform.localScale=Vector3.one; }
        void Update()
        {
            amount=Mathf.MoveTowards(amount,hovered?1:0,Time.unscaledDeltaTime*7);
            label.color=Color.Lerp(new Color(.81f,.85f,.82f,.85f),new Color(1,.88f,.62f,1),amount);
            transform.localScale=Vector3.one*(1+.04f*amount);
        }
    }
}
