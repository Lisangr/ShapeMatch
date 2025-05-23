#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class ReplacementRule
{
    public string nameContains;
    public GameObject[] replacementPrefabs;
}

public class ReplaceGrassEditor : MonoBehaviour
{
    public ReplacementRule[] replacementRules;
    public bool searchInChildren = true;
    public bool showDebugLogs = true;

    [MenuItem("Tools/Replace Objects By Name")]
    static void ReplaceObjects()
    {
        ReplaceGrassEditor instance = FindObjectOfType<ReplaceGrassEditor>();
        if (instance == null)
        {
            return;
        }

        Undo.SetCurrentGroupName("Mass Replacement");
        int group = Undo.GetCurrentGroup();

        try
        {
            foreach (var rule in instance.replacementRules)
            {
                if (string.IsNullOrEmpty(rule.nameContains) || rule.replacementPrefabs == null || rule.replacementPrefabs.Length == 0)
                {
                     continue;
                }

  List<TransformData> targets = new List<TransformData>();
                foreach (GameObject root in GetSceneRoots())
                {
                    FindTransformsByNameRecursive(
                        root.transform,
                        rule.nameContains,
                        ref targets,
                        instance.searchInChildren
                    );
                }

                if (instance.showDebugLogs)
                   
                foreach (var transformData in targets)
                {
                    ReplaceObject(transformData, rule.replacementPrefabs);
                }
            }
        }
        finally
        {           
            Undo.CollapseUndoOperations(group);
        }
    }

    static void ReplaceObject(TransformData oldTransformData, GameObject[] prefabs)
    {
        GameObject newPrefab = prefabs[Random.Range(0, prefabs.Length)];

        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);

        newObj.transform.SetPositionAndRotation(
            oldTransformData.position,
            oldTransformData.rotation
        );
        newObj.transform.localScale = oldTransformData.scale;
        newObj.transform.SetParent(oldTransformData.parent);

         if (oldTransformData.gameObject != null)
        {
            Undo.DestroyObjectImmediate(oldTransformData.gameObject);
        }

        Undo.RegisterCreatedObjectUndo(newObj, "Replace object");
    }

    static List<GameObject> GetSceneRoots()
    {
        List<GameObject> roots = new List<GameObject>();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded) roots.AddRange(scene.GetRootGameObjects());
        }
        return roots;
    }

    static void FindTransformsByNameRecursive(
        Transform current,
        string searchPattern,
        ref List<TransformData> results,
        bool searchInChildren
    )
    {
        if (current.name.IndexOf(searchPattern, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            results.Add(new TransformData(current));
            if (!searchInChildren) return;
        }

        foreach (Transform child in current)
        {
            FindTransformsByNameRecursive(child, searchPattern, ref results, searchInChildren);
        }
    }

    private struct TransformData
    {
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly Vector3 scale;
        public readonly Transform parent;
        public readonly GameObject gameObject;

        public TransformData(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
            scale = t.localScale;
            parent = t.parent;
            gameObject = t.gameObject;
        }
    }
}
#endif