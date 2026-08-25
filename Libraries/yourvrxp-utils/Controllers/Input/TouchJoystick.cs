using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace yourvrexperience.Utils
{
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public enum ConfigurationDirections { ALL, HORIZONTAL, VERTICAL }

        [SerializeField] private float dotRange = 1;
        [SerializeField] private float deadArea = 0;
        [SerializeField] private ConfigurationDirections directionOptions = ConfigurationDirections.ALL;

        [SerializeField] protected RectTransform bg = null;
        [SerializeField] private RectTransform dot = null;
        [SerializeField] private float limitMovement = 1;

        private Canvas _joystickCanvas;
        private Camera _joystickCamera;

        private Vector2 _joystickInput = Vector2.zero;
        private Vector2 _staticPosition = Vector2.zero;

        public float DotRange
        {
            get { return dotRange; }
            set { dotRange = Mathf.Abs(value); }
        }

        public float ZonaMuerta
        {
            get { return deadArea; }
            set { deadArea = Mathf.Abs(value); }
        }

        public float Horizontal
        {
            get { return _joystickInput.x; }
        }
        public float Vertical
        {
            get { return _joystickInput.y; }
        }
        public Vector2 Direction
        {
            get { return new Vector2(Horizontal, Vertical); }
        }

        public ConfigurationDirections DirectionConfig
        {
            get { return DirectionConfig; }
            set { directionOptions = value; }
        }
        void Start()
        {
            DotRange = dotRange;
            ZonaMuerta = deadArea;
            _joystickCanvas = GetComponentInParent<Canvas>();

            Vector2 center = new Vector2(0.5f, 0.5f);
            bg.pivot = center;
            dot.anchorMin = center;
            dot.anchorMax = center;
            dot.pivot = center;
            dot.anchoredPosition = Vector2.zero;

            _staticPosition = bg.anchoredPosition;
            bg.anchoredPosition = _staticPosition;
            bg.gameObject.SetActive(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _joystickInput = Vector2.zero;
            dot.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _joystickCamera = null;
            if (_joystickCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _joystickCamera = _joystickCanvas.worldCamera;
            }

            Vector2 position = RectTransformUtility.WorldToScreenPoint(_joystickCamera, bg.position);
            Vector2 radius = bg.sizeDelta / 2;
            _joystickInput = (eventData.position - position) / (radius * _joystickCanvas.scaleFactor);
            if (directionOptions == ConfigurationDirections.HORIZONTAL)
            {
                _joystickInput = new Vector2(_joystickInput.x, 0f);
            }
            else
            {
                if (directionOptions == ConfigurationDirections.VERTICAL)
                {
                    _joystickInput = new Vector2(0f, _joystickInput.y);
                }
            }
            if (_joystickInput.magnitude > deadArea)
            {
                if (_joystickInput.magnitude > 1)
                {
                    _joystickInput = _joystickInput.normalized;
                }
            }
            else
            {
                _joystickInput = Vector2.zero;
            }
            dot.anchoredPosition = _joystickInput * radius * dotRange;
        }
    }
}