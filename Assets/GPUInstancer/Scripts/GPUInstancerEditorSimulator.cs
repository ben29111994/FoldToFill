﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    /// <summary>
    /// Simulate GPU Instancing while game is not running or when editor is paused
    /// </summary>
    public class GPUInstancerEditorSimulator
    {
        public GPUInstancerManager gpuiManager;
        public bool simulateAtEditor;
        public bool initializingInstances;
        public static GPUInstancerCameraData sceneViewCameraData = new GPUInstancerCameraData(null);

        public static readonly string sceneViewCameraName = "SceneCamera";

        public GPUInstancerEditorSimulator(GPUInstancerManager gpuiManager)
        {
            this.gpuiManager = gpuiManager;

            if (sceneViewCameraData == null)
                sceneViewCameraData = new GPUInstancerCameraData(null);
            sceneViewCameraData.renderOnlySelectedCamera = true;

            if (gpuiManager != null)
            {
                EditorApplication.update -= FindSceneViewCamera;
                EditorApplication.update += FindSceneViewCamera;
#if UNITY_2017_2_OR_NEWER
                EditorApplication.pauseStateChanged -= HandlePauseStateChanged;
                EditorApplication.pauseStateChanged += HandlePauseStateChanged;
#else
                EditorApplication.playmodeStateChanged = HandlePlayModeStateChanged;
#endif
            }
        }

        public void StartSimulation()
        {
            if ((Application.isPlaying && !EditorApplication.isPaused) || gpuiManager == null)
                return;

            initializingInstances = true;

            simulateAtEditor = true;
            EditorApplication.update -= FindSceneViewCamera;
            EditorApplication.update += FindSceneViewCamera;
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
#else
            EditorApplication.playmodeStateChanged = HandlePlayModeStateChanged;
#endif
        }

        public void StopSimulation()
        {
            if (!Application.isPlaying)
                gpuiManager.ClearInstancingData();

            simulateAtEditor = false;
            Camera.onPreCull -= CameraOnPreCull;
            EditorApplication.update -= EditorUpdate;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#endif
        }

        public void ClearEditorUpdates()
        {
            simulateAtEditor = false;
            Camera.onPreCull -= CameraOnPreCull;
            EditorApplication.update -= FindSceneViewCamera;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.pauseStateChanged -= HandlePauseStateChanged;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
#else
            EditorApplication.playmodeStateChanged = null;
#endif
        }

        private void FindSceneViewCamera()
        {
            if (sceneViewCameraData.mainCamera == null || sceneViewCameraData.mainCamera.name != sceneViewCameraName)
            {
                Camera currentCam = Camera.current;
                if (currentCam != null && currentCam.name == sceneViewCameraName)
                    sceneViewCameraData.SetCamera(currentCam);
                else
                    return;
            }
            EditorApplication.update -= FindSceneViewCamera;
        }

        private void EditorUpdate()
        {
            if (sceneViewCameraData.mainCamera != null && sceneViewCameraData.mainCamera.name == sceneViewCameraName)
            {
                if (initializingInstances)
                {
                    if (!gpuiManager.isInitialized)
                    {
                        gpuiManager.Awake();
                        gpuiManager.InitializeRuntimeDataAndBuffers();
                        if (gpuiManager.GetComponent<GPUInstancerLODColorDebugger>())
                            gpuiManager.GetComponent<GPUInstancerLODColorDebugger>().ChangeLODColors();
                    }
                    initializingInstances = false;
                    return;
                }

                Camera.onPreCull -= CameraOnPreCull;
                Camera.onPreCull += CameraOnPreCull;
                EditorApplication.update -= EditorUpdate;
            }
        }

        private void CameraOnPreCull(Camera cam)
        {
            if (sceneViewCameraData.mainCamera == cam)
            {
                gpuiManager.Update();
                gpuiManager.UpdateBuffers(sceneViewCameraData);
            }
        }


#if UNITY_2017_2_OR_NEWER        
        public void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            StopSimulation();
        }

        public void HandlePauseStateChanged(PauseState state)
        {
            if (gpuiManager == null)
            {
                EditorApplication.pauseStateChanged -= HandlePauseStateChanged;
                return;
            }
            if (Application.isPlaying)
            {
                switch (state)
                {
                    case PauseState.Paused:
                        StartSimulation();
                        break;
                    case PauseState.Unpaused:
                        StopSimulation();
                        break;
                }
            }
        }
#else
        public void HandlePlayModeStateChanged()
        {
            if (Application.isPlaying && EditorApplication.isPaused)
                StartSimulation();
            else
                StopSimulation();
        }
#endif // UNITY_2017_2_OR_NEWER
    }
}
#endif // UNITY_EDITOR