using UnityEngine;

namespace AmnesiaUnity
{
    /// Un cammina-e-guarda minimo per esplorare la mappa in Play mode.
    /// Nessun pacchetto: usa l'Input Manager classico. Va su una Capsule con un
    /// CharacterController e una Camera come figlia (vedi istruzioni nel chat).
    /// WASD/frecce per muoversi, mouse per guardare, Shift per correre, Esc per
    /// liberare il cursore.
    [RequireComponent(typeof(CharacterController))]
    public sealed class FreeWalk : MonoBehaviour
    {
        public float velocita = 4f;
        public float corsa = 8f;
        public float sensibilita = 2f;
        public float gravita = 20f;

        private CharacterController _cc;
        private Camera _occhio;
        private float _pitch;
        private float _vy;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _occhio = GetComponentInChildren<Camera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            var mx = Cursor.lockState==CursorLockMode.Locked ? Input.GetAxis("Mouse X") * sensibilita : 0;
            var my = Cursor.lockState==CursorLockMode.Locked ? Input.GetAxis("Mouse Y") * sensibilita : 0;
            transform.Rotate(0f, mx, 0f);
            _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
            if (_occhio != null)
            {
                _occhio.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            }

            var avanti = Input.GetAxisRaw("Vertical");
            var lato = Input.GetAxisRaw("Horizontal");
            var v = Input.GetKey(KeyCode.LeftShift) ? corsa : velocita;
            var moto = transform.forward * avanti + transform.right * lato;
            if (moto.sqrMagnitude > 1f)
            {
                moto.Normalize();
            }
            moto *= v;

            if (_cc.isGrounded && _vy < 0f)
            {
                _vy = -2f;
            }
            _vy -= gravita * Time.deltaTime;
            moto.y = _vy;
            _cc.Move(moto * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool release=Cursor.lockState==CursorLockMode.Locked;
                Cursor.lockState = release?CursorLockMode.None:CursorLockMode.Locked;
                Cursor.visible = release;
            }
        }
    }
}
