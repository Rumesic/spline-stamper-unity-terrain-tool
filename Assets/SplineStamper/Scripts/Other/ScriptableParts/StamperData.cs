using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StamperData
{
    [SerializeField] public float[] flatUndoHeights { get { return flatUndoHeightsSaved; } set { flatUndoHeightsSaved = value; } }
    [SerializeField] public float[] flatUndoSplats { get { return flatUndoSplatsSaved; } set { flatUndoSplatsSaved = value; } }


    [SerializeField] private float[] flatUndoHeightsSaved;
    [SerializeField] private float[] flatUndoSplatsSaved;

}
