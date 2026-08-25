using System;
using System.Collections;
using System.Collections.Generic;
using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace yourvrexperience.VR
{
	public class PanelInputTextAction : MonoBehaviour
	{
       
		public const string EventPanelInputActionEdit = "EventPanelInputActionEdit";
		public const string EventPanelInputActionCut = "EventPanelInputActionCut";
		public const string EventPanelInputActionCopy = "EventPanelInputActionCopy";
		public const string EventPanelInputActionPaste = "EventPanelInputActionPaste";
		public const string EventPanelInputActionReturn = "EventPanelInputActionReturn";
		public const string EventPanelInputActionClose = "EventPanelInputActionClose";
		public const string EventPanelInputActionUndo = "EventPanelInputActionUndo";
		public const string EventPanelInputActionDelete = "EventPanelInputActionDelete";
		public const string EventPanelInputActionSpace = "EventPanelInputActionSpace";

		public const string EventPanelInputExternalClose = "EventPanelInputExternalClose";
		public const string EventPanelInputConfirmationClose = "EventPanelInputConfirmationClose";

		[SerializeField] private GameObject Content;
		[SerializeField] private Button Edit;
		[SerializeField] private Button Cut;
		[SerializeField] private Button Copy;
		[SerializeField] private Button Paste;
		[SerializeField] private Button Return;
		[SerializeField] private Button Close;
		[SerializeField] private Button Undo;
		[SerializeField] private Button Delete;
		[SerializeField] private Button Space;
        [SerializeField] private float PanelDistance = 0.4f;
        [SerializeField] private float PanelZoom = 2f;
        [SerializeField] private string PanelLayerCast = "UI";

		private CustomInput _inputDescriptionObject;

		private int _startPosition = -1;
		private int _endPosition = -1;
		private bool _isEditionActivated = false;
		private string _selection = "";
        private string _memoryBuffer = "";
        private List<string> _textHistory = new List<string>();
		private int _layerUI;

		public CustomInput InputDescriptionObject
		{
			get { return _inputDescriptionObject; }
			set { 
				if ((_inputDescriptionObject == null) || ((value != null) && (_inputDescriptionObject != value)))
				{
					if (_inputDescriptionObject != null)
					{
						_inputDescriptionObject.onTextSelection.RemoveListener(GetTextSelectedText);
						_inputDescriptionObject.onSelect.RemoveListener(GetSelected);
						_inputDescriptionObject.interactable = true;
					}
					_isEditionActivated = false;
					_inputDescriptionObject = value; 
					_inputDescriptionObject.onTextSelection.AddListener(GetTextSelectedText);
					_inputDescriptionObject.onSelect.AddListener(GetSelected);
					_textHistory = new List<string>();
				}
				else
				{
					if (value == null)
					{
						_inputDescriptionObject.onTextSelection.RemoveListener(GetTextSelectedText);
						_inputDescriptionObject.onSelect.RemoveListener(GetSelected);
						_inputDescriptionObject = null; 
					}
				}
			}
		}

		public void Initialize()
		{			
			Edit.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("word.edit");
			Cut.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("word.cut");
			Copy.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("word.copy");
			Paste.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("word.paste");
			Return.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.Instance.GetText("word.return");

			Edit.onClick.AddListener(OnEdit);
			Cut.onClick.AddListener(OnCut);
			Copy.onClick.AddListener(OnCopy);
			Paste.onClick.AddListener(OnPaste);
			Return.onClick.AddListener(OnReturn);
			Close.onClick.AddListener(OnClose);
			Undo.onClick.AddListener(OnUndo);
			Delete.onClick.AddListener(OnDelete);
			Space.onClick.AddListener(OnSpace);

			_layerUI = LayerMask.GetMask(PanelLayerCast);

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			this.gameObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
#if ENABLE_OCULUS
			this.gameObject.AddComponent<OVRRaycaster>();
#elif ENABLE_OPENXR
			this.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
#elif ENABLE_ULTIMATEXR
			if (this.gameObject.GetComponent<UxrCanvas>() == null) this.gameObject.AddComponent<UxrCanvas>();
			if (this.gameObject.GetComponent<UxrLaserPointerRaycaster>() == null) this.gameObject.AddComponent<UxrLaserPointerRaycaster>();
			this.gameObject.GetComponent<UxrCanvas>().CanvasInteractionType = UxrInteractionType.LaserPointers;
			this.gameObject.GetComponentInChildren<Canvas>().renderMode = RenderMode.WorldSpace;									
#elif ENABLE_NREAL
			this.gameObject.AddComponent<CanvasRaycastTarget>();					
#endif			
#endif

			UIEventController.Instance.Event += OnUIEvent;
		}

		private void ResetCaretAndBufferPositions(bool freeMemory = true)
		{
			_startPosition = -1;
			_endPosition = -1;
			_isEditionActivated = false;
			if (freeMemory)
			{
				_memoryBuffer = "";
			}
		}

		void OnDestroy()
		{
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
			UIEventController.Instance.DispatchUIEvent(EventPanelInputConfirmationClose);
		}

        public void Destroy()
		{
			if (_inputDescriptionObject == null)
			{
				if (this.gameObject != null)
				{
					GameObject.Destroy(this.gameObject);
				}				
			}
			else
			{
				_inputDescriptionObject.interactable = true;
				SetVisibility(false);
			}
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventPanelInputExternalClose))
			{
				_inputDescriptionObject = null;
				Destroy();
			}
			if (_inputDescriptionObject != null)
			{
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionEdit))
				{
					if (_selection.Length > 0)
					{
						if (GameObject.FindAnyObjectByType<ScreenVRKeyboardView>() == null)
						{
							ScreenController.Instance.CreateScreen(ScreenVRKeyboardView.ScreenName, false, true,  _inputDescriptionObject.gameObject, _selection, 30);
						}
					}
				}
				if (nameEvent.Equals(ScreenVRKeyboardView.EventScreenVRKeyboardSetNewText))
				{
					if (_inputDescriptionObject.gameObject == (GameObject)parameters[0])
					{
						string newText = (string)parameters[1];
						if ((_startPosition != -1) && (_endPosition != -1))
						{
							string data = _inputDescriptionObject.text;
							_textHistory.Add(data);
							_inputDescriptionObject.text = yourvrexperience.Utils.Utilities.ReplaceTextPosition(data, _startPosition, _endPosition, newText);
							ResetCaretAndBufferPositions();
						}					
					}
					ResetCaretAndBufferPositions();
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionCut))
				{
					_isEditionActivated = false;
					_memoryBuffer = ((_selection.Length > 0)?_selection:"");
					if (_memoryBuffer.Length > 0)
					{
						_textHistory.Add(_inputDescriptionObject.text);
						_inputDescriptionObject.text = yourvrexperience.Utils.Utilities.ReplaceTextPosition(_inputDescriptionObject.text, _startPosition, _endPosition, "");
					}
					ResetCaretAndBufferPositions(false);
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionCopy))
				{
					_isEditionActivated = false;
					_memoryBuffer = ((_selection.Length > 0)?_selection:"");
					ResetCaretAndBufferPositions(false);
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionPaste))
				{
					if (_memoryBuffer.Length > 0)
					{
						_textHistory.Add(_inputDescriptionObject.text);
						_inputDescriptionObject.text = yourvrexperience.Utils.Utilities.ReplaceTextPosition(_inputDescriptionObject.text, _inputDescriptionObject.caretPosition, _inputDescriptionObject.caretPosition, _memoryBuffer);
					}
					ResetCaretAndBufferPositions();
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionReturn))
				{
					if ((_startPosition != -1) && (_endPosition != -1))
					{
						_textHistory.Add(_inputDescriptionObject.text);
						_inputDescriptionObject.text = yourvrexperience.Utils.Utilities.ReplaceTextPosition(_inputDescriptionObject.text, _inputDescriptionObject.caretPosition, _inputDescriptionObject.caretPosition, "\n");	
					}
					ResetCaretAndBufferPositions(false);
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionClose))
				{
					_isEditionActivated = false;
					ResetCaretAndBufferPositions(false);
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionUndo))
				{
					if ((_startPosition != -1) && (_endPosition != -1))
					{
						if (_textHistory.Count > 0)
						{
							string previousData = _textHistory[_textHistory.Count - 1];
							_textHistory.RemoveAt(_textHistory.Count - 1);
							_inputDescriptionObject.text = previousData;	
						}
					}
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionDelete))
				{
					if (_startPosition != -1)
					{
						string data = _inputDescriptionObject.text;
						_textHistory.Add(data);
						data = yourvrexperience.Utils.Utilities.ReplaceTextPosition(data, _inputDescriptionObject.caretPosition - 1, _inputDescriptionObject.caretPosition, "");
						_inputDescriptionObject.caretPosition--;
						_inputDescriptionObject.text = data;	
					}
				}
				if (nameEvent.Equals(PanelInputTextAction.EventPanelInputActionSpace))
				{
					if (_startPosition != -1)
					{
						string data = _inputDescriptionObject.text;
						_textHistory.Add(data);
						data = yourvrexperience.Utils.Utilities.ReplaceTextPosition(_inputDescriptionObject.text, _inputDescriptionObject.caretPosition, _inputDescriptionObject.caretPosition, " ");
						_inputDescriptionObject.text = data;	
					}
				}
			}
        }

        private void OnSpace()
        {
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionSpace);
        }

        private void OnDelete()
        {
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionDelete);
        }

        private void OnUndo()
        {
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionUndo);
        }

        private void OnReturn()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionReturn);
        }

        private void OnClose()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionClose);
        }

        private void OnPaste()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionPaste);
        }

        private void OnCopy()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionCopy);
        }

        private void OnCut()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionCut);
        }

        private void OnEdit()
        {
			Destroy();
            UIEventController.Instance.DispatchUIEvent(EventPanelInputActionEdit);
        }

		private void GetTextSelectedText(string str, int start, int end)
		{
            _selection = str.Substring(Mathf.Min(start, end), Mathf.Abs(end - start));
            _startPosition = start;
            _endPosition = end;
		}

        private void GetSelected(string value)
        {
			if (!_isEditionActivated)
			{
				_startPosition = _inputDescriptionObject.caretPosition;
				_endPosition = _inputDescriptionObject.caretPosition;
			}            
        }

        private void ReleaseSelection()
		{
			_inputDescriptionObject.ReleaseSelection();
		}	

		public void SetVisibility(bool visible)
		{
			Content.SetActive(visible);			
		}

        private void Update()
		{
			if (_inputDescriptionObject != null)
			{
				if ((_startPosition != -1) || (_endPosition != -1))
				{
					if (!_isEditionActivated)
					{
						if (VRInputController.Instance.ActionPrimaryUp())
						{
							_isEditionActivated = true;	
							_inputDescriptionObject.interactable = false;							
							RaycastHit hitCollision = new RaycastHit();
							Vector3 pointScreen = RaycastingTools.GetRaycastOriginForward(VRInputController.Instance.VRController.CurrentController.transform.position, VRInputController.Instance.VRController.CurrentController.transform.forward, ref hitCollision, 100, _layerUI);
							float sizePanel = ScreenController.Instance.SizeVRScreen / PanelZoom;
							pointScreen -= VRInputController.Instance.Camera.transform.forward.normalized * PanelDistance;
							SetVisibility(true);
							this.transform.position = pointScreen;	
							this.transform.forward = VRInputController.Instance.Camera.transform.forward.normalized;
							this.transform.localScale = new Vector3(sizePanel, sizePanel, sizePanel);
						}
					}
				}
			}
		}
    }
}