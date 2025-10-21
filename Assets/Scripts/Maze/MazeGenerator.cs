using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public static MazeGenerator Instance;

    [Header("미로 설정")]
    public int width = 10;
    public int height = 10;
    public GameObject cellPrefab;
    public float cellSize = 2f;

    [Header("시각화 설정")]
    public bool visualizeGeneration = false;                        //생성 과정 보기
    public float viaulizationSpeed = 0.05f;                         //속도
    public Color visitedColor = Color.cyan;                         //방문한 칸 색상
    public Color currentColor = Color.yellow;                       //현재 칸 색상
    public Color backtrackColor = Color.magenta;                    //뒤로 가기 색상

    private MazeCell[,] _maze;
    private Stack<MazeCell> _cellStack;                              //DFS를 위한 스택 

    void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        _maze = new MazeCell[width, height];
        _cellStack = new Stack<MazeCell>();

        //모든 셀 생성
        CreateCells();                          

        if (visualizeGeneration)
        {
            StartCoroutine(GenerateWithDFSVisualized());
        }
        else
        {
            GenerateWithDFS();
        }
    }

    //DFS 알고리즘으로 생성
    private void GenerateWithDFS()
    {
        MazeCell current = _maze[0, 0];
        current.visited = true;
        //첫번쨰 현재칸을 스택에 넣는다. 
        _cellStack.Push(current);                    

        while (_cellStack.Count > 0)
        {
            current = _cellStack.Peek();
            //방문하지 않은 이웃 찾기
            List<MazeCell> unvisitedNeighbors = GetUnvisitedNeighbors(current); 

            if (unvisitedNeighbors.Count > 0)
            {
                //랜덤하게 이웃 선택
                MazeCell next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)]; 
                //벽 제거
                RemoveWallBetween(current, next);                       
                next.visited = true;
                _cellStack.Push(next);
            }
            else
            {
                //백 트래킹
                _cellStack.Pop();                    
            }
        }
    }

    //셀 생성 함수 
    private void CreateCells()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("셀 프리팹이 없음");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{z}";

                MazeCell cell = cellObj.GetComponent<MazeCell>();
                if (cell == null)
                {
                    Debug.LogError("MazeCell 스크립트 없음");
                    return;
                }
                cell.Initialize(x, z);
                _maze[x, z] = cell;
            }
        }
    }

    //방문하지 않은 이웃 찾기
    private List<MazeCell> GetUnvisitedNeighbors(MazeCell cell)
    {
        List<MazeCell> neighbors = new List<MazeCell>();

        //상하좌우 체크
        if (cell.x > 0 && !_maze[cell.x - 1, cell.z].visited)
            neighbors.Add(_maze[cell.x - 1, cell.z]);

        if (cell.x < width - 1 && !_maze[cell.x + 1, cell.z].visited)
            neighbors.Add(_maze[cell.x + 1, cell.z]);

        if (cell.z > 0 && !_maze[cell.x, cell.z - 1].visited)
            neighbors.Add(_maze[cell.x, cell.z - 1]);

        if (cell.z < height - 1 && !_maze[cell.x, cell.z + 1].visited)
            neighbors.Add(_maze[cell.x, cell.z + 1]);

        return neighbors;
    }

    //두 셀 사이의 벽 제거
    private void RemoveWallBetween(MazeCell current, MazeCell next)
    {
        if (current.x < next.x)                 //오른쪽
        {
            current.RemoveWall("right");
            next.RemoveWall("left");
        }
        else if (current.x > next.x)            //왼쪽
        {
            current.RemoveWall("left");
            next.RemoveWall("right");
        }
        else if (current.z < next.z)            //위
        {
            current.RemoveWall("top");
            next.RemoveWall("bottom");
        }
        else if (current.z > next.z)            //아래
        {
            current.RemoveWall("bottom");
            next.RemoveWall("top");
        }
    }

    //특정 위치의 셀 가져오기
    public MazeCell GetCell(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
            return _maze[x, z];

        return null;
    }

    //시각화된 DFS 미로 생성 
    private IEnumerator GenerateWithDFSVisualized()                         
    {
        MazeCell current = _maze[0, 0];
        current.visited = true;

        current.SetColor(currentColor);                        
        _cellStack.Clear();

        //첫번쨰 현재칸을 스택에 넣는다. 
        _cellStack.Push(current);

        yield return new WaitForSeconds(viaulizationSpeed);

        int totalCells = width * height;          
        int visitedCount = 1;             

        while (_cellStack.Count > 0)
        {
            current = _cellStack.Peek();
            // 현재 칸 강조
            current.SetColor(currentColor);                 
            yield return new WaitForSeconds(viaulizationSpeed);

            //방문하지 않은 이웃 찾기
            List<MazeCell> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                //랜덤하게 이웃 선택
                MazeCell next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                //벽 제거
                RemoveWallBetween(current, next);

                // 현재 칸 방문 완료 색으로
                current.SetColor(visitedColor);
                next.visited = true;
                visitedCount++;              
                _cellStack.Push(next);

                next.SetColor(currentColor);
                yield return new WaitForSeconds(viaulizationSpeed);   
            }
            else
            {
                current.SetColor(backtrackColor);
                yield return new WaitForSeconds(viaulizationSpeed);

                current.SetColor(visitedColor);

                //백 트래킹
                _cellStack.Pop();

            }

            yield return new WaitForSeconds(viaulizationSpeed);
            ResetAllColors();
            Debug.Log($"미로 생성 완료! 총 ({visitedCount} / {totalCells} 칸)");
        }

        //모든 칸 색상 초기화
        void ResetAllColors()                                            
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    _maze[x, z].SetColor(Color.white);
                }
            }
        }
    }
}
