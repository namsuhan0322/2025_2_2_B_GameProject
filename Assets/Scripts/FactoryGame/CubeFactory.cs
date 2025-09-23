using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeFactory : MonoBehaviour
{
    [Header("�����հ� ��ġ")]
    public GameObject cubePrefab;
    public Transform queuePoint;                // ť ������
    public Transform woodStorage;               // ���� â��
    public Transform metalStorage;              // �ݼ� â��
    public Transform assemblyArea;              // ���� ����

    // �ڷ� ������
    private Queue<GameObject> _materialQueue = new Queue<GameObject>();                      // ���� �԰� ť
    private Stack<GameObject> _woodWarehouse = new Stack<GameObject>();                      // ���� â�� ����
    private Stack<GameObject> _metalWarehouse = new Stack<GameObject>();                     // �ݼ� â�� ����
    private Stack<string> _assemblyStack = new Stack<string>();                              // ���� �ھ� ����
    private List<WorkRequest> _requestList = new List<WorkRequest>();                        // ��û�� ����Ʈ
    private Dictionary<ProductType, int> _products = new Dictionary<ProductType, int>();     // ����ǰ ��ųʸ�

    // ���� ����
    public int money = 500;
    public int score = 0;

    private float _lastMaterialTime;
    private float _lastOrderTime;

    void Start()
    {
        _products[ProductType.Chair] = 0;

        _assemblyStack.Push("����");
        _assemblyStack.Push("����");
        _assemblyStack.Push("�غ�");
    }

    void Update()
    {
        HandleInput();
        UpdateVisuals();
        AutoEvent();
    }

    private void AddMaterial()
    {
        // ���� �Ϸ� ����
        ResourceType randomType = (Random.value > 0.5f) ? ResourceType.Wood : ResourceType.Metal;

        GameObject newCube = Instantiate(cubePrefab);
        ResourceCube cubeComponent = newCube.AddComponent<ResourceCube>();
        cubeComponent.Initalize(randomType);

        // ť�� �߰� (�� �ڷ�)
        _materialQueue.Enqueue(newCube);

        Debug.Log($"{randomType} ���� ����, ť ��� : {_materialQueue.Count} ��");
    }

    private void ProcessQueue()
    {
        if (_materialQueue.Count == 0)
        {
            Debug.Log("ť�� ����ֽ��ϴ�.");
            return;
        }

        // ť���� ���Ḧ ������ (���Լ���)
        GameObject cube = _materialQueue.Dequeue();
        ResourceCube resource = cube.GetComponent<ResourceCube>();

        // â�� ���ÿ� �߰� (�� ����)
        if (resource.type == ResourceType.Wood)
        {
            _woodWarehouse.Push(cube);
            Debug.Log($"���� â�� �԰�! â�� : {_woodWarehouse.Count} ��");
        }
        else if (resource.type == ResourceType.Metal)
        {
            _metalWarehouse.Push(cube);
            Debug.Log($"�ݼ� â�� �԰�! â�� : {_metalWarehouse.Count} ��");
        }
    }

    private void ProcessAssembly()
    {
        if (_woodWarehouse.Count == 0 || _metalWarehouse.Count == 0)            // ��� Ȯ��
        {
            Debug.Log("������ ��ᰡ �����մϴ�.!");
            return;
        }

        if (_assemblyStack.Count == 0)
        {
            Debug.Log("���� �۾��� �����ϴ�.!");
            return;
        }

        // ���ÿ��� �۾��� ������ (���Լ���)
        string work = _assemblyStack.Pop();

        // ��� �Ҹ�
        GameObject wood = _woodWarehouse.Pop();
        GameObject metal = _metalWarehouse.Pop();
        Destroy(wood);
        Destroy(metal);

        // ��� �۾� �Ϸ�� ��ǰ ����
        if (_assemblyStack.Count == 0)
        {
            _products[ProductType.Chair]++;
            score += 100;

            // ���� �ٽ� ä���
            _assemblyStack.Push("����");
            _assemblyStack.Push("����");
            _assemblyStack.Push("�غ�");

            Debug.Log($"���� �ϼ�! �� ���� : {_products[ProductType.Chair]} ��");
        }
    }

    private void AddRequest()
    {
        int quantity = Random.Range(1, 4);
        int reward = quantity * 200;

        WorkRequest newRequest = new WorkRequest(ProductType.Chair, quantity, reward);

        _requestList.Add(newRequest);

        Debug.Log("�� ��û�� ����!");
    }

    private void ProcessRequests()
    {
        if (_requestList.Count == 0)
        {
            Debug.Log("ó���� ��û���� �����ϴ�.");
            return;
        }

        // ù ��° ��û�� ó�� (����Ʈ �������)
        WorkRequest firestRequest = _requestList[0];

        if (_products[firestRequest.productType] >= firestRequest.quantity)
        {
            _products[firestRequest.productType] -= firestRequest.quantity;
            money += firestRequest.reward;
            score += firestRequest.reward;

            _requestList.RemoveAt(0);         // ����Ʈ ù ��° ����
        }
        else
        {
            int available = _products[firestRequest.productType];
            int needed = firestRequest.quantity - available;
            Debug.Log($"��� ���� ! {needed} �� �� �ʿ� (���� : {available} ��)");
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
            // ������ �Ʒ����� ���� ����
            Vector3 position = basePoint.position + Vector3.up * (i * 1.1f);
            stackArray[stackArray.Length - 1 - i].transform.position = position;
        }
    }

    private void OnGUI()
    {
        // ���� ����
        GUI.Label(new Rect(10, 10, 200, 20), $"�� : {money}�� || ���� : {score}��");

        // �ڷᱸ�� ��Ȳ
        GUI.Label(new Rect(10, 40, 250, 20), $"���� ť(Queue) : {_materialQueue.Count}�� ���");
        GUI.Label(new Rect(10, 60, 250, 20), $"���� â��(Stack) : {_woodWarehouse.Count}��");
        GUI.Label(new Rect(10, 80, 250, 20), $"�ݼ� â��(Stack) : {_metalWarehouse.Count}��");
        GUI.Label(new Rect(10, 100, 250, 20), $"���� ����(Stack) : {_assemblyStack.Count}�� �۾�");
        GUI.Label(new Rect(10, 120, 250, 20), $"����ǰ(Dic) : {_products[ProductType.Chair]}��");
        GUI.Label(new Rect(10, 140, 250, 20), $"��û��(List) : {_requestList.Count}��");

        // ��û�� ���
        GUI.Label(new Rect(10, 170, 200, 20), "==== ��û�� ��� ====");
        for (int i = 0; i < _requestList.Count && i < 3; i++)
        {
            WorkRequest request = _requestList[i];
            GUI.Label(new Rect(10, 190 + i * 20, 300, 20),
                $"[{i} ���� {request.quantity} �� -> {request.reward} ��]");
        }

        // ���۹�
        GUI.Label(new Rect(300, 40, 150, 20), "==== ���۹� ====");
        GUI.Label(new Rect(300, 60, 150, 20), "1Ű : ���� ť �߰�");
        GUI.Label(new Rect(300, 80, 150, 20), "QŰ : ť -> â��");
        GUI.Label(new Rect(300, 100, 150, 20), "AŰ : ���� (����)");
        GUI.Label(new Rect(300, 120, 150, 20), "SŰ : ��û ó��");
        GUI.Label(new Rect(300, 140, 150, 20), "RŰ : ��û�� �߰�");
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
        // 3�ʸ��� �ڵ� ���� �߰�
        if (Time.time - _lastMaterialTime > 3f)
        {
            AddMaterial();
            _lastMaterialTime = Time.time;
        }

        // 10�ʸ��� ��û�� �߰�
        if (Time.time - _lastOrderTime > 10f)
        {
            AddRequest();
            _lastOrderTime = Time.time;
        }
    }
}
