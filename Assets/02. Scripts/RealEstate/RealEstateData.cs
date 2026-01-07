using System;
using UnityEngine;

[System.Serializable]
public class RealEstateData {
    public string id;
    public string name;

    public long price; 

    public float monthlyYield;
    public string ownedText;
    public bool owned;
    public string imageKey;
    [System.NonSerialized]
    public Sprite image;
    public DateTime buyDate;
    public DateTime nextIncomeDate;
    public DateTime lastPriceUpdateDate;
}