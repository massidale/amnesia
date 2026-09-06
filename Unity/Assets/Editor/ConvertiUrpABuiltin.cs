using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Converte i materiali URP in Built-in Standard (per gli asset URP: Slavika,
    /// OccaSoftware…), e RIPARA quelli gia' convertiti a cui e' rimasto l'Albedo
    /// vuoto — tipico quando la texture stava solo in `_BaseMap` (URP) e non in
    /// `_MainTex` (Built-in): senza Albedo lo Standard mostra grigio piatto.
    ///
    /// Robusto anche con URP non installato: leggiamo texture e colore dai dati
    /// SALVATI del .mat (m_SavedProperties), che restano leggibili pure con lo
    /// shader d'errore (il magenta). E' rieseguibile: chi e' gia' a posto lo salta.
    ///
    /// Uso: seleziona materiali, una cartella (es. OccaSoftware/EmaceArt), o gli
    /// oggetti in scena, poi lancia. Annullabile con Cmd+Z.
    public static class ConvertiUrpABuiltin
    {
        [MenuItem("Amnesia/Converti materiali URP → Built-in (selezione)")]
        static void Converti()
        {
            var mats = Raccogli();
            if (mats.Count == 0)
            {
                Debug.LogWarning("Niente da convertire. Seleziona materiali, una cartella, o gli oggetti in scena.");
                return;
            }

            var standard = Shader.Find("Standard");
            if (standard == null) { Debug.LogError("Non trovo lo shader Standard."); return; }

            int convertiti = 0, riparati = 0, gia = 0;
            foreach (var m in mats)
            {
                if (m == null) continue;

                // La texture del colore, da dove sta: _MainTex o (se vuoto) _BaseMap.
                Texture baseTex = TexSalvata(m, "_MainTex", "_BaseMap");
                Texture bump = TexSalvata(m, "_BumpMap");
                Texture metal = TexSalvata(m, "_MetallicGlossMap");
                Color baseCol = ColSalvato(m, "_Color", "_BaseColor");

                bool urp = ShaderUrpOErrore(m);
                bool albedoVuoto = !urp && m.HasProperty("_MainTex")
                                   && m.GetTexture("_MainTex") == null && baseTex != null;

                if (!urp && !albedoVuoto) { gia++; continue; }

                Undo.RecordObject(m, "Converti/Ripara materiale");

                if (urp)
                {
                    m.shader = standard;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", baseCol);
                    if (bump != null && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", bump); m.EnableKeyword("_NORMALMAP"); }
                    if (metal != null && m.HasProperty("_MetallicGlossMap")) { m.SetTexture("_MetallicGlossMap", metal); m.EnableKeyword("_METALLICGLOSSMAP"); }
                    convertiti++;
                }
                else riparati++;

                // In ogni caso: se l'Albedo e' vuoto e una texture c'e', mettila.
                if (baseTex != null && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == null)
                    m.SetTexture("_MainTex", baseTex);

                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Convertiti da URP: {convertiti}. Riparati (Albedo vuoto): {riparati}. Gia' a posto: {gia}. Cmd+Z per annullare.");
        }

        static bool ShaderUrpOErrore(Material m)
        {
            var sh = m.shader;
            if (sh == null) return true;
            var n = sh.name;
            return n.Contains("InternalErrorShader")
                || n.StartsWith("Universal Render Pipeline")
                || n.StartsWith("URP")
                || n.Contains("/URP/");
        }

        static Texture TexSalvata(Material m, params string[] nomi)
        {
            var so = new SerializedObject(m);
            var arr = so.FindProperty("m_SavedProperties.m_TexEnvs");
            if (arr == null) return null;
            foreach (var voluto in nomi)
                for (int i = 0; i < arr.arraySize; i++)
                {
                    var el = arr.GetArrayElementAtIndex(i);
                    if (el.FindPropertyRelative("first").stringValue != voluto) continue;
                    var t = el.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (t != null) return t;   // il primo nome con una texture vince
                }
            return null;
        }

        static Color ColSalvato(Material m, params string[] nomi)
        {
            var so = new SerializedObject(m);
            var arr = so.FindProperty("m_SavedProperties.m_Colors");
            if (arr == null) return Color.white;
            foreach (var voluto in nomi)
                for (int i = 0; i < arr.arraySize; i++)
                {
                    var el = arr.GetArrayElementAtIndex(i);
                    if (el.FindPropertyRelative("first").stringValue == voluto)
                        return el.FindPropertyRelative("second").colorValue;
                }
            return Color.white;
        }

        static HashSet<Material> Raccogli()
        {
            var mats = new HashSet<Material>();
            foreach (var o in Selection.objects)
            {
                if (o is Material m) { mats.Add(m); continue; }
                if (o is GameObject go)
                {
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        foreach (var mm in r.sharedMaterials)
                            if (mm != null) mats.Add(mm);
                    continue;
                }
                var path = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                    foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { path }))
                        mats.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            return mats;
        }
    }
}
