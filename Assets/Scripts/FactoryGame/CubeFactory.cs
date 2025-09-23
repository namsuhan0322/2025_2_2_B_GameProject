using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeFactory : MonoBehaviour
{
    [Header("프리팹과 위치")]
    public GameObject cubePrefab;
    public Transform queuePoint;                // 큐 시작점
    public Transform woodStorage;               // 나무 창고
    public Transform metalStorage;              // 금속 창고
    public Transform assemblyArea;              // 조립 구역

    // 자료 구조들
    private Queue<GameObject> _materialQueue = new Queue<GameObject>();                      // 원료 입고 큐
    private Stack<GameObject> _woodWarehouse = new Stack<GameObject>();                      // 나무 창고 스택
    private Stack<GameObject> _metalWarehouse = new Stack<GameObject>();                     // 금속 창고 스택
    private Stack<string> _assemblyStack = new Stack<string>();                              // 조립 자업 스택
    private List<WorkRequest> _requestList = new List<WorkRequest>();                        // 요청서 리스트
    private Dictionary<ProductType, int> _products = new Dictionary<ProductType, int>();     // 완제품 딕셔너리

    // 게임 상태
    public int money = 500;
    public int score = 0;

    private float _lastMaterialTime;
    private float _lastOrderTime;

    void Start()
    {
        _products[ProductType.Chair] = 0;

        _assemblyStack.Push("포장");
        _assemblyStack.Push("조립");
        _assemblyStack.Push("준비");
    }

    void Update()
    {
        HandleInput();
        UpdateVisuals();
        AutoEvent();
    }

    private void AddMaterial()
    {
        // 랜덤 완료 생성
        ResourceType randomType = (Random.value > 0.5f) ? ResourceType.Wood : ResourceType.Metal;

        GameObject newCube = Instantiate(cubePrefab);
        ResourceCube cubeComponent = newCube.AddComponent<ResourceCube>();
        cubeComponent.Initalize(randomType);

        // 큐에 추가 (맨 뒤로)
        _materialQueue.Enqueue(newCube);

        Debug.Log($"{randomType} 원료 도착, 큐 대기 : {_materialQueue.Count} 개");
    }

    private void ProcessQueue()
    {
        if (_materialQueue.Count == 0)
        {
            Debug.Log("큐가 비어있습니다.");
            return;
        }

        // 큐에서 원료를 꺼내기 (선입선출)
        GameObject cube = _materialQueue.Dequeue();
        ResourceCube resource = cube.GetComponent<ResourceCube>();

        // 창고 스택에 추가 (맨 위에)
        if (resource.type == ResourceType.Wood)
        {
            _woodWarehouse.Push(cube);
            Debug.Log($"나무 창고 입고! 창고 : {_woodWarehouse.Count} 개");
        }
        else if (resource.type == ResourceType.Metal)
        {
            _metalWarehouse.Push(cube);
            Debug.Log($"금속 창고 입고! 창고 : {_metalWarehouse.Count} 개");
        }
    }

    private void ProcessAssembly()
    {
        if (_woodWarehouse.Count == 0 || _metalWarehouse.Count == 0)            // 재료 확인
        {
            Debug.Log("조립할 재료가 부족합니다.!");
            return;
        }

        if (_assemblyStack.Count == 0)
        {
            Debug.Log("조립 작업이 없습니다.!");
            return;
        }

        // 스택에서 작업을 꺼내기 (후입선출)
        string work = _assemblyStack.Pop();

        // 재료 소모
        GameObject wood = _woodWarehouse.Pop();
        GameObject metal = _metalWarehouse.Pop();
        Destroy(wood);
        Destroy(metal);

        // 모든 작업 완료시 제품 생산
        if (_assemblyStack.Count == 0)
        {
            _products[ProductType.Chair]++;
            score += 100;

            // 스택 다시 채우기
            _assemblyStack.Push("포장");
            _assemblyStack.Push("조립");
            _assemblyStack.Push("준비");

            Debug.Log($"의자 완성! 총 의자 : {_products[ProductType.Chair]} 개");
        }
    }

    private void AddRequest()
    {
        int quantity = Random.Range(1, 4);
        int reward = quantity * 200;

        WorkRequest newRequest = new WorkRequest(ProductType.Chair, quantity, reward);

        _requestList.Add(newRequest);

        Debug.Log("새 요청서 도착!");
    }

    private void ProcessRequests()
    {
        if (_requestList.Count == 0)
        {
            Debug.Log("처리할 요청서가 없습니다.");
            return;
        }

        // 첫 번째 요청서 처리 (리스트 순서대로)
        WorkRequest firestRequest = _requestList[0];

        if (_products[firestRequest.productType] >= firestRequest.quantity)
        {
            _products[firestRequest.productType] -= firestRequest.quantity;
            money += firestRequest.reward;
            score += firestRequest.reward;

            _requestList.RemoveAt(0);         // 리스트 첫 번째 제거
        }
        else
        {
            int available = _products[firestRequest.productType];
            int needed = firestRequest.quantity - available;
            Debug.Log($"재고 부족 ! {needed} 개 더 필요 (현재 : {available} 개)");
        }
    }

    private void UpdateVisuals()
    {
        UpdateQueueVisual();
        UpdateWarehouseVisual();
    }

    private void UpdateQueueVisual()
    {
        if (queuePoint == null) return;

        GameObject[] queueArray = _materialQueue.ToArray();
        for ( int i = 0; i < queueArray.Length; i++ )
        {
            Vector3 position = queuePoint.position + Vector3.right * (i * 1.2f);
            queueArray[i].transform.position = position;
        }
    }

    private void UpdateWarehouseVisual()
    {
        UpdateStackVisual(_woodWarehouse.ToArray(), woodStorage);
        UpdateStackVisual(_metalWarehouse.ToArray(), metalStorage);
    }

    private void UpdateStackVisual(GameObject[] stackArray, Transform basePoint)
    {
        if (basePoint == null) return;

        for (int i = 0; i < stackArray.Length; i++)
        {
            // 스택은 아래에서 위로 쌓임
            Vector3 position = basePoint.position + Vector3.up * (i * 1.1f);
            stackArray[stackArray.Length - 1 - i].transform.position = position;
        }
    }

    private void OnGUI()
    {
        // 게임 상태
        GUI.Label(new Rect(10, 10, 200, 20), $"돈 : {money}원 || 점수 : {score}점");

        // 자료구조 현황
        GUI.Label(new Rect(10, 40, 250, 20), $"원료 큐(Queue) : {_materialQueue.Count}개 대기");
        GUI.Label(new Rect(10, 60, 250, 20), $"나무 창고(Stack) : {_woodWarehouse.Count}개");
        GUI.Label(new Rect(10, 80, 250, 20), $"금속 창고(Stack) : {_metalWarehouse.Count}개");
        GUI.Label(new Rect(10, 100, 250, 20), $"조립 스택(Stack) : {_assemblyStack.Count}개 작업");
        GUI.Label(new Rect(10, 120, 250, 20), $"완제품(Dic) : {_products[ProductType.Chair]}개");
        GUI.Label(new Rect(10, 140, 250, 20), $"요청서(List) : {_requestList.Count}개");

        // 요청서 목록
        GUI.Label(new Rect(10, 170, 200, 20), "==== 요청서 목록 ====");
        for (int i = 0; i < _requestList.Count && i < 3; i++)
        {
            WorkRequest request = _requestList[i];
            GUI.Label(new Rect(10, 190 + i * 20, 300, 20),
                $"[{i} 의자 {request.quantity} 개 -> {request.reward} 원]");
        }

        // 조작법
        GUI.Label(new Rect(300, 40, 150, 20), "==== 조작법 ====");
        GUI.Label(new Rect(300, 60, 150, 20), "1키 : 원료 큐 추가");
        GUI.Label(new Rect(300, 80, 150, 20), "Q키 : 큐 -> 창고");
        GUI.Label(new Rect(300, 100, 150, 20), "A키 : 조립 (스택)");
        GUI.Label(new Rect(300, 120, 150, 20), "S키 : 요청 처리");
        GUI.Label(new Rect(300, 140, 150, 20), "R키 : 요청서 추가");
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) AddMaterial();
        if (Input.GetKeyDown(KeyCode.Q)) ProcessQueue();
        if (Input.GetKeyDown(KeyCode.A)) ProcessAssembly();
        if (Input.GetKeyDown(KeyCode.S)) ProcessRequests();
        if (Input.GetKeyDown(KeyCode.R)) AddRequest();
    }

    private void AutoEvent()
    {
        // 3초마다 자동 원료 추가
        if (Time.time - _lastMaterialTime > 3f)
        {
            AddMaterial();
            _lastMaterialTime = Time.time;
        }

        // 10초마다 요청서 추가
        if (Time.time - _lastOrderTime > 10f)
        {
            AddRequest();
            _lastOrderTime = Time.time;
        }
    }
}
