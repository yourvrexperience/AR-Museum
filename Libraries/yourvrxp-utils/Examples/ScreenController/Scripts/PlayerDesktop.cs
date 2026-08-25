using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class PlayerDesktop : MonoBehaviour
	{
		public const string EventPlayerAvatarHasStarted = "EventPlayerAvatarHasStarted";
		public const string EventPlayerAvatarEnableMovement = "EventPlayerAvatarEnableMovement";

		[SerializeField] private GameObject Body;
		[SerializeField] private float Speed = 50;
        [SerializeField] private float Sensitivity = 7F;
		[SerializeField] private float HeightCamera = 1f;

		private bool _enableMovement = true;
		private float _rotationY = 0F;
		private Vector3 _forwardCamera = Vector3.zero;
		private GameObject _assetToPlace = null;

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			SystemEventController.Instance.DispatchSystemEvent(EventPlayerAvatarHasStarted, this.gameObject);
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventPlayerAvatarEnableMovement))
			{
				_enableMovement = (bool)parameters[0];
			}
			if (nameEvent.Equals(ScreenBuilder.EventScreenBuilderBuildObject))
			{
				ItemMultiObjectEntry itemToCreate = (ItemMultiObjectEntry)parameters[0];
				string nameAsset = (string)itemToCreate.Objects[2];
				_assetToPlace = AssetBundleController.Instance.CreateGameObject(nameAsset);
				_assetToPlace.transform.parent = ScreenControllerApp.Instance.Level.transform;
			}
		}

		private void Move()
        {
			float axisVertical = Input.GetAxis("Vertical");
			float axisHorizontal = Input.GetAxis("Horizontal");
			Vector3 forward = axisVertical * Camera.main.transform.forward * Speed * Time.deltaTime;
			Vector3 lateral = axisHorizontal * Camera.main.transform.right * Speed * Time.deltaTime;
			Vector3 increment = forward + lateral;
			increment.y = 0;
			transform.GetComponent<Rigidbody>().MovePosition(transform.position + increment);
        }

        private void RotateCamera()
        {
			float rotationX = Camera.main.transform.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * Sensitivity;
			_rotationY = _rotationY + Input.GetAxis("Mouse Y") * Sensitivity;
			_rotationY = Mathf.Clamp(_rotationY, -60, 60);
			Quaternion rotation = Quaternion.Euler(-_rotationY, rotationX, 0);
			_forwardCamera = rotation * Vector3.forward;
			this.transform.forward = new Vector3(_forwardCamera.x, 0, _forwardCamera.z);
        }

		void Update()
		{
			if (_enableMovement)
			{
				Move();
				RotateCamera();
				Camera.main.transform.position = this.transform.position + new Vector3(0, HeightCamera, 0);
				Camera.main.transform.forward = _forwardCamera;

				if (_assetToPlace != null)
				{
					RaycastHit hitData = new RaycastHit();
					Vector3 posFloor = RaycastingTools.GetMouseCollisionPoint(Camera.main, ref hitData, ScreenControllerApp.Instance.LayerFloor);
					if (posFloor != Vector3.zero)
					{
						_assetToPlace.transform.position = posFloor;
					}
					if (Input.GetMouseButtonDown(0))
					{
						_assetToPlace = null;
					}
				}
				else
				{
					if (Input.GetMouseButtonDown(0))
					{
						SystemEventController.Instance.DispatchSystemEvent(ScreenControllerApp.EventScreenControllerBasicRequestInteractionMouse);
					}
				}
			}
		}
	}
}