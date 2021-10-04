using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SplineStamper/StamperScriptable"),]

[Serializable]
public class StamperScriptable : ScriptableObject
{
    [SerializeField] public List<StamperData> stampDataList = new List<StamperData>();

}
