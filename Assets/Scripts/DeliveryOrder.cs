using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ������ ��� �ֹ�
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

    // ������
    public DeliveryOrder(int id, Building restaurant, Building customer, float rewardAmount)
    {
        orderID = id;
        restaurantBuilding = restaurant;
        customerBuilding = customer;
        restaurantName = restaurant.buildingName;
        customerName = customer.buildingName;
        orederTime = Time.time;
        timeLimit = Random.Range(60f, 120f);                // 1�� ~ 2�� ����
        reward = rewardAmount;
        state = OrderState.WaitingPickup;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0f, timeLimit - (Time.time - orederTime));                 // ���� �ð� ����
    }

    public bool IsExpired()                                                         // �ֹ� �Ҹ�
    {
        return GetRemainingTime() <= 0f;
    }
}
