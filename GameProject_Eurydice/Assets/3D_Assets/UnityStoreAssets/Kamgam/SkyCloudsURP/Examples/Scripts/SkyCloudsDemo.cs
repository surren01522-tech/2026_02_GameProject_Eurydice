using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.SkyClouds
{
    public class SkyCloudsDemo : MonoBehaviour
    {
        public int FrameRate = -1;

        void Awake()
        {
            // Unlimited
            Application.targetFrameRate = FrameRate;
            QualitySettings.vSyncCount = 0;

            // Limited
            // QualitySettings.vSyncCount = 1;
        }
    }
}
