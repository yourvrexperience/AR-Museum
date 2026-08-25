#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using UnityEngine;
using UnityEngine.Assertions;

namespace yourvrexperience.VR
{
	/// <summary>
	/// Visual portion of ray tool.
	/// </summary>
	public class HandRayToolView : MonoBehaviour
	/*
#if ENABLE_OCULUS
		, InteractableToolView
#endif
*/
	{
		private const int NUM_RAY_LINE_POSITIONS = 25;
		private const float DEFAULT_RAY_CAST_DISTANCE = 3.0f;

		[SerializeField] private Transform _targetTransform = null;
		[SerializeField] private Material _normalColor = null;
		[SerializeField] private Material _selectedColor = null;
		[SerializeField] private Transform _referenceRay = null;

#if ENABLE_OCULUS
		public bool EnableState
		{
			get
			{
				return _targetTransform.gameObject.activeSelf;
			}
			set
			{
				_targetTransform.gameObject.SetActive(value);
				if (OculusHandsManager.Instance.EnableVisualRays)
                {
					_referenceRay.gameObject.GetComponent<LineRenderer>().enabled = value;
				}
				else
                {
					_referenceRay.gameObject.GetComponent<LineRenderer>().enabled = false;
				}
			}
		}

		private bool _toolActivateState = false;

		public bool ToolActivateState
		{
			get { return _toolActivateState; }
			set
			{
				_toolActivateState = value;
                if (_referenceRay != null)
                {
                    if (_referenceRay.gameObject.activeSelf)
                    {
                        _referenceRay.gameObject.GetComponent<LineRenderer>().material = _toolActivateState ? _selectedColor : _normalColor;
                    }
                }                
			}
		}

        public Transform ReferenceRay
        {
            get { return _referenceRay; }
        }

        private Vector3[] linePositions = new Vector3[NUM_RAY_LINE_POSITIONS];
		private Gradient _oldColorGradient, _highLightColorGradient;

		private void Awake()
		{
			Assert.IsNotNull(_targetTransform);

            _highLightColorGradient = new Gradient();
			_highLightColorGradient.SetKeys(
			  new GradientColorKey[] { new GradientColorKey(new Color(0.90f, 0.90f, 0.90f), 0.0f),
		  new GradientColorKey(new Color(0.90f, 0.90f, 0.90f), 1.0f) },
			  new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
			);
		}

		// public InteractableTool InteractableTool { get; set; }

		private void Update()
		{
			/*
			if (OculusHandsManager.Instance.EnableVisualRays)
            {
				var myPosition = InteractableTool.ToolTransform.position;
				var myForward = InteractableTool.ToolTransform.forward;
				myPosition += myForward.normalized * 0.02f;

				var targetPosition = myPosition + myForward * DEFAULT_RAY_CAST_DISTANCE;
				_targetTransform.position = targetPosition;

				if (_referenceRay != null)
				{
					_referenceRay.transform.position = myPosition;
					_referenceRay.transform.forward = myForward;
				}
			}
			else
            {
				_referenceRay.gameObject.SetActive(false);
			}
			*/
		}

		public static Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			t = Mathf.Clamp01(t);
			var oneMinusT = 1f - t;
			var oneMinusTSqr = oneMinusT * oneMinusT;
			var tSqr = t * t;
			return oneMinusT * oneMinusTSqr * p0 + 3f * oneMinusTSqr * t * p1 + 3f * oneMinusT * tSqr * p2 +
				t * tSqr * p3;
		}
#endif
	}
}
