using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace yourvrexperience.Utils
{
    public class BinaryDataDownloader : MonoBehaviour
    {
        
        public const string EventBinaryDataDownloaderDataDownloadedSuccess = "EventBinaryDataDownloaderDataDownloadedSuccess";
        public const string EventBinaryDataDownloaderDataDownloadedFailure = "EventBinaryDataDownloaderDataDownloadedFailure";

        private static BinaryDataDownloader instance;

        public static BinaryDataDownloader Instance
        {
            get
            {
                if (!instance)
                {
                    instance = GameObject.FindObjectOfType(typeof(BinaryDataDownloader)) as BinaryDataDownloader;
                    if (!instance)
                    {
                        GameObject container = new GameObject();
                        DontDestroyOnLoad(container);
                        container.name = "BinaryDataDownloader";
                        instance = container.AddComponent(typeof(BinaryDataDownloader)) as BinaryDataDownloader;
                    }
                }
                return instance;
            }
        }

        public delegate void BinaryDownloadEvent(string nameEvent, byte[] data, params object[] parameters);

        public event BinaryDownloadEvent Event;

        public void DispatchBinaryDownloadEvent(string nameEvent, byte[] data, params object[] parameters)
        {
            if (Event != null) Event(nameEvent, data, parameters);
        }

        public void Run(string url, params object[] parameters)
        {
            StartCoroutine(DownloadBinaryData(url, parameters));
        }

        IEnumerator DownloadBinaryData(string url, object[] parameters)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = request.downloadHandler.data;
#if UNITY_EDITOR
                    Debug.Log($"Downloaded {data.Length} bytes.");
#endif
                    DispatchBinaryDownloadEvent(EventBinaryDataDownloaderDataDownloadedSuccess, data, parameters);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"Failed to download data: {request.error}");
#endif
                    DispatchBinaryDownloadEvent(EventBinaryDataDownloaderDataDownloadedFailure, null, parameters);
                }
            }
        }
    }
}