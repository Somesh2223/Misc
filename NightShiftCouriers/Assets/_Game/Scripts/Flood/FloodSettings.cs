using System;
using UnityEngine;

// How the flood behaves over a shift (section 4.3). Create via Assets > Create > Night Shift > Flood Settings.
[CreateAssetMenu(fileName = "FloodSettings", menuName = "Night Shift/Flood Settings")]
public class FloodSettings : ScriptableObject
{
    [Serializable]
    public class Stage
    {
        public string name = "Stage";
        [Tooltip("Water surface height (metres, world Y) once this stage has fully risen.")]
        public float waterHeight = 0f;
        [Tooltip("Seconds this stage lasts before the next rise is warned about.")]
        public float duration = 60f;
    }

    [Tooltip("Stages in order. The first one is the starting water level.")]
    public Stage[] stages = new Stage[]
    {
        new Stage { name = "Dry streets", waterHeight = -0.5f, duration = 60f },
        new Stage { name = "Wading", waterHeight = 0.9f, duration = 60f },
        new Stage { name = "Deep", waterHeight = 2.2f, duration = 60f },
    };

    [Tooltip("Seconds of warning (siren) before each rise begins.")]
    public float warningSeconds = 8f;

    [Tooltip("How fast the water rises (metres per second).")]
    public float riseSpeed = 0.08f;
}
