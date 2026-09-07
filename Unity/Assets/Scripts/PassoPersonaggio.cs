using UnityEngine;

namespace AmnesiaUnity
{
    // Only Giorgio owns this component. NPCs retain their authored standing/seated poses.
    public sealed class PassoPersonaggio : MonoBehaviour
    {
        public Transform AncaSinistra, AncaDestra, GinocchioSinistro, GinocchioDestro;
        public Transform SpallaSinistra, SpallaDestra, GomitoSinistro, GomitoDestro;
        float phase, amount;
        Vector3 previous;
        void OnEnable() { previous=transform.position; ResetPose(); }
        void LateUpdate()
        {
            Vector3 movement=transform.position-previous;previous=transform.position;movement.y=0;
            Advance(movement,Time.deltaTime);
        }
        public void Advance(Vector3 movement,float seconds)
        {
            if(seconds<=0) return;
            movement.y=0;float distance=movement.magnitude;
            if(distance>1.5f) { ResetPose(); return; }
            float speed=distance/seconds;
            amount=Mathf.MoveTowards(amount,Mathf.Clamp01(speed/2.2f),seconds*7);
            float direction=Vector3.Dot(movement,-transform.forward)<0?-1:1;
            // The exploration controller is fast; cap cadence to avoid frantic leg cycling.
            phase+=Mathf.Min(distance*4.2f,seconds*12f)*direction;
            float swing=Mathf.Sin(phase)*amount;
            Rotate(AncaSinistra,swing*24);Rotate(AncaDestra,-swing*24);
            Rotate(GinocchioSinistro,-Mathf.Max(0,-Mathf.Sin(phase))*27*amount);
            Rotate(GinocchioDestro,-Mathf.Max(0,Mathf.Sin(phase))*27*amount);
            Rotate(SpallaSinistra,-swing*17);Rotate(SpallaDestra,swing*17);
            Rotate(GomitoSinistro,-Mathf.Abs(swing)*9);Rotate(GomitoDestro,-Mathf.Abs(swing)*9);
        }
        static void Rotate(Transform joint,float angle) { if(joint) joint.localRotation=Quaternion.Euler(angle,0,0); }
        public void ResetPose()
        {
            phase=amount=0;
            foreach(var joint in new[]{AncaSinistra,AncaDestra,GinocchioSinistro,GinocchioDestro,SpallaSinistra,SpallaDestra,GomitoSinistro,GomitoDestro}) Rotate(joint,0);
        }
        void OnDisable() { ResetPose(); }
    }
}
