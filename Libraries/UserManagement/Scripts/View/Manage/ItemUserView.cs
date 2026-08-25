using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ItemUserView : MonoBehaviour, ISlotView
    {
        public const string EVENT_ITEM_USER_SELECTED = "EVENT_ITEM_USER_SELECTED";

        private GameObject _parent;
        private int _index;
        private UserModel _dataUserModel;
        private Image _background;
        private Text _text;
        private bool _selected = false;
        private ItemMultiObjectEntry _dataGeneric;

        public int Index
        {
            get { return _index; }
        }
        public UserModel DataUserModel
        {
            get { return _dataUserModel; }
        }
        public virtual bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (_selected)
                {
                    _background.color = Color.cyan;
                }
                else
                {
                    _background.color = Color.white;
                }
            }
        }

        public ItemMultiObjectEntry Data
        {
            get { return _dataGeneric; }
        }

        public void Initialize(params object[] parameters)
        {
            _dataGeneric = (ItemMultiObjectEntry)parameters[0];
            _parent = (GameObject)_dataGeneric.Objects[0];
            _index = (int)_dataGeneric.Objects[1];
            _text = transform.Find("Text").GetComponent<Text>();
            _dataUserModel = (UserModel)_dataGeneric.Objects[2];
            _text.text = _dataUserModel.Nickname;
            _background = transform.GetComponent<Image>();
            transform.GetComponent<Button>().onClick.AddListener(SelectUser);
            transform.Find("Delete").GetComponent<Button>().onClick.AddListener(DeleteUser);

            UIEventController.Instance.Event += OnUIEvent;
        }

        void OnDestroy()
        {
            Destroy();
        }

        public bool Destroy()
        {
            if (_parent != null)
            {
                _parent = null;
                if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
                GameObject.Destroy(this.gameObject);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ApplyGenericAction(params object[] parameters)
        {
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent == EVENT_ITEM_USER_SELECTED)
            {
                if ((GameObject)parameters[0] == _parent)
                {
                    if ((GameObject)parameters[1] != this.gameObject)
                    {
                        Selected = false;
                    }
                }
            }
        }

        public void SelectUser()
        {
            ItemSelected();
        }

        private void DeleteUser()
        {
            ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenLoading, this.gameObject, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.wait"));
            Invoke("RunRemove", 1f);
        }

        public void RunRemove()
        {
            UIEventController.Instance.DispatchUIEvent(UsersController.EVENT_USER_REMOVE_SINGLE_RECORD, _dataUserModel.Id);
        }

        public void ItemSelected(bool dispatchEvent = true)
        {
            Selected = !Selected;
            UIEventController.Instance.DispatchUIEvent(EVENT_ITEM_USER_SELECTED, _parent, this.gameObject, (Selected ? _index : -1), _dataUserModel);
        }
    }
}