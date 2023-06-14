using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancer
{
    public class GPUInstancerHiZOcclusionGenerator : MonoBehaviour
    {
        public bool debuggerEnabled = false;
        [Range(0, 16)]
        public int debuggerHiZMipLevel = 0;
        public Camera mainCamera { get; set; }
        public Vector2 hiZTextureSize;
        public RenderTexture hiZDepthTexture;//{ get; private set; }
        [HideInInspector]
        public bool isVREnabled;

        private int _hiZMipLevels = 0;
        private int[] _hiZMipLevelIDs = null;
        private Shader _hiZShader = null;
        private Material _hiZMaterial = null;
        private CommandBuffer _hiZBuffer = null;

        private CameraEvent _cameraEvent = CameraEvent.AfterEverything;
#if UNITY_EDITOR
        private GPUInstancerHiZOcclusionDebugger _hiZOcclusionDebugger = null;
#endif
        private enum ShaderPass { SampleDepth, Reduce }

        private Vector2 _previousScreenSize;
        private bool _isInvalid;

        #region MonoBehaviour Methods

        private void Awake()
        {
            _hiZShader = Shader.Find(GPUInstancerConstants.SHADER_GPUI_HIZ_OCCLUSION_GENERATOR);
            _hiZMaterial = new Material(_hiZShader);
            hiZTextureSize = Vector2.zero;
        }

        private void OnDisable()
        {
            if (mainCamera != null)
            {
                if (_hiZBuffer != null)
                {
                    mainCamera.RemoveCommandBuffer(_cameraEvent, _hiZBuffer);
                    _hiZBuffer = null;
                }
            }

            if (hiZDepthTexture != null)
            {
                hiZDepthTexture.Release();
                hiZDepthTexture = null;
            }
        }

        private void LateUpdate()
        {
            if (mainCamera == null || _isInvalid)
                return;
                       
            if (_hiZBuffer == null || hiZDepthTexture == null)
            {
                Debug.LogWarning("GPUI HiZ Debpt buffer or texture is null where it should not be. Recreating them.");
                CreateHiZDepthTexture();
                CreateHiZCommandBuffer();
            }

            if (isVREnabled)
            {
#if UNITY_2017_2_OR_NEWER
                if (_previousScreenSize.x != UnityEngine.XR.XRSettings.eyeTextureWidth || _previousScreenSize.y != UnityEngine.XR.XRSettings.eyeTextureHeight)
                {
                    _previousScreenSize.x = UnityEngine.XR.XRSettings.eyeTextureWidth;
                    _previousScreenSize.y = UnityEngine.XR.XRSettings.eyeTextureHeight;
#else
                if (_previousScreenSize.x != UnityEngine.VR.VRSettings.eyeTextureWidth || _previousScreenSize.y != UnityEngine.VR.VRSettings.eyeTextureHeight)
                {
                    _previousScreenSize.x = UnityEngine.VR.VRSettings.eyeTextureWidth;
                    _previousScreenSize.y = UnityEngine.VR.VRSettings.eyeTextureHeight;
#endif
                    CreateHiZDepthTexture();
                    CreateHiZCommandBuffer();
                }


            }
            else
            {
                if (_previousScreenSize.x != mainCamera.pixelWidth || _previousScreenSize.y != mainCamera.pixelHeight)
                {
                    _previousScreenSize.x = mainCamera.pixelWidth;
                    _previousScreenSize.y = mainCamera.pixelHeight;

                    CreateHiZDepthTexture();
                    CreateHiZCommandBuffer();
                }
            }

            

#if UNITY_EDITOR
            if (debuggerEnabled)
            {
                if (_hiZOcclusionDebugger == null)
                    _hiZOcclusionDebugger = mainCamera.gameObject.AddComponent<GPUInstancerHiZOcclusionDebugger>();

                _hiZOcclusionDebugger.debuggerHiZMipLevel = debuggerHiZMipLevel;
            }

            if (!debuggerEnabled && _hiZOcclusionDebugger != null)
                DestroyImmediate(_hiZOcclusionDebugger);
#endif
        }


        #endregion


        #region Public Methods

        public void Initialize(Camera occlusionCamera = null)
        {
            _isInvalid = false;

            mainCamera = occlusionCamera != null ? occlusionCamera : gameObject.GetComponent<Camera>();

            if (mainCamera == null)
            {
                Debug.LogError("GPUI Hi-Z Occlision Culling Generator failed to initialize: camera not found.");
                _isInvalid = true;
                return;
            }

#if UNITY_2017_2_OR_NEWER
            if (UnityEngine.XR.XRSettings.enabled)
#else
            if (UnityEngine.VR.VRSettings.enabled)
#endif
            {
                isVREnabled = true;
#if UNITY_2017_2_OR_NEWER
                _previousScreenSize = new Vector2(UnityEngine.XR.XRSettings.eyeTextureWidth, UnityEngine.XR.XRSettings.eyeTextureHeight);
#else
                _previousScreenSize = new Vector2(UnityEngine.VR.VRSettings.eyeTextureWidth, UnityEngine.VR.VRSettings.eyeTextureHeight);
#endif

                if (mainCamera.stereoTargetEye != StereoTargetEyeMask.Both)
                {
                    Debug.LogError("GPUI Hi-Z Occlision works only for cameras that render to Both eyes. Disabling Occlusion Culling.");
                    _isInvalid = true;
                    return;
                }


            }
            else
            {
                isVREnabled = false;
                _previousScreenSize = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight);
            }

            mainCamera.depthTextureMode |= DepthTextureMode.Depth;

            CreateHiZDepthTexture();
            CreateHiZCommandBuffer();
        }

        #endregion


        #region Private Methods

        private void CreateHiZDepthTexture()
        {
            if (isVREnabled)
            {
#if UNITY_2017_2_OR_NEWER
                hiZTextureSize.x = Mathf.NextPowerOfTwo(UnityEngine.XR.XRSettings.eyeTextureWidth);
                hiZTextureSize.y = Mathf.NextPowerOfTwo(UnityEngine.XR.XRSettings.eyeTextureHeight);
#else
                hiZTextureSize.x = Mathf.NextPowerOfTwo(UnityEngine.VR.VRSettings.eyeTextureWidth);
                hiZTextureSize.y = Mathf.NextPowerOfTwo(UnityEngine.VR.VRSettings.eyeTextureHeight);
#endif
            }
            else
            {
                hiZTextureSize.x = Mathf.NextPowerOfTwo(mainCamera.pixelWidth);
                hiZTextureSize.y = Mathf.NextPowerOfTwo(mainCamera.pixelHeight);
            }

            _hiZMipLevels = (int)Mathf.Floor(Mathf.Log(hiZTextureSize.x, 2f));

            if (hiZTextureSize.x <= 0 || hiZTextureSize.y <= 0 || _hiZMipLevels == 0)
            {
                if (hiZDepthTexture != null)
                {
                    hiZDepthTexture.Release();
                    hiZDepthTexture = null;
                }

                if (_hiZBuffer != null)
                {
                    mainCamera.RemoveCommandBuffer(_cameraEvent, _hiZBuffer);
                    _hiZBuffer = null;
                }

                Debug.LogError("Cannot create GPUI HiZ Depth Texture: Screen size is too small.");
                return;
            }

            if (hiZDepthTexture != null)
                hiZDepthTexture.Release();

            hiZDepthTexture = new RenderTexture((int)hiZTextureSize.x, (int)hiZTextureSize.y, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            hiZDepthTexture.name = "GPUIHiZDepthTexture";
            hiZDepthTexture.filterMode = FilterMode.Point;
            hiZDepthTexture.useMipMap = true;
            hiZDepthTexture.autoGenerateMips = false;
            hiZDepthTexture.Create();
            hiZDepthTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        private void CreateHiZCommandBuffer()
        {
            if (hiZDepthTexture == null)
                return;

            //Debug.Log("(Re)creating the GPUI HiZ buffer...");

            _hiZMipLevelIDs = new int[_hiZMipLevels];

            if (_hiZBuffer != null)
                mainCamera.RemoveCommandBuffer(_cameraEvent, _hiZBuffer);

            _hiZBuffer = new CommandBuffer();
            _hiZBuffer.name = "GPUI Hi-Z Buffer";

            if (isVREnabled)
            {
                if (GPUInstancerConstants.gpuiSettings.testBothEyesForVROcclusion)
                    _hiZMaterial.EnableKeyword("HIZ_TEXTURE_FOR_BOTH_EYES");

#if UNITY_2018_3_OR_NEWER
                // Set the correct vr rendering mode if it is 2018.3 or later automatically
                GPUInstancerConstants.gpuiSettings.vrRenderingMode = UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePass ? 0 : 1;
#endif

                if (GPUInstancerConstants.gpuiSettings.vrRenderingMode == 0)
                {
                    _hiZBuffer.name += " VR SinglePass";
                    _hiZMaterial.EnableKeyword("SINGLEPASS_VR_ENABLED");
                }
                else
                {
                    _hiZBuffer.name += " VR MultiPass";
                    _hiZMaterial.EnableKeyword("MULTIPASS_VR_ENABLED");
                }
            }

            RenderTargetIdentifier id = new RenderTargetIdentifier(hiZDepthTexture);

            int width = (int)hiZTextureSize.x;
            int height = (int)hiZTextureSize.y;

            _hiZBuffer.Blit(null, id, _hiZMaterial, (int)ShaderPass.SampleDepth);

            for (int i = 0; i < _hiZMipLevels; ++i)
            {
                _hiZMipLevelIDs[i] = Shader.PropertyToID("GPUI_" + hiZDepthTexture.name + "_" + "Mip_Level_" + i.ToString());

                width = width >> 1;
                
                height = height >> 1;

                if (width == 0)
                    width = 1;

                if (height == 0)
                    height = 1;

                _hiZBuffer.GetTemporaryRT(_hiZMipLevelIDs[i], width, height, 0, FilterMode.Point, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);

                if (i == 0)
                    _hiZBuffer.Blit(id, _hiZMipLevelIDs[0], _hiZMaterial, (int)ShaderPass.Reduce);
                else
                    _hiZBuffer.Blit(_hiZMipLevelIDs[i - 1], _hiZMipLevelIDs[i], _hiZMaterial, (int)ShaderPass.Reduce);

                _hiZBuffer.CopyTexture(_hiZMipLevelIDs[i], 0, 0, id, 0, i + 1);

                if (i >= 1)
                    _hiZBuffer.ReleaseTemporaryRT(_hiZMipLevelIDs[i - 1]);
            }

            _hiZBuffer.ReleaseTemporaryRT(_hiZMipLevelIDs[_hiZMipLevels - 1]);

            mainCamera.AddCommandBuffer(_cameraEvent, _hiZBuffer);
        }

        #endregion
    }

}