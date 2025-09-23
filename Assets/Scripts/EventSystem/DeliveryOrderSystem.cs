using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryOrderSystem : MonoBehaviour
{
    [Header("주문 설정")]
    public float ordergenerateInterval = 15f;                              // 주문 생성시간
    public int maxActiveOrders = 8;                                         // 최대 주문 숫자

    [Header("게임 상태")]
    public int totalOrdersGenerated = 0;
    public int completedOrders = 0;
    public int expiredOrders = 0;

    // 주문 리스트
    private List<DeliveryOrder> _currentOrders = new List<DeliveryOrder>();

    // Building 참조
    private List<Building> _restaurants = new List<Building>();
    private List<Building> _customers = new List<Building>();

    // Event 시스템
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
        FindAllBuilding();                          // 건물 초기 셋업

        // 초기 주문 생성
        StartCoroutine(GenerateInitialOrders());
        // 주기적 주문 생성
        StartCoroutine(OrderGenerator());
        // 만료 체크
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

        Debug.Log($"음식점 {_restaurants.Count}개, 고객 {_customers.Count} 개 발견");
    }

    private void CreatenNewOrder()
    {
        if (_restaurants.Count == 0 || _customers.Count == 0) return;

        // 랜덤 음식점과 고객 선탠
        Building randomRestaurant = _restaurants[Random.Range(0, _restaurants.Count)];
        Building randomCustomer = _customers[Random.Range(0, _customers.Count)];

        // 같은 건물이면 다시 선택
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

    // 픽업 함수
    private void PickupOrder(DeliveryOrder order)
    {
        order.state = OrderState.PickedUp;
        orderEvents.OnOrderPickedUp?.Invoke(order);
    }

    private void CompleteOrder(DeliveryOrder order)
    {
        order.state = OrderState.Completed;
        completedOrders++;

        // 보상 지급
        if (_driver != null)
            _driver.AddMoney(order.reward);

        // 완료된 주문 제거
        _currentOrders.Remove(order);
        orderEvents.OnOrderCompleted.Invoke(order);
    }

    // 주문 취소 소멸
    private void ExpireOrder(DeliveryOrder order)
    {
        order.state = OrderState.Expired;
        expiredOrders++;

        _currentOrders.Remove(order);
        orderEvents.OnOrderExpired?.Invoke(order);
    }

    // UI 정보 제공
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

    // 주문 찾아주는 함수
    DeliveryOrder FindOrderForPickup(Building restaurant)
    {
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.restaurantBuilding == restaurant && order.state  == OrderState.WaitingPickup) return order;
        }

        return null;
    }

    // 주문 찾아주는 함수
    DeliveryOrder FindOrderForDelivery(Building customer)
    {
        foreach (DeliveryOrder order in _currentOrders)
        {
            if (order.customerBuilding == customer && order.state == OrderState.PickedUp) return order;
        }

        return null;
    }

    // Event 처리
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

        // 시작할때 3개 주문 생성
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

        GUILayout.Label("=== 배달 주문 ===");
        GUILayout.Label($"활성 주문: {_currentOrders.Count}개");
        GUILayout.Label($"픽업 대기: {GetPickWaitingCount()}개");
        GUILayout.Label($"배달 대기: {GetDeliveryWaitingCount()}개");
        GUILayout.Label($"완료: {completedOrders}개 | 만료: {expiredOrders}");

        GUILayout.Space(10);

        foreach (DeliveryOrder order in _currentOrders)
        {
            string status = order.state == OrderState.WaitingPickup ? "픽업대기" : "배달대기";
            float timeLeft = order.GetRemainingTime();

            GUILayout.Label($"#{order.orderID}:{order.restaurantName} -> {order.customerName}");
            GUILayout.Label($"#{status} | {timeLeft:F0} 초 남음");
        }

        GUILayout.EndArea();
    }
}

