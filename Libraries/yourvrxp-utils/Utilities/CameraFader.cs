using UnityEngine;
using System.Collections;
using System;

namespace yourvrexperience.Utils
{
	[RequireComponent(typeof(MeshFilter))]
	public class CameraFader : MonoBehaviour 
	{
		public const string EventCameraFaderFadeCompleted = "EventCameraFaderFadeCompleted";

		private static CameraFader _instance;
		public static CameraFader Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = GameObject.FindObjectOfType<CameraFader>();
				}
				return _instance;
			}
		}

		[SerializeField] private float inAlpha = 1.0f;
		[SerializeField] private float outAlpha = 0.0f;

		private bool _fading;
		private float _fadeTimer;

		private Color _fromColor;
		private Color _toColor;
		private Material _material;

		private bool _isFadeIn;

		private bool _isEnabled = true;

		public bool IsEnabled 
		{
			get { return _isEnabled; }
			set { _isEnabled = value;
				gameObject.GetComponent<Renderer>().enabled = _isEnabled;				
			}
		}

		private void Awake()
		{
			_fading = false;
			_fadeTimer = 0;

			_material = gameObject.GetComponent<Renderer>().material;
			_fromColor = _material.color;
			_toColor = _material.color;

			yourvrexperience.Utils.Utilities.ReverseNormals(gameObject);
		}

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string _nameEvent, object[] _parameters)
		{
			if (_nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
			{
				_instance = null;
			}
		}

		void Update()
		{
			if (_fading == false)
				return;

			_fadeTimer += Time.deltaTime;
			_material.color = Color.Lerp(_fromColor, _toColor, _fadeTimer);
			if(_material.color == _toColor)
			{
				_fading = false;
				_fadeTimer = 0;
				SystemEventController.Instance.DispatchSystemEvent(EventCameraFaderFadeCompleted, _isFadeIn);
			}
		}

		public void FadeOut()
		{
			_isFadeIn = false;
			if (this != null)
            {
				if (_material != null)
                {
					_material.color = new Color(0, 0, 0, 1);
					_fromColor.a = inAlpha;
					_toColor.a = outAlpha;
					if (_toColor != _material.color)
					{
						_fading = true;
					}
				}
			}
		}

		public void FadeIn()
		{
			_isFadeIn = true;
			if (this != null)
			{
				if (_material != null)
				{
					_material.color = new Color(0, 0, 0, 0);
					_fromColor.a = outAlpha;
					_toColor.a = inAlpha;
					if (_toColor != _material.color)
					{
						_fading = true;
					}
				}
			}
		}
	}
}