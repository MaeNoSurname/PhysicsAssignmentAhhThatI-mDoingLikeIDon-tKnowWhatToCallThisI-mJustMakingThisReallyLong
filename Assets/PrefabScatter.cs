using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NoisePrefabScatter : MonoBehaviour
{
    [System.Serializable]
    public class PrefabRule
    {
        [Header("Prefab")]
        public GameObject prefab;

        [Header("Amount")]
        [Min(0)]
        public int amount = 50;

        [Header("Noise / Grouping")]
        [Tooltip("Higher values create smaller, more frequent noise patches.")]
        public float noiseScale = 0.1f;

        [Range(0f, 1f)]
        public float noiseThreshold = 0.5f;

        [Tooltip("Changes the noise pattern for this prefab.")]
        public Vector2 noiseOffset;

        [Range(0f, 1f)]
        public float groupingStrength = 1f;

        [Header("Spacing")]
        [Min(0f)]
        public float minimumSpacing = 1f;

        [Header("Transform")]
        public Vector2 scaleRange =
            new Vector2(0.8f, 1.2f);

        public bool randomYRotation = true;

        public Vector2 yRotationRange =
            new Vector2(0f, 360f);

        public float heightOffset = 0f;

        [Header("Surface")]
        public bool alignToSurfaceNormal = false;

        [Range(0f, 1f)]
        public float normalAlignmentStrength = 1f;
    }

    [Header("Scatter Area")]
    public Vector2 areaSize =
        new Vector2(20f, 20f);

    public float raycastHeight = 20f;
    public float raycastDistance = 50f;

    public LayerMask groundMask = ~0;

    [Header("Rules")]
    public List<PrefabRule> prefabRules =
        new List<PrefabRule>();

    [Header("Generation")]
    public int seed = 12345;

    [Min(1)]
    public int attemptsPerObject = 30;

    [Header("Hierarchy")]
    public bool createSeparateParents = true;

    [Header("Editor")]
    [Tooltip("Automatically regenerates whenever an Inspector value changes.")]
    public bool autoUpdate = true;

    private bool isGenerating = false;

#if UNITY_EDITOR
    private bool regenerateQueued = false;
#endif

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (isGenerating)
            return;

        isGenerating = true;

        Clear();

        Random.State previousRandomState =
            Random.state;

        Random.InitState(seed);

        foreach (PrefabRule rule in prefabRules)
        {
            if (
                rule.prefab == null ||
                rule.amount <= 0
            )
            {
                continue;
            }

            GenerateRule(rule);
        }

        Random.state =
            previousRandomState;

        isGenerating = false;
    }

    void GenerateRule(PrefabRule rule)
    {
        Transform parent = transform;

        if (createSeparateParents)
        {
            GameObject container =
                new GameObject(
                    rule.prefab.name +
                    "_Scatter"
                );

            container.transform.SetParent(
                transform
            );

            container.transform.localPosition =
                Vector3.zero;

            container.transform.localRotation =
                Quaternion.identity;

            container.transform.localScale =
                Vector3.one;

            parent =
                container.transform;
        }

        List<Vector3> placedPositions =
            new List<Vector3>();

        int placed = 0;

        int maximumAttempts =
            rule.amount *
            attemptsPerObject;

        int attempts = 0;

        while (
            placed < rule.amount &&
            attempts < maximumAttempts
        )
        {
            attempts++;

            Vector3 samplePosition =
                GetRandomSamplePosition();

            float noise =
                Mathf.PerlinNoise(
                    (
                        samplePosition.x +
                        rule.noiseOffset.x +
                        seed
                    ) *
                    rule.noiseScale,

                    (
                        samplePosition.z +
                        rule.noiseOffset.y +
                        seed
                    ) *
                    rule.noiseScale
                );

            float placementValue =
                Mathf.Lerp(
                    Random.value,
                    noise,
                    rule.groupingStrength
                );

            if (
                placementValue <
                rule.noiseThreshold
            )
            {
                continue;
            }

            Vector3 rayOrigin =
                samplePosition +
                Vector3.up *
                raycastHeight;

            if (
                !Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    raycastDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                continue;
            }

            Vector3 placementPosition =
                hit.point +
                hit.normal *
                rule.heightOffset;

            if (
                !HasEnoughSpacing(
                    placementPosition,
                    placedPositions,
                    rule.minimumSpacing
                )
            )
            {
                continue;
            }

            Quaternion rotation =
                CalculateRotation(
                    rule,
                    hit.normal
                );

            GameObject instance;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance =
                    (GameObject)
                    PrefabUtility.InstantiatePrefab(
                        rule.prefab,
                        parent
                    );

                instance.transform.SetPositionAndRotation(
                    placementPosition,
                    rotation
                );
            }
            else
#endif
            {
                instance =
                    Instantiate(
                        rule.prefab,
                        placementPosition,
                        rotation,
                        parent
                    );
            }

            float scale =
                Random.Range(
                    Mathf.Min(
                        rule.scaleRange.x,
                        rule.scaleRange.y
                    ),
                    Mathf.Max(
                        rule.scaleRange.x,
                        rule.scaleRange.y
                    )
                );

            instance.transform.localScale =
                Vector3.Scale(
                    instance.transform.localScale,
                    Vector3.one * scale
                );

            placedPositions.Add(
                placementPosition
            );

            placed++;
        }

        if (placed < rule.amount)
        {
            Debug.LogWarning(
                rule.prefab.name +
                ": only placed " +
                placed +
                " / " +
                rule.amount +
                ". Try lowering Minimum Spacing or Noise Threshold.",
                this
            );
        }
    }

    Vector3 GetRandomSamplePosition()
    {
        float x =
            Random.Range(
                -areaSize.x * 0.5f,
                areaSize.x * 0.5f
            );

        float z =
            Random.Range(
                -areaSize.y * 0.5f,
                areaSize.y * 0.5f
            );

        return transform.TransformPoint(
            new Vector3(
                x,
                0f,
                z
            )
        );
    }

    bool HasEnoughSpacing(
        Vector3 position,
        List<Vector3> existingPositions,
        float minimumSpacing
    )
    {
        if (minimumSpacing <= 0f)
            return true;

        float spacingSquared =
            minimumSpacing *
            minimumSpacing;

        for (
            int i = 0;
            i < existingPositions.Count;
            i++
        )
        {
            Vector3 difference =
                position -
                existingPositions[i];

            difference.y = 0f;

            if (
                difference.sqrMagnitude <
                spacingSquared
            )
            {
                return false;
            }
        }

        return true;
    }

    Quaternion CalculateRotation(
        PrefabRule rule,
        Vector3 surfaceNormal
    )
    {
        float yRotation = 0f;

        if (rule.randomYRotation)
        {
            yRotation =
                Random.Range(
                    rule.yRotationRange.x,
                    rule.yRotationRange.y
                );
        }

        Quaternion yawRotation =
            Quaternion.Euler(
                0f,
                yRotation,
                0f
            );

        if (!rule.alignToSurfaceNormal)
        {
            return yawRotation;
        }

        Quaternion surfaceRotation =
            Quaternion.FromToRotation(
                Vector3.up,
                surfaceNormal
            );

        Quaternion aligned =
            surfaceRotation *
            yawRotation;

        return Quaternion.Slerp(
            yawRotation,
            aligned,
            rule.normalAlignmentStrength
        );
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (
            int i = transform.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                transform.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(
                    child.gameObject
                );
            }
            else
            {
                DestroyImmediate(
                    child.gameObject
                );
            }
        }
    }

#if UNITY_EDITOR

    void OnValidate()
    {
        if (
            !autoUpdate ||
            Application.isPlaying ||
            isGenerating
        )
        {
            return;
        }


        if (regenerateQueued)
            return;

        regenerateQueued = true;

        EditorApplication.delayCall +=
            RegenerateFromEditor;
    }

    void RegenerateFromEditor()
    {
        regenerateQueued = false;

        if (
            this == null ||
            gameObject == null
        )
        {
            return;
        }

        if (
            !autoUpdate ||
            Application.isPlaying
        )
        {
            return;
        }

        Generate();

        EditorUtility.SetDirty(
            gameObject
        );
    }

#endif

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix =
            transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                areaSize.x,
                0.1f,
                areaSize.y
            )
        );
    }
}