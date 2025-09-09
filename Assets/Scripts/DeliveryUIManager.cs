using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryUIManager : MonoBehaviour
{
    [Header("UI 요소")]
    public Text statusText;
    public Text messageText;
    public Slider batterySlider;
    public Image batteryFill;

    [Header("게임 오브젝트")]
    public DeliveryDriver driver;

    void Start()
    {
        if (driver != null)
        {
            // Event 구독
            driver.driverEvents.OnMoneyChanged.AddListener(UpdateMoney);
            driver.driverEvents.OnBatteryChanged.AddListener(UpdateBattery);
            driver.driverEvents.OnDeliveryCountChanged.AddListener(UpdateDeliveryCount);
            driver.driverEvents.OnMoveStarted.AddListener(OnMoveStarted);
            driver.driverEvents.OnMoveStoped.AddListener(OnMoveStopped);
            driver.driverEvents.OnLowBattery.AddListener(OnLowBattery);
            driver.driverEvents.OnLowBatteryEmpty.AddListener(OnBatteryEmpty);
            driver.driverEvents.OnDeliveryCompleted.AddListener(OnDeliveryCompleted);
        }

        UpdateUI();
    }

    void Update()
    {
        if (statusText != null && driver != null)
            statusText.text = driver.GetStatusText();
    }

    private void ShowMessage(string message, Color color)               // 메세지 표시 함수
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
            StartCoroutine(ClearMessageAgterDelay(2f));
        }
    }

    private IEnumerator ClearMessageAgterDelay(float delay)             // 일정 시간 지난 후 사라진다.
    {
        yield return new WaitForSeconds(delay);
        if (messageText != null)
            messageText.text = "";
    }

    private void UpdateMoney(float money)
    {
        ShowMessage($"돈 : {money} 원", Color.green);
    }

    private void UpdateBattery(float battery)
    {
        if (batterySlider != null)
            batterySlider.value = battery / 100f;

        if (batteryFill != null)
        {
            if (battery > 50f)
                batteryFill.color = Color.green;
            else if (battery > 20f)
                batteryFill.color = Color.yellow;
            else 
                batteryFill.color = Color.red;
        }
    }

    private void UpdateDeliveryCount(int count)
    {
        ShowMessage($"배달 완료 : {count}건", Color.blue);
    }

    private void OnMoveStarted()
    {
        ShowMessage($"이동 시작", Color.cyan);
    }

    private void OnMoveStopped()
    {
        ShowMessage($"이동 정지", Color.grey);
    }

    private void OnLowBattery()
    {
        ShowMessage($"배터리 부족!", Color.red);
    }

    private void OnBatteryEmpty()
    {
        ShowMessage($"배터리 방전!", Color.red);
    }

    private void OnDeliveryCompleted()
    {
        ShowMessage($"배달 완료!", Color.green);
    }

    private void UpdateUI()
    {
        if (driver != null)
        {
            UpdateMoney(driver.currentMoney);
            UpdateBattery(driver.batteryLevel);
            UpdateDeliveryCount(driver.deliveryCount);
        }
    }

    private void OnDestroy()                    // 객체가 파괴 된때 시점
    {
        if (driver != null)
        {
            // Event 구독 해제
            driver.driverEvents.OnMoneyChanged.RemoveListener(UpdateMoney);
            driver.driverEvents.OnBatteryChanged.RemoveListener(UpdateBattery);
            driver.driverEvents.OnDeliveryCountChanged.RemoveListener(UpdateDeliveryCount);
            driver.driverEvents.OnMoveStarted.RemoveListener(OnMoveStarted);
            driver.driverEvents.OnMoveStoped.RemoveListener(OnMoveStopped);
            driver.driverEvents.OnLowBattery.RemoveListener(OnLowBattery);
            driver.driverEvents.OnLowBatteryEmpty.RemoveListener(OnBatteryEmpty);
            driver.driverEvents.OnDeliveryCompleted.RemoveListener(OnDeliveryCompleted);
        }
    }
}
