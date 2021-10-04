using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SplineStamper : MonoBehaviour
{
    [Header("Ctrl + Left click - Add Point")]
    [Header("Controls")]

    [SerializeField]
    public StamperScriptable scriptablePath;
    public List<Stamper> creators = new List<Stamper>();

    public void CreatePathCreator(Vector3 pos)
    {
        GameObject temp = new GameObject("Stamper" + (creators.Count + 1).ToString());
        temp.transform.position = pos;
        temp.transform.parent = gameObject.transform;
        Stamper creator = temp.AddComponent<Stamper>();
        creator.splineStamper = this;
        AssignIndexes();

        StamperData newData = new StamperData();
        AddPathDataToScriptable(newData);
        creators.Add(creator);
    }

    public void DestroyAll()
    {
        for(int i = creators.Count - 1; i >= 0; i--)
        {
            GameObject toRemove = creators[i].gameObject;
            creators.Remove(creators[i]);
            DestroyImmediate(toRemove);
        }
        creators.TrimExcess();
    }

    public void AssignIndexes()
    {
        for (int i = creators.Count - 1; i >= 0; i--)
        {
            creators[i].indexInData = i;
        }
    }
    public void UpdateEditor()
    {
        for(int i = creators.Count - 1; i >= 0; i--)
        {
            if (creators[i] == null)
            {
                creators.RemoveAt(i);
                scriptablePath.stampDataList.RemoveAt(i);
            }
            
        }
        creators.TrimExcess();
        scriptablePath.stampDataList.TrimExcess();
        AssignIndexes();
    }

    private void Reset()
    {
        if (scriptablePath == null)
        {
            CreateAScriptableBecauseISaySo();
        }
    }


    public void CreateAScriptableBecauseISaySo()
    {
        scriptablePath = ScriptableObject.CreateInstance<StamperScriptable>();
        AssetDatabase.CreateAsset(scriptablePath, "Assets/" + "PathScriptable_" + Random.Range(0, 30000) + ".asset");
    }



    public void AddPathDataToScriptable(StamperData pathData)
    {
        if (scriptablePath != null)
        {
            scriptablePath.stampDataList.Add(pathData);
        }
    }


    public void RemoveScriptableDataAt(int indexToRemoveAt)
    {
        scriptablePath.stampDataList.RemoveAt(indexToRemoveAt);
        scriptablePath.stampDataList.TrimExcess();
    }
}
