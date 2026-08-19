using Amnesia.Core;
using UnityEngine;

namespace AmnesiaUnity
{
    /// Cammina, e si ferma quando si parla. La telecamera sta dietro le spalle e
    /// scende in faccia quando comincia una conversazione: in un gioco in cui la
    /// meccanica e' guardare in faccia uno che mente, l'inquadratura non e' un
    /// dettaglio di regia.
    public sealed class Giocatore : MonoBehaviour
    {
        public const float PortataDiParola = 2.5f;

        private Bootstrap _gioco;
        private Camera _occhio;
        private Pannello _pannello;
        private Transform _bersaglio;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            _pannello = FindFirstObjectByType<Pannello>();

            var posizione = _gioco.World.ActorOf("player").Position ?? new Cell(0, 0);
            transform.position = _gioco.InScena(posizione) + Vector3.up * 0.9f;

            _occhio = new GameObject("occhio").AddComponent<Camera>();
            _occhio.transform.SetParent(transform, false);
            _occhio.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            _occhio.fieldOfView = 62f;
        }

        private void Update()
        {
            if (_gioco == null || !_gioco.enabled)
            {
                return;
            }
            if (_pannello != null && _pannello.Aperto)
            {
                InquadraIlVolto();
                return;
            }

            var avanti = Input.GetAxisRaw("Vertical");
            var lato = Input.GetAxisRaw("Horizontal");
            if (Input.GetKey(KeyCode.Mouse1))
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * 3f, 0f);
            }
            else
            {
                transform.Rotate(0f, lato * 90f * Time.deltaTime, 0f);
            }
            transform.position += transform.forward * (avanti * 4.5f * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.E))
            {
                var chi = _gioco.PiuVicino(transform.position, PortataDiParola);
                if (!string.IsNullOrEmpty(chi))
                {
                    _bersaglio = GameObject.Find(chi).transform;
                    _pannello.Apri(chi);
                }
            }
        }

        /// Mentre si parla la telecamera va addosso, e ci resta. Il resto del
        /// paese smette di esistere: e' l'unica cosa che questa scena di prova
        /// deve dimostrare.
        private void InquadraIlVolto()
        {
            if (_bersaglio == null)
            {
                return;
            }
            var verso = _bersaglio.position + Vector3.up * 0.35f;
            var da = verso - (verso - transform.position).normalized * 1.6f;
            _occhio.transform.position = Vector3.Lerp(_occhio.transform.position, da + Vector3.up * 0.2f, Time.deltaTime * 4f);
            _occhio.transform.rotation = Quaternion.Slerp(
                _occhio.transform.rotation, Quaternion.LookRotation(verso - _occhio.transform.position), Time.deltaTime * 5f);
        }
    }
}
