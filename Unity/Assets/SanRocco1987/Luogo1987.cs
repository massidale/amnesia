using UnityEngine;

namespace AmnesiaUnity
{
    /// Semantic identity stays attached to the movable prefab.
    public sealed class Luogo1987 : MonoBehaviour
    {
        public string Id;
        public string Nome;
        public Vector2 Dimensioni;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.3f,.8f,.7f,.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0,1.3f,0), new Vector3(Dimensioni.x,2.6f,Dimensioni.y));
        }
    }
}
