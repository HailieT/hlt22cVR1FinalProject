using UnityEngine;
using UnityEngine.Events;

public class VRButton : MonoBehaviour
{
    [Header("Settings")]
    public UnityEvent onPressed; // Drag the function you want to run here in Inspector

    // CHANGED: Replaced string tag with LayerMask for faster, more reliable detection
    [Tooltip("Set this to the Layer your VR Hands are on (e.g., 'XR Hands')")]
    public LayerMask activatorLayer;

    [Header("Visuals")]
    public Transform buttonTop; // The moving part of the button
    public float pressDistance = 0.02f; // How far it moves down
    public Material pressedMaterial; // Optional: color change when pressed

    [Header("Debug")]
    public bool debugMode = false; // Enable to see collision messages

    private Vector3 startPos;
    private bool isPressed = false;
    private Material originalMaterial;
    private Renderer btnRenderer;

    private void Start()
    {
        if (buttonTop == null) buttonTop = transform;
        startPos = buttonTop.localPosition;

        btnRenderer = buttonTop.GetComponent<Renderer>();
        if (btnRenderer != null) originalMaterial = btnRenderer.material;

        // Verify setup
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"VRButton '{gameObject.name}' needs a Collider component!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"VRButton '{gameObject.name}' collider should be set to 'Is Trigger'!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // CHANGED: Use bitwise operation to check if the object's layer is in our mask
        if (((1 << other.gameObject.layer) & activatorLayer) != 0)
        {
            if (debugMode)
            {
                Debug.Log($"Button '{gameObject.name}' touched by hand: {other.gameObject.name}");
            }

            if (!isPressed)
            {
                PressButton();
            }
        }
    }

    private void PressButton()
    {
        isPressed = true;

        if (debugMode)
        {
            Debug.Log($"Button '{gameObject.name}' PRESSED!");
        }

        onPressed.Invoke(); // Run the function assigned in Unity

        // Visual feedback: Move button down
        buttonTop.localPosition = startPos - new Vector3(0, pressDistance, 0);

        // Visual feedback: Change color
        if (pressedMaterial != null && btnRenderer != null)
            btnRenderer.material = pressedMaterial;

        // Reset button after 0.5 seconds
        Invoke("ResetButton", 0.5f);
    }

    private void ResetButton()
    {
        isPressed = false;

        // Visual feedback: Return to start
        buttonTop.localPosition = startPos;

        // Visual feedback: Return to original color
        if (originalMaterial != null && btnRenderer != null)
            btnRenderer.material = originalMaterial;
    }
}