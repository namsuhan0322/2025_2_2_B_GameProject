using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryOrderSystem : MonoBehaviour
{
    [Header("�ֹ� ����")]
    public float ordergenerateInterval = 15f;                              // �ֹ� �����ð�
    public int maxActiveOrders = 8;                                         // �ִ� �ֹ� ����

    [Header("���� ����")]
    public int totalOrdersGenerated = 0;
    public int completedOrders = 0;
    public int expiredOrders = 0;

    // �ֹ� ����Ʈ
    private List<DeliveryOrder> _currentOrders = new List<DeliveryOrder>();

    // Building ����
    private List<Building> _restaurants = new List<Building>();
    private List<Building> _customers = new List<Building>();

    // Event �ý���
    [System.Serializable]
    public class OrderSystemEvents
    {
        public UnityEvent<DeliveryOrder> OnNewOrderAdded;
        public UnityEvent<DeliveryOrder> OnOrderPickedUp;
        public UnityEvent<DeliveryOrder> OnOrderCompleted;
        public UnityEvent<DeliveryOrder> OnOrderExpired;
    }

    public OrderSystemEvents orderEvents;
    private DeliveryDriver _driver;

    void Start()
    {
        _driver = FindObjectOfType<DeliveryDriver>();
        FindAllBuilding();                          // �ǹ� �ʱ� �¾�

        // �ʱ� �ֹ� ����
        StartCoroutine(GenerateInitialOrders());
        // �ֱ��� �ֹ� ����
        StartCoroutine(OrderGenerator());
        // ���� üũ
        StartCoroutine(ExpiredOrderChecker());
    }

    private void FindAllBuilding()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();

        foreach (Building building in allBuildings)
        {
            if (building.BuildingType == BuildingType.Restaurant)
                _restaurants.Add(building);
            else if (building.BuildingType == BuildingType.Customer)
                _customers.Add(building);
        }

        Debug.Log($"������ {_restaurants.Count}��, �� {_customers.Count} �� �߰�");
    }

    private void CreatenNewOrder()
    {
        if (_restaurants.Count == 0 || _customers.Count == 0) return;

        // ���� �������� �� ����
        Building randomRestaurant = _restaurants[Random.Range(0, _restaurants.Count)];
        Building randomCustomer = _customers[Random.Range(0, _customers.Count)];

        // ���� �ǹ��̸� �ٽ� ����
        if (randomRestaurant == randomCustomer)
            randomCustomer = _customers[Random.Range(0, _customers.Count)];

        float reward = Random.Range(3000f, 8000f);

        DeliveryOrder newOrder = new DeliveryOrder(
            ++totalOrdersGenerated,
            randomRestaurant,
            randomCustomer,
            reward
            );

        _currentOrders.Add(newOrder);
        orderEvents.OnNewOrderAdded?.Invoke(newOrder);
    }

    // �Ⱦ� �Լ�
    private void PickupOrder(DeliveryOrder order)
    {
        order.state = OrderState.PickedUp;
        orderEvents.OnOrderPickedUp?.Invoke(order);
    }

    private void CompleteOrder(DeliveryOrder order)
    {
        order.state = OrderState.Completed;
        completedOrders++;

        // ���� ����
        if (_driver != null)
            _driver.AddMoney(order.reward);

        // �Ϸ�� �ֹ� ����
        _currentOrders.Remove(order);
        orderEvents.OnOrderCompleted.Invoke(order);
    }

    // �ֹ� ��� �Ҹ�
    private void ExpireOrder(DeliveryOrder order)
    {
        order.state = OrderState.Expired;
        expiredOrders++;

        _currentOrders.Remove(order);
        orderEvents.OnOrderExpired?.Invoke(order);
    }

    // UI ���� ����
    public List<DeliveryOrder> GetCurrentOrders()
    {
        return new List<DeliveryOrder>(_currentOrders);
    }

    public int GetPickWaitingCount()
    {
        int count = 0;
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.state == OrderState.WaitingPickup) count++;
        }
        return count;
    }

    public int GetDeliveryWaitingCount()
    {
        int count = 0;
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.state == OrderState.PickedUp) count++;
        }
        return count;
    }

    // �ֹ� ã���ִ� �Լ�
    DeliveryOrder FindOrderForPickup(Building restaurant)
    {
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.restaurantBuilding == restaurant && order.state  == OrderState.WaitingPickup) return order;
        }

        return null;
    }

    // �ֹ� ã���ִ� �Լ�
    DeliveryOrder FindOrderForDelivery(Building customer)
    {
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.customerBuilding == customer && order.state == OrderState.PickedUp) return order;
        }

        return null;
    }

    // Event ó��
    public void OndriverEnteredRestaurant(Building restaurant)
    {
        DeliveryOrder orderToPickup = FindOrderForPickup(restaurant);

        if (orderToPickup != null)
            PickupOrder(orderToPickup);
    }

    public void OnDriverEnteredCustom(Building customer)
    {
        DeliveryOrder orderToDelivery = FindOrderForDelivery(customer);

        if (orderToDelivery != null)
            CompleteOrder(orderToDelivery);
    }

    IEnumerator GenerateInitialOrders()
    {
        yield return new WaitForSeconds(1f);

        // �����Ҷ� 3�� �ֹ� ����
        for (int i = 0; i < 3; i++)
        {
            CreatenNewOrder();
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator OrderGenerator()
    {
        while (true)
        {
            yield return new WaitForSeconds(ordergenerateInterval);

            if (_currentOrders.Count < maxActiveOrders)
                CreatenNewOrder();
        }
    }

    IEnumerator ExpiredOrderChecker()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            List<DeliveryOrder> expiredOrders = new List<DeliveryOrder>();

            foreach (DeliveryOrder order in _currentOrders)
            {
                if (order.IsExpired() && order.state != OrderState.Completed)
                    expiredOrders.Add(order);
            }

            foreach (DeliveryOrder expired in expiredOrders)
            {
                ExpireOrder(expired);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 1300));

        GUILayout.Label("=== ��� �ֹ� ===");
        GUILayout.Label($"Ȱ�� �ֹ�: {_currentOrders.Count}��");
        GUILayout.Label($"�Ⱦ� ���: {GetPickWaitingCount()}��");
        GUILayout.Label($"��� ���: {GetDeliveryWaitingCount()}��");
        GUILayout.Label($"�Ϸ�: {completedOrders}�� | ����: {expiredOrders}");

        GUILayout.Space(10);

        foreach (DeliveryOrder order in _currentOrders)
        {
            string status = order.state == OrderState.WaitingPickup ? "�Ⱦ����" : "��޴��";
            float timeLeft = order.GetRemainingTime();

            GUILayout.Label($"#{order.orderID}:{order.restaurantName} -> {order.customerName}");
            GUILayout.Label($"#{status} | {timeLeft:F0} �� ����");
        }

        GUILayout.EndArea();
    }
}

