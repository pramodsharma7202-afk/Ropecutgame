using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data asset describing one level layout: hero start, rope definitions,
/// static objects, goal position, star times, and a background index.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "RopeCut/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Hero")]
    public Vector2 heroStart;

    [Header("Ropes")]
    public List<RopeDef> ropes = new List<RopeDef>();

    [Header("Static objects")]
    public List<ObjectDef> objects = new List<ObjectDef>();

    [Header("Goal")]
    public Vector2 goalPos;

    [Header("Stars")]
    [Tooltip("Target times (in seconds) to complete the level and earn 1, 2, or 3 stars.")]
    public float[] starTimes = new float[3] { 30f, 20f, 10f };

    [Header("Presentation")]
    public int backgroundIndex;

    /// <summary>Rope definition: anchor position, segment count, target is the hero.</summary>
    [Serializable]
    public class RopeDef
    {
        public Vector2 anchorPos;
        public int segmentCount;
        public bool targetIsHero = true;
    }

    /// <summary>Static object definition placed in the level.</summary>
    [Serializable]
    public class ObjectDef
    {
        public enum PrefabType { Box, Spikes, Fire, Water, Door, Pin }

        public PrefabType prefabType;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public bool isStatic = true;
    }
}
