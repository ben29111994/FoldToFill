using UnityEngine;

namespace GPUInstancer
{

    public class GPUInstancerHiZOcclusionDebugger : MonoBehaviour
    {
#if UNITY_EDITOR
        private Shader debugShader = null;
        private Material debugMaterial = null;
        private GPUInstancerHiZOcclusionGenerator hiZOcclusionGenerator = null;

        [HideInInspector] public int debuggerHiZMipLevel = 0;

        private void OnEnable()
        {
            debugShader = Shader.Find(GPUInstancerConstants.SHADER_GPUI_HIZ_OCCLUSION_DEBUGGER);
            debugMaterial = new Material(debugShader);
            hiZOcclusionGenerator = FindObjectOfType<GPUInstancerHiZOcclusionGenerator>();

            if (hiZOcclusionGenerator != null && hiZOcclusionGenerator.isVREnabled)
            {
                if (GPUInstancerConstants.gpuiSettings.testBothEyesForVROcclusion)
                    debugMaterial.EnableKeyword("HIZ_TEXTURE_FOR_BOTH_EYES");

                if (GPUInstancerConstants.gpuiSettings.vrRenderingMode == 0)
                    debugMaterial.EnableKeyword("SINGLEPASS_VR_ENABLED");
                else
                    debugMaterial.EnableKeyword("MULTIPASS_VR_ENABLED");

            }

        }

        private void OnDisable()
        {
            debugShader = null;
            debugMaterial = null;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (hiZOcclusionGenerator != null)
            {
                debugMaterial.SetInt("_HiZMipLevel", debuggerHiZMipLevel);
                Graphics.Blit(hiZOcclusionGenerator.hiZDepthTexture, destination, debugMaterial);
            }
        }
#endif
    }
}