using UnityEngine;
using UnityEngine.InputSystem;

// The retro / VHS post-process on the player's camera (plan section 2 and 4.17).
// Works with Unity's built-in renderer (this project's default). F10 toggles it.
// The body cam snapshots go through it too, so highlight clips look like tape.
[RequireComponent(typeof(Camera))]
public class RetroCameraEffect : MonoBehaviour
{
    [SerializeField] private Shader shader;
    [SerializeField, Tooltip("Size of the chunky pixels.")]
    private float pixelSize = 3f;
    [SerializeField, Tooltip("Colour levels per channel (lower = more retro).")]
    private float colorSteps = 24f;
    [SerializeField, Range(0f, 0.5f)] private float scanlines = 0.12f;
    [SerializeField, Range(0f, 0.3f)] private float grain = 0.06f;
    [SerializeField, Range(0f, 5f)] private float chroma = 1.5f;
    [SerializeField, Range(0f, 5f)] private float wobble = 1f;
    [SerializeField, Range(0f, 3f)] private float vignette = 0.9f;

    private Material material;
    private bool effectOn = true;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
        {
            effectOn = !effectOn;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!effectOn || shader == null || !shader.isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (material == null)
        {
            material = new Material(shader);
        }
        material.SetFloat("_PixelSize", pixelSize);
        material.SetFloat("_ColorSteps", colorSteps);
        material.SetFloat("_Scanlines", scanlines);
        material.SetFloat("_Grain", grain);
        material.SetFloat("_Chroma", chroma);
        material.SetFloat("_Wobble", wobble);
        material.SetFloat("_Vignette", vignette);
        Graphics.Blit(source, destination, material);
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}
