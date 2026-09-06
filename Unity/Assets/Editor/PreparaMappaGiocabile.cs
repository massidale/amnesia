using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Prende la scena aperta (la tua «mappa» disegnata a mano) e ci mette
    /// dentro il gioco: Bootstrap, il pannello dei dialoghi, il menu e il
    /// giocatore vero — quello che cammina, si avvicina e parla con la gente.
    ///
    /// Bootstrap va in modo «mappa a mano»: non rigenera niente, usa il paese e
    /// gli abitanti che hai gia' piazzato (quelli col componente Personaggio).
    /// Il vecchio omino da passeggio (FreeWalk) viene spento, cosi' non ci sono
    /// due telecamere che litigano.
    ///
    /// E' RIESEGUIBILE senza danni: se il gioco c'e' gia', non lo raddoppia.
    /// Dopo: SALVA (Cmd+S) e premi Play.
    public static class PreparaMappaGiocabile
    {
        [MenuItem("Amnesia/Prepara mappa giocabile")]
        static void Prepara()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }

            if (Object.FindFirstObjectByType<Bootstrap>() != null)
            {
                Debug.LogWarning("C'e' gia' un Bootstrap in scena: la mappa e' gia' pronta. "
                    + "Se vuoi rifarla, cancella a mano «gioco», «pannello», «menu», «giocatore».");
                return;
            }

            // Dove far partire il giocatore: accanto alla madre. Se c'e' Rosa in
            // scena (un Personaggio con id «rosa»), Giorgio nasce di fianco a lei.
            // Altrimenti dove stava il vecchio FreeWalk, o un punto di ripiego.
            Vector3 partenza = new Vector3(846f, 12f, 503f);
            bool accantoARosa = false;
            foreach (var pers in Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None))
                if (pers.Id.Trim() == "rosa")
                {
                    partenza = pers.transform.position + pers.transform.right * 2f + Vector3.up * 1f;
                    accantoARosa = true;
                    break;
                }

            var vecchio = Object.FindFirstObjectByType<FreeWalk>();
            if (vecchio != null)
            {
                if (!accantoARosa) partenza = vecchio.transform.position + Vector3.up * 1f;
                // Non lo cancello: lo spengo. Cosi' resti libero di tornarci, e
                // le sue telecamera/ascoltatore non fanno a pugni col giocatore.
                vecchio.gameObject.SetActive(false);
                Debug.Log("Vecchio giocatore da passeggio (FreeWalk) spento, non cancellato.");
            }
            if (accantoARosa) Debug.Log("Giorgio spawnera' accanto a Rosa.");

            // Il gioco: quattro oggetti, come nella scena di prova.
            var boot = new GameObject("gioco").AddComponent<Bootstrap>();
            new GameObject("pannello").AddComponent<Pannello>();
            new GameObject("menu").AddComponent<Menu>();

            var giocatore = new GameObject("giocatore");
            giocatore.transform.position = partenza;
            giocatore.AddComponent<Giocatore>();
            // Un ascoltatore audio, cosi' Unity non brontola che non ce n'e'.
            giocatore.AddComponent<AudioListener>();

            // I flag privati di Bootstrap si mettono da qui: mappa a mano ON,
            // generazione OFF. Cosi' non spuntano case sulla griglia di gioco.
            var so = new SerializedObject(boot);
            Imposta(so, "mappaAMano", true);
            Imposta(so, "generaScenografia", false);
            Imposta(so, "villaggioCompleto", false);
            // La foto dall'alto come mappa della pausa, se c'e'.
            var img = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Amnesia/mappa_paese.png");
            var propImg = so.FindProperty("mappaImmagine");
            if (img != null && propImg != null) propImg.objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();

            var scena = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scena);

            Debug.Log("Mappa pronta a giocare. SALVA con Cmd+S, poi premi Play. "
                + "WASD per camminare, E per parlare a chi ti si avvicina, Esc per la pausa. "
                + "(Serve il file .env con la chiave OpenRouter perche' i dialoghi funzionino.)");
        }

        static void Imposta(SerializedObject so, string campo, bool valore)
        {
            var prop = so.FindProperty(campo);
            if (prop != null) prop.boolValue = valore;
            else Debug.LogWarning($"Campo «{campo}» non trovato su Bootstrap: controlla il nome.");
        }
    }
}
