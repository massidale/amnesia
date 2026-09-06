using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    [RequireComponent(typeof(RawImage))]
    public sealed class AnteprimaOggetto : MonoBehaviour, IDragHandler
    {
        private GameObject _rig;
        private Transform _pivot;
        private GameObject _model;
        private Camera _camera;
        private RenderTexture _texture;
        private float _yaw;
        private float _pitch;

        public void Mostra(string id, bool aperta = false, bool braccialetto = true)
        {
            if (_rig == null) Crea();
            if (_model != null) { _model.SetActive(false); Destroy(_model); }
            var prefab = Resources.Load<GameObject>("Oggetti1987/" + id);
            if (prefab == null) { _camera.Render(); return; }
            _model = Instantiate(prefab, _pivot);
            foreach(var collider in _model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var lid = _model.transform.Find("coperchio");
            if (lid != null) lid.localRotation = Quaternion.Euler(aperta ? 115 : 0, 0, 0);
            var tie = _model.transform.Find("spago");
            if (tie != null) tie.gameObject.SetActive(!aperta);
            var jewel = _model.transform.Find("braccialetto");
            if (jewel != null) jewel.gameObject.SetActive(aperta && braccialetto);
            _pivot.localRotation = Quaternion.identity;
            var renderers = _model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var scale = 1.5f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            _model.transform.localScale *= scale;
            _model.transform.localPosition = (_pivot.position - bounds.center) * scale;
            _yaw = -18; _pitch = 0;
            Disegna();
        }

        private void Crea()
        {
            // Beyond game cameras' far planes; gameplay layers and global lighting are unchanged.
            _rig = new GameObject("studio_oggetto_3d");
            _rig.transform.position = new Vector3(12000,12000,12000);
            _pivot = new GameObject("rotazione").transform;
            _pivot.SetParent(_rig.transform, false);
            _camera = new GameObject("camera_oggetto").AddComponent<Camera>();
            _camera.transform.SetParent(_rig.transform, false);
            _camera.transform.localPosition = new Vector3(0,2.2f,-2.7f);
            _camera.transform.LookAt(_pivot);
            _camera.orthographic = true; _camera.orthographicSize = 1.12f;
            _camera.nearClipPlane = .1f; _camera.farClipPlane = 8;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(.79f,.82f,.79f,1);
            _camera.enabled = false;
            _texture = new RenderTexture(512,512,24,RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _texture.Create(); _camera.targetTexture = _texture;
            GetComponent<RawImage>().texture = _texture;
            var light = new GameObject("luce_oggetto").AddComponent<Light>();
            light.transform.SetParent(_rig.transform, false);
            light.transform.localPosition = new Vector3(-1,2,-2);
            light.type = LightType.Point; light.range = 7; light.intensity = 2.2f;
            light.color = new Color(1,.96f,.88f);
        }

        public void OnDrag(PointerEventData data)
        {
            _yaw -= data.delta.x * .65f;
            _pitch = Mathf.Clamp(_pitch + data.delta.y * .65f,-180,180);
            Disegna();
        }
        private void Disegna()
        {
            if (_pivot == null) return;
            _pivot.localRotation = Quaternion.Euler(_pitch,_yaw,0);
            _camera.Render();
        }
        private void OnEnable() { if(_rig != null) { _rig.SetActive(true); Disegna(); } }
        private void OnDisable() { if(_rig != null) _rig.SetActive(false); }
        private void OnDestroy()
        {
            if(_camera != null) _camera.targetTexture = null;
            if(_texture != null) { _texture.Release(); Destroy(_texture); }
            if(_rig != null) Destroy(_rig);
        }
    }
}
