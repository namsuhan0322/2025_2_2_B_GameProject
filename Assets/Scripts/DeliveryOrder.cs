using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 간단한 배달 주문
[System.Serializable]
public class DeliveryOrder
{
    public int orderID;
    public string restaurantName;
    public string customerName;
    public Building restaurantBuilding;
    public Building customerBuilding;
    public float orederTime;
    public float timeLimit;
    public float reward;
    public OrderState state;

    // 생성자
    public DeliveryOrder(int id, Building restaurant, Building customer, float rewardAmount)
    {
        orderID = id;
        restaurantBuilding = restaurant;
        customerBuilding = customer;
        restaurantName = restaurant.buildingName;
        customerName = customer.buildingName;
        orederTime = Time.time;
        timeLimit = Random.Range(60f, 120f);                // 1분 ~ 2분 제한
        reward = rewardAmount;
        state = OrderState.WaitingPickup;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0f, timeLimit - (Time.time - orederTime));                 // 남은 시간 리턴
    }

    public bool IsExpired()                                                         // 주문 소멸
    {
        return GetRemainingTime() <= 0f;
    }
}
