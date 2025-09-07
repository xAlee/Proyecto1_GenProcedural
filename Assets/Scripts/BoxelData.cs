using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxelData
{
    public int x;
    public int y;
    public int type;
}

[System.Serializable]
public class BoxelDataCollection
{
    public List<BoxelData> boxels = new List<BoxelData>();
}