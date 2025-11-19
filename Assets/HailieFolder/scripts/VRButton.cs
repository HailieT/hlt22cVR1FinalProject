using UnityEngine;
using UnityEngine.Events;

public class VRButton : MonoBehaviour
{
    [Header("Settings")]
    public UnityEvent onPressed; // Drag the function you want to run here in Inspector
    public string handTag = "PlayerHand"; // Make sure your VR hands have this tag!

    [Header("Visuals")]
    public Transform buttonTop; // The moving part of the button
    public float pressDistance = 0.02f; // How far it moves down
    public Material pressedMaterial; // Optional: color change when pressed

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPressed) return;

        // Check if the object touching the button is the hand
        // (Or if you don't want to use tags, remove this if check to let ANYTHING press it)
        if (other.CompareTag(handTag) || other.gameObject.name.ToLower().Contains("hand"))
        {
            PressButton();
        }
    }

    private void PressButton()
    {
        isPressed = true;
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
        buttonTop.localPosition = startPos;
        if (btnRenderer != null) btnRenderer.material = originalMaterial;
    }
}
