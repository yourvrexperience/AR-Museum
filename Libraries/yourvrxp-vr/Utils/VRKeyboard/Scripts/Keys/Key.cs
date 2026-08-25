using yourvrexperience.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
    public class Key : MonoBehaviour
    {
        protected Text key;

        public virtual void Awake()
        {
            key = transform.Find("Text").GetComponent<Text>();
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (key.text.Equals("ENTER"))
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerEnter);
                }
                else if (key.text.Equals("BACKSPACE"))
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerBackspace);
                }
                else if (key.text.Equals("CLEAR"))
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerClear);
                }
                else if (key.text.Equals("CAPSLOCK"))
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerCapsLock);
                }
                else if (key.text.Equals("SHIFT"))
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerShift);
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(KeyboardManager.EventKeyboardManagerKeycode, key.text);
                }                        
            });
        }

        public virtual void CapsLock(bool isUppercase) { }
        public virtual void ShiftKey() { }
    };
}