using System;
using System.Collections.Generic;
using TMPro;
using yourvrexperience.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using static yourvrexperience.Narration.GameLevelData;
#if ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using ZXing;
using ZXing.Common;
using Unity.Collections;
#endif
#if ENABLE_QR_META
using Meta.XR.MRUtilityKit;
#endif

namespace yourvrexperience.template6dof
{
#if ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META
    public static class QRSpaceConverter
    {
        private static Transform Area => NavMeshController.Instance.AreaMaxST.transform;

        private static Vector3 POILocal =>
            Area.InverseTransformPoint(MainController.Instance.LevelView.PlayingPOI.Root.transform.position);

        private static Quaternion POIRotation =>
            Quaternion.Euler(0, GetPOIYaw(MainController.Instance.LevelView.PlayingPOI.Forward), 0);

        public static void AreaToQR(Vector3 posArea, Quaternion rotArea, out Vector3 posQR, out Quaternion rotQR)
        {
            Quaternion inv = Quaternion.Inverse(POIRotation);
            posQR = inv * (posArea - POILocal);
            rotQR = inv * rotArea;
        }

        public static float GetPOIYaw(Vector3 forward)
        {
            Vector3 fwdLocal = Area.InverseTransformDirection(forward);
            fwdLocal.y = 0;
            return Mathf.Atan2(fwdLocal.x, fwdLocal.z) * Mathf.Rad2Deg;
        }

        public static void QRToArea(Vector3 posQR, Quaternion rotQR, out Vector3 posArea, out Quaternion rotArea)
        {
            Quaternion poiRot = POIRotation;
            posArea = poiRot * posQR + POILocal;
            rotArea = poiRot * rotQR;
        }

        public static void WorldToAreaFromQR(Transform obj, out Vector3 posArea, out Quaternion rotArea)
        {
            Transform qrRoot = QRImageController.Instance.GetContentRootQR(MainController.Instance.LevelView.PlayingQR);
            Vector3 posQR = qrRoot.InverseTransformPoint(obj.position);
            Quaternion rotQR = Quaternion.Inverse(qrRoot.rotation) * obj.rotation;
            QRToArea(posQR, rotQR, out posArea, out rotArea);
        }
    }
#endif

#if ENABLE_QR_MODE && !ENABLE_QR_META
    [RequireComponent(typeof(ARTrackedImageManager))]
#endif
    public class QRImageController : MonoBehaviour
#if ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META
    , ISerializationCallbackReceiver
#endif
    {
        public const string EventQRImageControllerReportQRDetected = "EventQRImageControllerReportQRDetected";
        public const string EventQRImageControllerReportQRLost = "EventQRImageControllerReportQRLost";
        public const string EventQRImageControllerReportQRInRange = "EventQRImageControllerReportQRInRange";
        public const string EventQRImageControllerReportQROutOfRange = "EventQRImageControllerReportQROutOfRange";

        public const float MIN_DISTANCE_TO_DETECT_QR = 2f;

        private static QRImageController _instance;

        public static QRImageController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(QRImageController)) as QRImageController;
                }
                return _instance;
            }
        }

#if ENABLE_QR_MODE || ENABLE_QR_XR || ENABLE_QR_META
        // Which local axis of the tracked pose points out of the printed QR, and which points to its top.
        // AR Foundation: +Y normal, +Z top. MRUK: VERIFY on device with the debug arrows (likely +Z normal, +Y top).
#if ENABLE_QR_META
        private static readonly Vector3 QR_NORMAL_AXIS = Vector3.forward;
        private static readonly Vector3 QR_TOP_AXIS = Vector3.up;
#else
        private static readonly Vector3 QR_NORMAL_AXIS = Vector3.up;
        private static readonly Vector3 QR_TOP_AXIS = Vector3.forward;
#endif

        private class DetectedQR
        {
            public Transform tracked;          // live pose from the platform (null while lost)
#if !ENABLE_QR_META
            public ARTrackedImage trackedImage;
#endif
            public GameObject detectedQR;      // debug marker + visibility collider
            public Transform contentRoot;      // gravity-aligned parent for narration content
            public bool isInRange;
            public bool isVisible;
            public string identity;
        }

        [SerializeField]
        [Tooltip("Prefab detection test QR")]
        GameObject prefabQR;

        [SerializeField]
        [Tooltip("Link to the camera used for AR")]
        Camera arCamera;

        private Dictionary<string, DetectedQR> _detectedQRs = new Dictionary<string, DetectedQR>();
        private HashSet<string> _knownIdentities = new HashSet<string>();
        private bool _hasAreaBeenDetected = true;

#if ENABLE_QR_META
        private List<MRUKTrackable> _seenTrackables = new List<MRUKTrackable>();
        private bool _detectionStarted = false;
#else
        private class PendingQR
        {
            public ARTrackedImage trackedImage;
            public int attempts;
        }

        [SerializeField]
        [Tooltip("Reference Image Library")]
        XRReferenceImageLibrary imageLibrary;

        [Tooltip("Camera Manager used to get the latest camera image")]
        [SerializeField] private ARCameraManager cameraManager;

        private ARTrackedImageManager _trackedImageManager;
        private BarcodeReaderGeneric _reader;
        private Dictionary<TrackableId, PendingQR> _pending = new Dictionary<TrackableId, PendingQR>();
        private HashSet<TrackableId> _assigned = new HashSet<TrackableId>();
        private float _nextDecodeTime;
        private const float DECODE_INTERVAL = 0.25f;

        public XRReferenceImageLibrary ImageLibrary
        {
            get { return imageLibrary; }
            set
            {
                imageLibrary = value;
                _trackedImageManager.referenceLibrary = imageLibrary;
            }
        }
#endif

        public bool HasAreaBeenDetected
        {
            get { return _hasAreaBeenDetected; }
        }

        public Camera ARCamera
        {
            get { return arCamera; }
        }

        void Awake()
        {
#if !ENABLE_QR_META
            _trackedImageManager = GetComponent<ARTrackedImageManager>();
            _reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                    TryHarder = false
                }
            };
#endif
        }

        void OnEnable()
        {
#if !ENABLE_QR_META
            _trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
#endif
        }

        void OnDisable()
        {
#if !ENABLE_QR_META
            _trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
#endif
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { }

        // ------------------------------------------------------------------
        // Common API
        // ------------------------------------------------------------------

        public void CreateImageLibrary(List<Asset> assets)
        {
            _knownIdentities.Clear();
            foreach (Asset qr in assets) _knownIdentities.Add(qr.Value);

#if ENABLE_QR_META
            // No library needed: the headset decodes the payload itself
            _detectionStarted = true;
            ProcessSeenTrackables();
#else
            var runtimeLib = _trackedImageManager.CreateRuntimeLibrary() as MutableRuntimeReferenceImageLibrary;
            _trackedImageManager.referenceLibrary = runtimeLib;
            _trackedImageManager.enabled = true;

            // texture must be Read/Write enabled, ideally RGBA32
            foreach (Asset qr in assets)
            {
                var qrTexture = AssetBundleController.Instance.CreateTexture(qr.Value);
                runtimeLib.ScheduleAddImageWithValidationJob(qrTexture, qr.Value, float.Parse(qr.Animations));
            }
#endif
            MainController.Instance.ApplyOclusionNavigation();
        }

        public void ClearAllDetections()
        {
            foreach (var detectedQR in _detectedQRs.Values)
            {
                if (detectedQR.detectedQR != null) Destroy(detectedQR.detectedQR);
                if (detectedQR.contentRoot != null) Destroy(detectedQR.contentRoot.gameObject);
            }
            _detectedQRs.Clear();

#if ENABLE_QR_META
            // MRUK owns its trackables: never destroy them. Re-evaluate the ones still present.
            ProcessSeenTrackables();
#else
            _pending.Clear();
            _assigned.Clear();
            FindFirstObjectByType<ARSession>()?.Reset(); // only if you really need fresh detections
#endif
        }

        public Vector3 GetPositionQR(string name)
        {
            GameObject detectedQR = GetGameObjectQR(name);
            return detectedQR != null ? detectedQR.transform.position : Vector3.zero;
        }

        public GameObject GetGameObjectQR(string name)
        {
            DetectedQR detectedQR = GetDetectedQR(name);
            return detectedQR != null ? detectedQR.detectedQR : null;
        }

        public Transform GetContentRootQR(string identity)
        {
            return _detectedQRs.TryGetValue(identity, out var d) ? d.contentRoot : null;
        }

        public bool CheckVisiblePoint(string nameVisibleImage)
        {
            DetectedQR detectedQR = GetDetectedQR(nameVisibleImage);
            return detectedQR != null && detectedQR.isInRange;
        }

        private DetectedQR GetDetectedQR(string name)
        {
            return _detectedQRs.TryGetValue(name, out DetectedQR detectedQR) ? detectedQR : null;
        }

        private bool IsAcceptedIdentity(string identity)
        {
            if (string.IsNullOrEmpty(identity)) return false;
            // Ignore unrelated QR codes (posters, tickets...) once the level's list is known
            return _knownIdentities.Count == 0 || _knownIdentities.Contains(identity);
        }

        private bool IsTracking(DetectedQR d)
        {
#if ENABLE_QR_META
            return d.tracked != null && d.tracked.gameObject.activeInHierarchy;
#else
            return d.trackedImage != null && d.trackedImage.trackingState == TrackingState.Tracking;
#endif
        }

        public static Quaternion GravityAlignedFrame(Quaternion qrRotation)
        {
            Vector3 normal = qrRotation * QR_NORMAL_AXIS; // out of the image
            Vector3 top = qrRotation * QR_TOP_AXIS;       // toward the top of the image

            // Wall QR: the visitor faces into the wall (-normal). Floor/table QR: use the image's top.
            Vector3 dir = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.7f ? top : -normal;
            dir = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;

            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private DetectedQR AssignPrefab(string identity, Transform tracked, float halfSize)
        {
            // Both objects live beside the trackable, not under it, so they survive
            // the platform destroying/recreating the trackable GameObject
            var root = new GameObject("QRContent_" + identity).transform;
            root.SetParent(tracked.parent, false);
            root.SetPositionAndRotation(tracked.position, GravityAlignedFrame(tracked.rotation));

            /*
            // VISUAL MARKER FOR DEBUGGING PURPOSES
            GameObject marker = Instantiate(prefabQR, tracked.parent);
            marker.transform.SetPositionAndRotation(tracked.position, tracked.rotation);
            marker.transform.localScale = Vector3.one * halfSize;
            marker.GetComponentInChildren<TextMeshProUGUI>().text = identity;
            */
            GameObject marker = new GameObject("Marker_"+identity);

            var detected = new DetectedQR
            {
                tracked = tracked,
                detectedQR = marker,
                contentRoot = root,
                isInRange = false,
                isVisible = false,
                identity = identity
            };
            _detectedQRs[identity] = detected;
            return detected;
        }

        private void UpdateContentRoots()
        {
            foreach (var item in _detectedQRs.Values)
            {
                // Only follow while really tracking, otherwise keep the last good pose (avoids jumps)
                if (!IsTracking(item)) continue;

                var t = item.tracked;
                item.detectedQR.transform.SetPositionAndRotation(t.position, t.rotation); // raw pose, for axis checks
                Quaternion targetRot = GravityAlignedFrame(t.rotation);
                item.contentRoot.position = Vector3.Lerp(item.contentRoot.position, t.position, 0.3f);
                item.contentRoot.rotation = Quaternion.Slerp(item.contentRoot.rotation, targetRot, 0.3f);
            }
        }

        // ------------------------------------------------------------------
        // MRUK backend (Meta Quest)
        // ------------------------------------------------------------------
#if ENABLE_QR_META
        // Hook to MRUK component > TrackableAdded
        public void OnTrackableAdded(MRUKTrackable trackable)
        {
            if (trackable == null || trackable.TrackableType != OVRAnchor.TrackableType.QRCode) return;
            if (!_seenTrackables.Contains(trackable)) _seenTrackables.Add(trackable);
            if (_detectionStarted) TryRegisterTrackable(trackable);
        }

        // Hook to MRUK component > TrackableRemoved
        public void OnTrackableRemoved(MRUKTrackable trackable)
        {
            _seenTrackables.Remove(trackable);
            foreach (var d in _detectedQRs.Values)
            {
                // Keep the content root at its last pose; just stop following
                if (d.tracked == trackable.transform) d.tracked = null;
            }
        }

        private void ProcessSeenTrackables()
        {
            _seenTrackables.RemoveAll(t => t == null);
            foreach (var trackable in _seenTrackables) TryRegisterTrackable(trackable);
        }

        private void TryRegisterTrackable(MRUKTrackable trackable)
        {
            string identity = trackable.MarkerPayloadString;
            if (!IsAcceptedIdentity(identity)) return;

            if (_detectedQRs.TryGetValue(identity, out DetectedQR existing))
            {
                existing.tracked = trackable.transform; // same QR re-detected: rebind
                return;
            }
            AssignPrefab(identity, trackable.transform, GetTrackableHalfSize(trackable));
        }

        private float GetTrackableHalfSize(MRUKTrackable trackable)
        {
            // VERIFY: PlaneRect holds the QR's 2D bounds in recent MRUK versions
            if (trackable.PlaneRect.HasValue)
            {
                Rect r = trackable.PlaneRect.Value;
                return Mathf.Min(r.width, r.height) / 2;
            }
            return 0.1f;
        }
#else
        // ------------------------------------------------------------------
        // AR Foundation + ZXing backend (phones)
        // ------------------------------------------------------------------
        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
            {
                _pending[trackedImage.trackableId] = new PendingQR { trackedImage = trackedImage };
            }
            foreach (var removed in eventArgs.removed)
            {
                _pending.Remove(removed.Key);
                _assigned.Remove(removed.Key);
                foreach (var d in _detectedQRs.Values)
                {
                    if (d.trackedImage == removed.Value) { d.trackedImage = null; d.tracked = null; }
                }
            }
        }

        private void ProcessPending()
        {
            _nextDecodeTime -= Time.deltaTime;
            if (_pending.Count == 0 || _nextDecodeTime > 0) return;
            _nextDecodeTime = DECODE_INTERVAL;

            var candidates = new List<PendingQR>(_pending.Values);
            if (candidates.Count == 0) return;

            int maxAttempts = 0;
            foreach (var c in candidates) { c.attempts++; maxAttempts = Mathf.Max(maxAttempts, c.attempts); }

            // Escalate quality after a few cheap attempts
            string identity = GetIdentity(maxAttempts > 3);
            if (!IsAcceptedIdentity(identity) || _detectedQRs.ContainsKey(identity)) return;

            // Prefer the image whose reference name matches; otherwise, if only one is pending,
            // trust the decoded content (it was probably matched to the wrong reference)
            PendingQR target = candidates.Find(c => c.trackedImage.referenceImage.name == identity);
            if (target == null && candidates.Count == 1) target = candidates[0];
            if (target == null) return;

            var img = target.trackedImage;
            float halfSize = Mathf.Min(img.size.x, img.size.y) / 2;
            DetectedQR detected = AssignPrefab(identity, img.transform, halfSize);
            detected.trackedImage = img;

            _pending.Remove(img.trackableId);
            _assigned.Add(img.trackableId);
        }

        private string GetIdentity(bool highQuality)
        {
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return null;

            using (image)
            {
                int div = highQuality ? 1 : 2; // full resolution when retrying
                var p = new XRCpuImage.ConversionParams(image, TextureFormat.R8, XRCpuImage.Transformation.None)
                {
                    outputDimensions = new Vector2Int(image.width / div, image.height / div)
                };

                var buffer = new NativeArray<byte>(image.GetConvertedDataSize(p), Allocator.Temp);
                try
                {
                    image.Convert(p, buffer);
                    _reader.Options.TryHarder = highQuality;
                    var source = new RGBLuminanceSource(buffer.ToArray(),
                        p.outputDimensions.x, p.outputDimensions.y,
                        RGBLuminanceSource.BitmapFormat.Gray8);
                    return _reader.Decode(source)?.Text;
                }
                finally
                {
                    buffer.Dispose();
                }
            }
        }
#endif

        // ------------------------------------------------------------------
        // Range / visibility events (shared)
        // ------------------------------------------------------------------
        private void Update()
        {
#if !ENABLE_QR_META
            ProcessPending();
#endif
            UpdateContentRoots();

            foreach (KeyValuePair<string, DetectedQR> item in _detectedQRs)
            {
                GameObject trackedObject = item.Value.detectedQR;
                if (trackedObject == null) continue;

                bool isVisible = yourvrexperience.Utils.Utilities.CheckInsideFrustrum(trackedObject.GetComponent<Collider>(), arCamera);
                bool isInFront = yourvrexperience.Utils.Utilities.CheckInFrontOfCamera(trackedObject.transform.position, arCamera);
                float distance = Vector3.Distance(arCamera.transform.position, trackedObject.transform.position);

                if (!item.Value.isInRange)
                {
                    if (distance <= MIN_DISTANCE_TO_DETECT_QR && isInFront)
                    {
                        item.Value.isInRange = true;
                        SystemEventController.Instance.DispatchSystemEvent(EventQRImageControllerReportQRInRange, item.Key);
                    }
                }
                else
                {
                    if (distance > MIN_DISTANCE_TO_DETECT_QR || !isInFront)
                    {
                        item.Value.isInRange = false;
                        SystemEventController.Instance.DispatchSystemEvent(EventQRImageControllerReportQROutOfRange, item.Key);
                    }
                }

                if (!item.Value.isVisible)
                {
                    if (distance <= MIN_DISTANCE_TO_DETECT_QR && isVisible)
                    {
                        item.Value.isVisible = true;
                        SystemEventController.Instance.DispatchSystemEvent(EventQRImageControllerReportQRDetected, item.Key);
                    }
                }
                else
                {
                    if (distance > MIN_DISTANCE_TO_DETECT_QR || !isVisible)
                    {
                        item.Value.isVisible = false;
                        SystemEventController.Instance.DispatchSystemEvent(EventQRImageControllerReportQRLost, item.Key);
                    }
                }
            }
        }
#endif
    }
}