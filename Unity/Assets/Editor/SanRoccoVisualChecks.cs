using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class VisualChecks
    {
        [MenuItem("Amnesia/San Rocco 1987/Verifica abitanti e case")]
        public static void CheckPeopleAndRooms()
        {
            var actors=UnityEngine.Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None);
            if(actors.Length!=12 || actors.Select(a=>a.Id).Distinct().Count()!=12) throw new Exception("Attesi 12 abitanti distinti");
            foreach(var actor in actors) {
                if(actor.Id=="teresa") {
                    var bench=GameObject.Find("panchina_teresa_piazza").transform;
                    if(Vector3.Distance(actor.transform.position,bench.TransformPoint(new Vector3(0,.025f,0)))>.01f)
                        throw new Exception("Teresa deve sedere sulla panchina della piazza");
                }
                if(!People.Ids.Take(12).Contains(actor.Id)) throw new Exception("Abitante fuori luogo: "+actor.Id);
                bool seated=People.Seated(actor.Id);
                if(!actor.transform.Find(seated?"posa_seduta":"posa_in_piedi")) throw new Exception("Posa errata: "+actor.Id);
                var chest=actor.transform.TransformPoint(new Vector3(0,seated?1.08f:1.25f,0));
                var blocked=Physics.OverlapSphere(chest,.13f).Where(c=>!c.transform.IsChildOf(actor.transform)).ToArray();
                if(blocked.Length>0) throw new Exception("Personaggio interseca arredo: "+actor.Id+" / "+string.Join(",",blocked.Select(c=>c.name)));
            }
            foreach(var home in UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None).Where(l=>l.Id.StartsWith("casa_")||l.Id=="canonica")) {
                if(home.transform.Find("insegna")) throw new Exception("Insegna domestica: "+home.Id);
                var shelf=home.transform.Find("arredi_casa/libreria_parete");
                if(!shelf || Mathf.Abs(shelf.localPosition.z+.24f-(home.Dimensioni.y/2-.17f))>.025f) throw new Exception("Libreria staccata dal muro: "+home.Id);
                foreach(float sign in new[]{-1f,1f}) for(int k=0;k<3;k++) {
                    float z=-home.Dimensioni.y/2+home.Dimensioni.y/3*(k+.5f);
                    var center=home.transform.TransformPoint(new Vector3(sign*(home.Dimensioni.x/2-.65f),1.8f,z));
                    var blocked=Physics.OverlapBox(center,new Vector3(.35f,.57f,.80f),home.transform.rotation)
                        .Where(c=>c.transform.IsChildOf(home.transform.Find("arredi_casa"))).ToArray();
                    if(blocked.Length>0) throw new Exception("Arredo davanti finestra: "+home.Id+" / "+string.Join(",",blocked.Select(c=>c.name)));
                }
            }
            Debug.Log("SAN_ROCCO_PEOPLE_ROOMS_OK: 12 identita, pose e arredi verificati");
        }
        [MenuItem("Amnesia/San Rocco 1987/Verifica rifinitura")]
        public static void CheckPolish()
        {
            var places=UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None);
            foreach(var place in places.Where(p=>p.Id!="cava" && p.Id!="magazzino_b17")) {
                if(!place.transform.Find("soffitto")) throw new Exception("Soffitto assente: "+place.Id);
                if(!place.transform.Find("intradosso_tetto")) throw new Exception("Intradosso assente: "+place.Id);
            }
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(text.GetComponent<Renderer>().sharedMaterial.shader.name!="SanRocco/TextDepth") throw new Exception("Testo privo di depth test: "+text.text);
            if(!GameObject.Find("creste_continue")) throw new Exception("Creste continue assenti");
            if(!GameObject.Find("rosone")) throw new Exception("Rosone assente");
            foreach(var lamp in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="lampione"))
                foreach(var place in places) {
                    var p=place.transform.InverseTransformPoint(lamp.position);
                    if(Mathf.Abs(p.x)<place.Dimensioni.x/2 && Mathf.Abs(p.z)<place.Dimensioni.y/2) throw new Exception("Lampione dentro "+place.Id);
                }
            Debug.Log("SAN_ROCCO_POLISH_OK");
        }
    }
}
