// FaceCamera.cs
// Place this in: Assets/Scripts/
//
// Makes any GameObject's sprite always face the main camera (billboard effect).
// Automatically added to the detective marker at runtime by TrapManager.

using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main == null) return;
        // Mirror the camera's forward so the sprite faces the viewer
        transform.forward = Camera.main.transform.forward;
    }
}
