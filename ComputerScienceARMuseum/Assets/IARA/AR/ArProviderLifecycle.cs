using System.Collections;
using Iara.Application;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace Iara.AR
{
    // The AR scene owns manual initialization; global automatic startup stays disabled.
    public sealed class ArProviderLifecycle : MonoBehaviour
    {
        [SerializeField] private ARSession session;
        [SerializeField] private ARTrackedImageManager images;
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private ARCameraBackground background;
        [SerializeField] private ArTrackingController controller;
        [SerializeField] private ARInputManager inputManager;
        public void ConfigureInput(ARInputManager input) { inputManager = input; }
        private XRManagerSettings manager;
        private bool ownsLoader;
        public void Configure(ARSession value, ARTrackedImageManager imageManager, ARCameraManager camera,
            ARCameraBackground cameraBackground, ArTrackingController tracking)
        { session = value; images = imageManager; cameraManager = camera; background = cameraBackground; controller = tracking; }
        private IEnumerator Start()
        {
            if (UnityEngine.Application.isEditor)
            { controller.SetProviderStatus("Editor / simulated observations only; no camera tracking."); yield break; }
            if (UnityEngine.Application.platform != RuntimePlatform.Android)
            { controller.SetProviderStatus("The configured AR provider requires Android."); yield break; }
            while (string.IsNullOrEmpty(ExhibitSelection.ExhibitId)) yield return null;
            if (controller.State == null) yield break;
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null) { controller.SetProviderStatus("XR configuration unavailable."); yield break; }
            if (manager.activeLoader == null)
            {
                ownsLoader = true;
                controller.SetProviderStatus("Starting local ARCore provider...");
                yield return manager.InitializeLoader();
            }
            if (manager.activeLoader == null)
            { controller.SetProviderStatus("ARCore could not initialize. Check device support and prerequisites."); yield break; }
            session.attemptUpdate = false;
            yield return ARSession.CheckAvailability();
            if (ARSession.state != ARSessionState.Ready && ARSession.state != ARSessionState.SessionInitializing && ARSession.state != ARSessionState.SessionTracking)
            { controller.SetProviderStatus("AR support unavailable. Provision ARCore before offline use."); yield break; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                var timeout = Time.realtimeSinceStartupAsDouble + 30;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && Time.realtimeSinceStartupAsDouble < timeout)
                    yield return null;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                { controller.SetProviderStatus("Camera permission is required. Grant it in Android settings and reopen AR."); yield break; }
            }
#endif
            manager.StartSubsystems();
            if (inputManager != null) inputManager.enabled = true;
            cameraManager.enabled = true; background.enabled = true; images.enabled = true; session.enabled = true;
            controller.SetProviderStatus("");
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            if (images != null) images.enabled = false;
            if (session != null) session.enabled = false;
            if (background != null) background.enabled = false;
            if (cameraManager != null) cameraManager.enabled = false;
            if (inputManager != null) inputManager.enabled = false;
            if (ownsLoader && manager != null) { manager.StopSubsystems(); manager.DeinitializeLoader(); }
            ownsLoader = false;
        }
    }
}
