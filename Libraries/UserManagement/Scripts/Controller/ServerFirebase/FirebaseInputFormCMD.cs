#if ENABLE_FIREBASE
using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using yourvrexperience.Utils;


namespace yourvrexperience.UserManagement
{
    public class FirebaseInputFormCMD : MonoBehaviour
    {
        public const bool DEBUG = false;

        public const string EVENT_FIREBASE_INPUT_FORM_CONFIRMED_REGISTER    = "EVENT_FIREBASE_INPUT_FORM_CONFIRMED_REGISTER";
        public const string EVENT_FIREBASE_INPUT_FORM_RESULT_ERROR          = "EVENT_FIREBASE_INPUT_FORM_RESULT_ERROR";
        public const string EVENT_FIREBASE_INPUT_FORM_DELAYED_DESTROY       = "EVENT_FIREBASE_INPUT_FORM_DELAYED_DESTROY";

        protected string _email;
        protected string _jsonFormData;
        protected long _registerdate;
        protected string _ip;

        protected bool _confirmedUser = false;
        protected bool _confirmedProfile = false;
        
        public static void CommandFirebaseInputForm(params object[] parameters)
        {
            GameObject command = new GameObject();
            command.AddComponent<FirebaseInputFormCMD>().Initialize(parameters);
            command.name = "FirebaseInputFormCMD";
        }
         
        public virtual void Initialize(params object[] _list)
        {
            SystemEventController.Instance.Event += OnSystemEvent;

            if (DEBUG) Debug.LogError("UserManagement::CreateNewUser::Checking existing user...");

            _email = (string)_list[0];
            _jsonFormData = (string)_list[1];
            _registerdate = yourvrexperience.Utils.Utilities.GetTimestamp();
            _ip = IpManager.GetIP(ADDRESSFAM.IPv4) + "|" + IpManager.GetIP(ADDRESSFAM.IPv6);

            SubmitNewInputForm();
        }

        public void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        protected void SubmitNewInputForm()
        {
            if (DEBUG) Debug.LogError("UserManagement::SubmitNewInputForm::Running Transaction...");
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference(FirebaseController.TABLE_INPUTFORMS);
            reference.RunTransaction(AddNewInputFormTransaction)
              .ContinueWith(task => {
                  if (task.Exception != null)
                  {
                      if (DEBUG) Debug.LogError("UserManagement::SubmitNewInputForm::EXCEPTION=" + task.Exception.Message);
                      SystemEventController.Instance.DispatchSystemEvent(EVENT_FIREBASE_INPUT_FORM_RESULT_ERROR);
                  }
                  else if (task.IsCompleted)
                  {
                      SystemEventController.Instance.DispatchSystemEvent(EVENT_FIREBASE_INPUT_FORM_CONFIRMED_REGISTER, true);
                  }
              });
        }

        protected TransactionResult AddNewInputFormTransaction(MutableData mutableData)
        {
            List<object> myInputForms = mutableData.Value as List<object>;

            if (myInputForms == null)
            {
                myInputForms = new List<object>();
            }

            Dictionary<string, object> newInputForm = new Dictionary<string, object>();
            newInputForm["email"] = _email;
            newInputForm["formdata"] = _jsonFormData;
            newInputForm["registerdate"] = _registerdate;            
            newInputForm["ip"] = _ip;
            myInputForms.Add(newInputForm);

            mutableData.Value = myInputForms;
            return TransactionResult.Success(mutableData);
        }

        protected virtual void OnSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_FIREBASE_INPUT_FORM_RESULT_ERROR)
            {
                SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_INPUT_FORM_RESULT, 1, false);
            }
            if (_nameEvent == EVENT_FIREBASE_INPUT_FORM_CONFIRMED_REGISTER)
            {
                SystemEventController.Instance.DelaySystemEvent(UsersController.EVENT_USER_INPUT_FORM_RESULT, 1, true);
            }
            if (_nameEvent == UsersController.EVENT_USER_INPUT_FORM_RESULT)
            {
                SystemEventController.Instance.DelaySystemEvent(EVENT_FIREBASE_INPUT_FORM_DELAYED_DESTROY, 2);
            }
            if (_nameEvent == EVENT_FIREBASE_INPUT_FORM_DELAYED_DESTROY)
            {
                GameObject.Destroy(this.gameObject);
            }            
        }
    }
}
#endif