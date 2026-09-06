using UnityEngine;

namespace AmnesiaUnity
{
    /// Bridge authored transforms to existing logical place IDs without clamping
    /// world metres into the unrelated legacy grid.
    public sealed class LuoghiScena1987 : MonoBehaviour
    {
        public Bootstrap Gioco;
        public Transform Giocatore;
        Luogo1987[] luoghi;
        void Start() { luoghi=FindObjectsByType<Luogo1987>(FindObjectsSortMode.None); }
        void LateUpdate()
        {
            if(Gioco==null||Gioco.Session==null||Gioco.Map==null) return;
            foreach(var actor in Gioco.Corpi) Synchronize(actor.Key,actor.Value.position);
            if(Giocatore) Synchronize("player",Giocatore.position);
        }
        void Synchronize(string id,Vector3 position)
        {
            foreach(var luogo in luoghi) {
                var p=luogo.transform.InverseTransformPoint(position);
                if(Mathf.Abs(p.x)>luogo.Dimensioni.x/2 || Mathf.Abs(p.z)>luogo.Dimensioni.y/2 || p.y < -1 || p.y > 10) continue;
                var cell=Gioco.Map.CenterOf(luogo.Id);
                if(!cell.HasValue) continue;
                Gioco.Session.World.ActorOf(id).Position=cell;
                return;
            }
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SanRocco1987") {
                string outdoor = new Vector2(position.x,position.z).magnitude<19 ? "piazza" :
                    (position.x>-65 && position.x<15 && position.z>85 && position.z<130 ? "castagneto" : null);
                Gioco.Session.World.ActorOf(id).Position=outdoor==null?null:Gioco.Map.CenterOf(outdoor);
            } else Gioco.Session.World.ActorOf(id).Position=null;
        }
    }
}
