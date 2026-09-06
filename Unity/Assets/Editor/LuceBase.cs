using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AmnesiaUnity.Editor
{
    /// Da' alla scena aperta una luce di base sensata: una Directional Light
    /// forte e un ambient chiaro, cosi' niente resta al buio. Serve quando la
    /// scena arrivava con la luce pilotata da uno script (poi rimosso) e resta
    /// scura anche con lo skybox.
    ///
    /// Non e' art direction: e' «accendi la luce». Poi la regoli a gusto.
    public static class LuceBase
    {
        [MenuItem("Amnesia/Luce base (scena aperta)")]
        static void Accendi()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }

            // Una sola Directional Light: riuso quella che c'e', o ne creo una.
            Light sole = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sole = l; break; }
            if (sole == null)
                sole = new GameObject("Sole").AddComponent<Light>();

            sole.type = LightType.Directional;
            sole.enabled = true;
            sole.gameObject.SetActive(true);
            sole.intensity = 1.3f;
            sole.color = new Color(1f, 0.96f, 0.84f);
            sole.shadows = LightShadows.Soft;
            sole.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ambient piatto e chiaro: garantisce che le facce in ombra si vedano
            // comunque, indipendentemente da skybox e lightmap.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            RenderSettings.ambientIntensity = 1f;

            DynamicGI.UpdateEnvironment();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Luce base impostata (direzionale 1.3 + ambient chiaro). Regola pure intensita' e rotazione. "
                + "Se qualcosa resta NERO: e' lightmap vecchia — Window → Rendering → Lighting, togli la spunta a "
                + "«Baked Global Illumination» o cancella i dati bake.");
        }
    }
}
