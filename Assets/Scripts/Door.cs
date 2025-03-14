using UnityEngine;
using TMPro;

public class Door : MonoBehaviour
{
    private bool isOpen;
    private bool isLocked;
    private Quaternion closedRotation; // Store the initial/closed rotation
    public float openAngle = 90f;      // Angle to rotate when opening
    public TextMeshProUGUI doorText;
    private bool allowUserInput = true;
    public float bargeForce = 10f; // Public variable to adjust barge force

    private Rigidbody rb; // Reference to the Rigidbody
    private Transform doorVisual; // Reference to the door visual transform

    private void Start()
    {
        isOpen = false;
        isLocked = false;
        closedRotation = transform.rotation;
        UpdateText();
        rb = GetComponent<Rigidbody>(); // Get Rigidbody component
        if (rb == null)
        {
            Debug.LogWarning("Door: Rigidbody component not found on the door. Barging will not apply physics force.");
        }

        // Find the door visual, assuming it's the first child
        if (transform.childCount > 0)
        {
            doorVisual = transform.GetChild(0);
        }
        else
        {
            Debug.LogError("Door: No child object found as door visual!");
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    public bool IsLocked()
    {
        return isLocked;
    }

    // --- Example Methods to control door state from outside ---
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        SetRotation(); // Update rotation to closed when locked
        UpdateText(); // Update text when locked state changes
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        SetRotation(); // Update rotation
        UpdateText(); // Update text when open state changes
    }

    public void AllowUserInput(bool allow)
    {
        allowUserInput = allow;
    }

    void Update()
    {
        if (allowUserInput)
        {
            // Keyboard input for door control
            if (Input.GetKeyDown(KeyCode.O))
            {
                isOpen = true;
                isLocked = false;
                SetRotation();
                UpdateText();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                isOpen = false;
                isLocked = false;
                SetRotation();
                UpdateText();
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                isOpen = false;
                isLocked = true;
                SetRotation();
                UpdateText();
            }
        }
    }

    public void OpenDoor()
    {
        isOpen = true;
        isLocked = false;
        SetRotation();
        UpdateText();
    }

    public void BargeDoor(Transform agentTransform) // Modified to accept agent's transform
    {
        isLocked = false; // Unlock the door first in case it's locked
        UpdateText();

        if (rb != null && agentTransform != null && doorVisual != null)
        {
            Vector3 doorPositionForForce = doorVisual.position; // Use door visual's position
            Vector3 direction = doorPositionForForce - agentTransform.position; // Direction from agent to door
            direction.y = 0; // Keep force horizontal
            direction.Normalize(); // Normalize to get direction vector of magnitude 1

            Debug.Log($"Door: Applying barge force: {bargeForce}, direction: {direction}, ForceMode: Impulse"); // Debug log
            Debug.Log($"Door: Door rotation before force: {transform.rotation.eulerAngles}"); // Debug log

            rb.AddForce(direction * bargeForce, ForceMode.Impulse); // Apply force

            Debug.Log($"Door: Door rotation after force: {transform.rotation.eulerAngles}"); // Debug log

            isOpen = true; // Set isOpen to true AFTER applying force
                           //SetRotation(); // Comment out SetRotation to allow physics to control rotation for barging
            Debug.Log("Door: Applied barge force and set isOpen = true.");
        }
        else
        {
            Debug.Log("Door: No Rigidbody, Agent Transform, or Door Visual found, cannot apply barge force.");
        }
    }

    private void SetRotation()
    {
        if (isOpen)
        {
            // Rotate to open position (around local Y-axis)
            transform.rotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        }
        else
        {
            // Rotate back to closed position
            transform.rotation = closedRotation;
        }
    }

    private void UpdateText()
    {
        if (isOpen)
        {
            doorText.text = "The door is open.";
        }
        else if (!isLocked)
        {
            doorText.text = "The door is closed.";
        }
        else
        {
            doorText.text = "The door is locked.";
        }
    }
}