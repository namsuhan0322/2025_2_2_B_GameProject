using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDungeon : MonoBehaviour
{
    [Header("���� ����")]
    public int roomCount = 8;                   // ��ü �����ϰ� ���� �� ���� (����/����/����/�Ϲ� ����)
    public int minSize = 4;                     // �� �ּ� ũ�� (Ÿ�� ����, ����/���� ����)
    public int maxSize = 8;                     // �� �ִ� ũ�� (Ÿ�� ����)

    [Header("������ ����")]
    public bool spawnEnemies = true;            // �Ϲ� ��� ���� �濡 ���� ���� ���� ����
    public bool spawnTreasures = true;          // ���� �濡 ������ ���� ���� ����
    public int enemiesPerRoom = 2;              // �Ϲ� �� 1���� ������ ���� ��

    [Header("Dictionary, HashSet")]
    private Dictionary<Vector2Int, Room> _rooms = new Dictionary<Vector2Int, Room>();        // rooms : �� �߽� ��ǥ -> �� ���� ����, �� ��Ÿ������ ����
    private HashSet<Vector2Int> _floors = new HashSet<Vector2Int>();                         // floors : �ٴ� Ÿ�� ��ǥ ����, � ĭ�� �ٴ����� ��ȸ
    private HashSet<Vector2Int> _walls = new HashSet<Vector2Int>();                          // walls : �� Ÿ�� ��ǥ ����, �ٴ� �ֺ��� �ڵ����� ä���.
    
    void Start()
    {
        Generate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Clear();
            Generate();
        }
    }

    public void Generate()
    {
        // �� ���� ���� ��Ģ������ �����.
        CreateRooms();
        // ��� �� ���̸� ������ ���� �Ѵ�.
        ConnectRooms();
        // �ٴ� �ֺ� Ÿ�Ͽ� ���� �ڵ� ��ġ�Ѵ�.
        CreateWalls();
        // ���� Unity �󿡼� Cube�� Ÿ���� �׸���.
        Render();
        // �濡 Ÿ�Կ� ���� ��/������ ��ġ�Ѵ�.
        SpawnObjects();
    }

    // ���۹� 1�� ����, �������� ���� �� ��ó (��/��/��/��)�� �������� �ΰ� �õ�
    // ������ ���� ���� ���������� ����, �Ϲ� �� �Ϻθ� ���������� ��ȯ
    private void CreateRooms()
    {
        // ���� �� : ������ (0,0)�� ��ġ
        Vector2Int pos = Vector2Int.zero;
        int size = Random.Range(minSize, maxSize);
        AddRoom(pos, size, RoomType.Start);                 // �� ���

        // ������ ��� ���� �õ�
        for (int i = 0; i < roomCount; i++)
        {
            var roomList = new List<Room>(_rooms.Values);                   // �̹� ������� �� �� �ϳ��� ��������
            Room baseRoom = roomList[Random.Range(0, roomList.Count)];

            // ���� �濡�� ��/��/��/�� �����Ÿ� �� �� �ĺ�
            Vector2Int[] dirs =
            {
                Vector2Int.up * 6, Vector2Int.down * 6, Vector2Int.left * 6, Vector2Int.right * 6
            };

            foreach (var dir in dirs)
            {
                Vector2Int newPos = baseRoom.centor + dir;              // �� �� �߽� ��ǥ
                int newSize = Random.Range(minSize, maxSize);           // �� �� ũ�� ����
                RoomType type = (i == roomCount - 1) ? RoomType.Boss : RoomType.Normal;
                if (AddRoom(newPos, newSize, type)) break;              // �� ������ ���� �ٴڰ� ��ġ�� ������ �߰� ���� -> ������ �������� ����
            }
        }

        // �Ϲݹ� �� ���� ������ ���������� ��ȯ
        int treasureCount = Mathf.Max(1, roomCount / 4);
        var normalRooms = new List<Room>();

        foreach (var room in _rooms.Values)                         // ���� �� ��� �� �Ϲ� �游 ����
        {
            if (room.type == RoomType.Normal)
                normalRooms.Add(room);
        }

        for (int i = 0; i < treasureCount && normalRooms.Count > 0; i++)     // ������ �Ϲݹ��� ���������� �ٲ۴�
        {
            int idx = Random.Range(0, normalRooms.Count);
            normalRooms[idx].type = RoomType.Treasure;
            normalRooms.RemoveAt(idx);
        }
    }

    // ������ �� �ϳ��� floor Ÿ�Ϸ� �߰�
    // ���� �ٴڰ� ��ġ�� false ��ȯ, ��ġ�� ���� ��� floor Ÿ�Ϸ� ä��� rooms�� �� ��Ÿ�� ���
    private bool AddRoom(Vector2Int center, int size, RoomType type)
    {
        // 1. ��ħ �˻�
        for (int x = -size / 2; x < size / 2; x++)
        {
            for (int y = -size / 2; y < size / 2; y++)
            {
                Vector2Int tile = center + new Vector2Int(x, y);
                if (_floors.Contains(tile)) return false;               // ��ĭ�̶� ��ġ�� ����
            }
        }

        // 2. �� ��Ÿ������ ���
        Room room = new Room(center, size, type);
        _rooms[center] = room;

        // 3. �� ������ floors�� ä���.
        for (int x = -size / 2; x < size / 2; x++)
        {
            for (int y = -size / 2; y < size / 2; y++)
            {
                _floors.Add(center + new Vector2Int(x, y));
            }
        }
        return true;
    }

    // ��� ���� ���� ������ ���� �Ѵ�.
    // ���� ���� �ܼ� List -> MST, A*
    private void ConnectRooms()
    {
        var roomList = new List<Room>(_rooms.Values);

        for (int i = 0; i < roomList.Count - 1; i++)
        {
            CreateCorridor(roomList[i].centor, roomList[i + 1].centor);
        }
    }

    // �� ��ǥ ���̸� x�� -> y�� ������ ���� ������ �Ǵ�.
    // ���� ġ�� L�� ����� ���´�.
    private void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;

        // x�� ���� : start.x -> end.x �� ��ĭ�� �̵��ϸ� �ٴ� Ÿ�� �߰�
        while (current.x != end.x)
        {
            _floors.Add(current);
            current.x += (end.x > current.x) ? 1 : -1;
        }

        // y�� ���� : x�� ������ �� start.y -> end.y �� ��ĭ�� �̵�
        while (current.y != end.y)
        {
            _floors.Add(current);
            current.y += (end.y > current.y) ? 1 : -1;
        }

        _floors.Add(end);           // ������ ������ ĭ�� �ٴ� ó��
    }

    // �ٴ� �ָ��� 8������ ��ĵ�Ͽ�, �ٴ��� �ƴ� ĭ�� walls�� ä���.
    private void CreateWalls()
    {
        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        // ��� �ٴ� Ÿ���� �������� �ֺ� �˻�
        foreach (var floor in _floors)
        {
            foreach (var dir in dirs)
            {
                Vector2Int wallPos = floor + dir;
                if (!_floors.Contains(wallPos))             // �ֺ� ĭ�� �ٴ��� �ƴϸ� "�� ĭ"���� ���
                    _walls.Add(wallPos);
            }
        }
    }

    // Ÿ���� Unity ������Ʈ�� ������
    // �ٴ� : Cube (0,1), �� Cube(1), �� �� ����
    private void Render()
    {
        // �ٴ� Ÿ�� ������
        foreach (var pos in _floors)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0, pos.y);             // y = 0 ��鿡 ��ġ
            cube.transform.localScale = new Vector3(1f, 0.1f, 1f);              // ���� �ٴ�
            cube.transform.SetParent(transform);                                // �θ� ����

            Room room = GetRoom(pos);
            if (room != null)
                cube.GetComponent<Renderer>().material.color = room.GetColor();
            else
                cube.GetComponent<Renderer>().material.color = Color.white;
        }

        // �� Ÿ�� ������
        foreach (var pos in _walls)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0.5f, pos.y);                      
            cube.transform.SetParent(transform);   
            cube.GetComponent<Renderer>().material.color= Color.black;
        }
    }

    // � �ٴ� ��ǥ�� "��� ��"�� ���ϴ��� ������
    private Room GetRoom(Vector2Int pos)
    {
        foreach (var room in _rooms.Values)
        {
            int halfSize = room.size / 2;
            if (Mathf.Abs(pos.x - room.centor.x) < halfSize && Mathf.Abs(pos.y - room.centor.y) < halfSize) return room;
        }

        return null;
    }

    private void SpawnObjects()
    {
        foreach(var room in _rooms.Values)
        {
            switch (room.type)
            {
                case RoomType.Start:
                    // ���۹��� ���� ����
                    break;
                case RoomType.Normal:
                    if (spawnEnemies)
                        SpawnEnemiesInRoom(room);
                    break;
                case RoomType.Treasure:
                    if (spawnTreasures)
                        SpawnTreasureInRoom(room);
                    break;
                case RoomType.Boss:
                    if (spawnEnemies)
                        SpawnBossInRoom(room);
                    break;
            }
        }
    }

    private Vector3 GetRandomPositionInRoom(Room room)
    {
        float halfSize = room.size / 2f - 1f;                                   // -1 �׵θ�
        float randomX = room.centor.x + Random.Range(-halfSize, halfSize);
        float randomZ = room.centor.y + Random.Range(-halfSize, halfSize);

        return new Vector3(randomX, 0.5f, randomZ);
    }

    // �� ����
    private void CreateEnemy(Vector3 position)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enemy.transform.position = position;
        enemy.transform.localScale = Vector3.one * 0.8f;
        enemy.transform.SetParent(transform);
        enemy.name = "Enemy";
        enemy.GetComponent<Renderer>().material.color = Color.red;
    }

    // ���� ����
    private void CreateBoss(Vector3 position)
    {
        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boss.transform.position = position;
        boss.transform.localScale = Vector3.one * 2f;
        boss.transform.SetParent(transform);
        boss.name = "Boss";
        boss.GetComponent<Renderer>().material.color = Color.cyan;
    }

    // ���� ����
    private void CreateTreasure(Vector3 position)
    {
        GameObject treasure = GameObject.CreatePrimitive(PrimitiveType.Cube);
        treasure.transform.position = position;
        treasure.transform.localScale = Vector3.one * 0.6f;
        treasure.transform.SetParent(transform);
        treasure.name = "Treasure";
        treasure.GetComponent<Renderer>().material.color = Color.black;
    }

    // ������ �Լ���
    private void SpawnEnemiesInRoom(Room room)
    {
        for (int i = 0; i < enemiesPerRoom; i++)
        {
            Vector3 spawnPos = GetRandomPositionInRoom(room);
            CreateEnemy(spawnPos);
        }
    }

    // ���� ����
    private void SpawnBossInRoom(Room room)
    {
        Vector3 spawnPos = new Vector3(room.centor.x, 1f, room.centor.y);
        CreateBoss(spawnPos);
    }

    // ���� ����
    private void SpawnTreasureInRoom(Room room)
    {
        Vector3 spawnPos = new Vector3(room.centor.x, 0.5f, room.centor.y);
        CreateTreasure(spawnPos);
    }

    // ���� ������ ��� ����
    private void Clear()
    {
        _rooms.Clear();
        _floors.Clear();
        _walls.Clear();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
