using System;
using TMPro;
using UnityEngine;

namespace Magnet.Utilities
{
    public class FPSDisplay : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;
        [Tooltip("How much smoothing to apply (lower = less smoothing, 0 = no smoothing)")]
        public float smoothing = 0.9f;

        [Header("Color Settings")]
        public Color goodColor = Color.green;
        public Color mediumColor = Color.yellow;
        public Color badColor = Color.red;

        [Tooltip("FPS thresholds for color changes")]
        public int mediumThreshold = 45;
        public int badThreshold = 30;

        private float smoothedFPS;


        private void Start()
        {
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        }

        private void Update()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;

            smoothedFPS = Mathf.Lerp(currentFPS, smoothedFPS, smoothing);

            int fpsToDisplay = Mathf.RoundToInt(smoothedFPS);

            fpsText.text = $"FPS: {fpsToDisplay}";

            if (fpsToDisplay >= mediumThreshold)
            {
                fpsText.color = goodColor;
            }
            else if (fpsToDisplay >= badThreshold)
            {
                fpsText.color = mediumColor;
            }
            else
            {
                fpsText.color = badColor;
            }
        }
    }
}
