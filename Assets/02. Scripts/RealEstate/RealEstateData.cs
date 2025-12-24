using System;
using UnityEngine;

[System.Serializable]
public class RealEstateData {
    public string id;
    public string name;
    public int price;
    public float monthlyYield;
    public string ownedText;

    // 추후 확장용
    public bool owned;

    // 이미지
    public string imageKey;   // ex) "estate_500"
    [System.NonSerialized]
    public Sprite image;      // 런타임 로드용

    public DateTime buyDate;
    public DateTime nextIncomeDate;
    public DateTime lastPriceUpdateDate;
}
