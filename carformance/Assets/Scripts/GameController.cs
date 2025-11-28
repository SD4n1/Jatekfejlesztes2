using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [System.Serializable]
    public class SceneCarLink
    {
        public string name;
        public CarData categoryData;
        public int teamIndex;
        public GameObject carObject;
    }

    [Header("Húzgáld be az összes pályán lévõ kocsit!")]
    public List<SceneCarLink> carsInScene;

    [Header("Extrák")]
    public Camera mainCamera;

    void Start()
    {
        var manager = RaceManager.Instance;

        if (manager == null || manager.selectedCar == null)
        {
            Debug.LogError("Nincs adat a RaceManagerben! Menübõl indítsd!");
            return;
        }

        CarData selectedCategory = manager.selectedCar;
        int selectedTeamIndex = manager.selectedLiveryIndex;

        foreach (var link in carsInScene)
        {
            bool isMatch = (link.categoryData == selectedCategory && link.teamIndex == selectedTeamIndex);

            if (isMatch)
            {
                link.carObject.SetActive(true);

                if (mainCamera != null)
                {
                    mainCamera.transform.SetParent(link.carObject.transform);
                }
            }
            else
            {
                link.carObject.SetActive(false);
            }
        }
    }
}