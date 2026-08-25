using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
	public class PanelRatingView : MonoBehaviour
	{
		public const float MAXIMUM_VALUE_VOTE = 5;

		[SerializeField] private Sprite EmptyStar;
		[SerializeField] private Sprite FullStar;
		[SerializeField] private Sprite HalfStar;

		private Transform _container;
		private string _eventData = "";
		private List<GameObject> _stars = new List<GameObject>();
		private bool _showScore = false;
		private bool _isInteractable = true;

		private string _property = "";

		public bool IsInteractable
		{
			get { return _isInteractable; }
			set { _isInteractable = value; }
		}
		public string Property
		{
			get { return _property; }
			set { _property = value; }
		}

		public void Initialize(int score, int totalVotes, string title, bool showScore, string eventData, bool isInteractable)
		{
			this._eventData = eventData;
			_container = this.gameObject.transform;

			if (_stars.Count == 0)
			{
				GameObject star0 = _container.Find("Star0").gameObject;
				star0.GetComponent<Button>().onClick.AddListener(OnClickStar0);
				_stars.Add(star0);

				GameObject star1 = _container.Find("Star1").gameObject;
				star1.GetComponent<Button>().onClick.AddListener(OnClickStar1);
				_stars.Add(star1);

				GameObject star2 = _container.Find("Star2").gameObject;
				star2.GetComponent<Button>().onClick.AddListener(OnClickStar2);
				_stars.Add(star2);

				GameObject star3 = _container.Find("Star3").gameObject;
				star3.GetComponent<Button>().onClick.AddListener(OnClickStar3);
				_stars.Add(star3);

				GameObject star4 = _container.Find("Star4").gameObject;
				star4.GetComponent<Button>().onClick.AddListener(OnClickStar4);
				_stars.Add(star4);
			}

			for (int i = 0; i < _stars.Count; i++)
			{
				_stars[i].GetComponent<Image>().overrideSprite = EmptyStar;
			}

			this._showScore = showScore;
			SetScore(score, totalVotes);

			_container.Find("Title").GetComponent<Text>().text = title;

			this._isInteractable = isInteractable;
		}

		public void SetScore(int score, int totalVotes)
		{
			int counter = 0;
			float starScore = ((float)score / (float)totalVotes);
			for (int i = 0; i < _stars.Count; i++)
			{
				float checkValue = i + 1;
				if (starScore >= checkValue)
				{
					counter++;
					_stars[i].GetComponent<Image>().overrideSprite = FullStar;
				}
				else
				{
					float segment = checkValue - starScore;
					if (segment < 0.5)
					{
						_stars[i].GetComponent<Image>().overrideSprite = HalfStar;
					}
					else
					{
						_stars[i].GetComponent<Image>().overrideSprite = EmptyStar;
					}
				}
			}

			if (_showScore)
			{
				string scoreGrade = LanguageController.Instance.GetText("message.score." + counter);
				if (score > 0)
				{
					_container.Find("Score").GetComponent<Text>().text = scoreGrade + " : " + ((starScore / MAXIMUM_VALUE_VOTE) * 100) + "%";
				}
				else
				{
					_container.Find("Score").GetComponent<Text>().text = "";
				}
			}
		}

		private void OnClickStar(int index)
		{
			if (_isInteractable)
			{
				if (_eventData.Length > 0)
				{
					UIEventController.Instance.DispatchUIEvent(_eventData, this.gameObject, index, _property);
				}
			}
		}

		private void OnClickStar0()
		{
			OnClickStar(0);
		}

		private void OnClickStar1()
		{
			OnClickStar(1);
		}

		private void OnClickStar2()
		{
			OnClickStar(2);
		}

		private void OnClickStar3()
		{
			OnClickStar(3);
		}

		private void OnClickStar4()
		{
			OnClickStar(4);
		}

		public void DisableInteraction()
		{
			_isInteractable = false;
		}

		public void SetTitle(string text)
		{
			_container.Find("Title").GetComponent<Text>().text = text;
		}

		public void SetTextScore(string text)
		{
			_container.Find("Score").GetComponent<Text>().text = text;
		}
	}
}