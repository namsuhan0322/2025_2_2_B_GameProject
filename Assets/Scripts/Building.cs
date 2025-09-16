using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Building : MonoBehaviour
{
    [Header("건물 정보")]
    public BuildingType BuildingType;
    public string buildingName = "건물";

    [System.Serializable]
    public class BuildingEvents
    {
        public UnityEvent<string> OnDriverEntered;
        public UnityEvent<string> OnDriverExited;
        public UnityEvent<BuildingType> OnServiceUsed;
    }

    public BuildingEvents buildingEvents;

    private DeliveryOrderSystem _orderSystem;

    void Start()
    {
        SetupBuilding();
        _orderSystem = FindObjectOfType<DeliveryOrderSystem>();

        CreateNameTag();
    }

    private void SetupBuilding()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            switch (BuildingType)
            {
                case BuildingType.Restaurant:
                    mat.color = Color.red;
                    break;
                case BuildingType.Customer:
                    mat.color = Color.green;
                    break;
                case BuildingType.ChargingStation:
                    mat.color = Color.yellow;
                    break;
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        DeliveryDriver driver = other.GetComponent<DeliveryDriver>();
        if (driver != null)
        {
            buildingEvents.OnDriverEntered?.Invoke(buildingName);
            HandleDriverService(driver);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DeliveryDriver driver = other.GetComponent<DeliveryDriver>();
        if (driver != null)
        {
            buildingEvents.OnDriverExited?.Invoke(buildingName);
            Debug.Log($"{buildingName} 을 떠났습니다.");
        }
    }

    private void CreateNameTag()
    {
        // 건물 위에 이름표 생성
        GameObject nameTag = new GameObject("NameTag");
        nameTag.transform.SetParent(transform);
        nameTag.transform.localPosition = Vector3.up * 1.5f;

        TextMesh textMesh = nameTag.AddComponent<TextMesh>();
        textMesh.text = buildingName;
        textMesh.characterSize = 0.2f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.white;
        textMesh.fontSize = 20;

        nameTag.AddComponent<BildBoard>();
    }

    private void HandleDriverService(DeliveryDriver driver)
    {
        switch (BuildingType)
        {
            case BuildingType.Restaurant:
                if (_orderSystem != null)
                    _orderSystem.OndriverEnteredRestaurant(this);
                break;
            case BuildingType.Customer:
                if (_orderSystem != null)
                    _orderSystem.OnDriverEnteredCustom(this);
                else
                    driver.CompleieDelivery();
                break;
            case BuildingType.ChargingStation:
                
                driver.ChargeBattery();
                break;
        }

        buildingEvents.OnServiceUsed?.Invoke(BuildingType);
    }
}
