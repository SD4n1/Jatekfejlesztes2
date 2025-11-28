using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCarCategory", menuName = "Racing/Car Data")]
public class CarData : ScriptableObject
{
    public string categoryName;
    public Texture categoryImage;

    [Header("Itt sorold fel a csapatokat!")]
    public List<CarOption> carOptions;

    [System.Serializable]
    public class CarOption
    {
        public string teamName;
        public GameObject prefab;
    }
}