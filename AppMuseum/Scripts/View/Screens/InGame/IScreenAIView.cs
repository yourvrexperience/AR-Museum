using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.template6dof
{
	public interface IScreenAIView : IScreenView 
    {
		void SetState(AIRequestStates state);
		GameObject GetGameObject();
		void SetTextInputField(string text);
		void SetDescriptionRecording(string text);
		void SetDescriptionProcessing(string text);
		void PositionNarrator();
	}
}