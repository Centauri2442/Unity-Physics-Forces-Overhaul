// SPDX-FileCopyrightText: (c)2024-2025 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024-2025 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024-2025 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Magnet.Physics.EditorExtensions
{
    public static class GizmosExtensions
    {
        /// <summary>
        /// Draws 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="texture"></param>
        public static void DrawTextureAtPosition(Vector3 position, Texture2D texture, float size = 1f)
        {
            // Get the Scene view camera
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null) return;

            Camera sceneCamera = sceneView.camera;

            // Build camera-facing billboard corners
            Vector3 cameraRight = sceneCamera.transform.right * (size / 2f);
            Vector3 cameraUp = sceneCamera.transform.up * (size / 2f);

            Vector3 worldBottomLeft = position - cameraRight - cameraUp;
            Vector3 worldTopLeft = position - cameraRight + cameraUp;
            Vector3 worldTopRight = position + cameraRight + cameraUp;

            // Convert to screen (GUI) space
            Vector3 screenBottomLeft = HandleUtility.WorldToGUIPoint(worldBottomLeft);
            Vector3 screenTopLeft = HandleUtility.WorldToGUIPoint(worldTopLeft);
            Vector3 screenTopRight = HandleUtility.WorldToGUIPoint(worldTopRight);
            Vector3 screenCenter= HandleUtility.WorldToGUIPoint(position);

            // Compute screen-space size
            float screenWidth = Vector3.Distance(screenTopLeft, screenTopRight);
            float screenHeight = Vector3.Distance(screenTopLeft, screenBottomLeft);

            // Build centered rect
            Rect guiRect = new Rect(
                screenCenter.x - screenWidth / 2f,
                screenCenter.y - screenHeight / 2f,
                screenWidth,
                screenHeight
            );

            // Draw the texture in the Scene view
            Handles.BeginGUI();
            GUI.DrawTexture(guiRect, texture);
            Handles.EndGUI();
        }
    }
}

#endif