using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk : MonoBehaviour
{
    [Header("청크 설정")]
    public int chunkSize = 16;
    public int chunkHeight = 64;

    [Header("perline Noise 설정")]
    public float noiseScale = 0.1f;
    public int octaves = 3;
    public float persistence = 0.5f;
    public float lacunaryity = 2.0f;

    [Header("지형 높이")]
    public int groundLevel = 32;
    public int heightVariation = 16;

    [Header("청크 배치 옵션")]
    public bool autoPositionByChunk = true;                 // 청크 좌표로 자동 배치

    // 3D 블록 배열
    private BlockType[, ,] _blocks;
    private Mesh _chunkMesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    [Header("청크 위치")]
    public Vector2Int chunkPosition;

    void Start()
    {
        SetupMesh();

        // 청크 좌표를 월드 위치로 적용
        if (autoPositionByChunk)
        {
            transform.position = new Vector3(chunkPosition.x * chunkSize, 0.0f, chunkPosition.y * chunkSize);
        }

        GenerateChunk();
        BuildMesh();
    }

    private void SetupMesh()
    {
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshCollider = gameObject.AddComponent<MeshCollider>();

        // Vertex Color Shader
        Shader vertexColorShader = Shader.Find("Custom/VertexColor");
        if (vertexColorShader == null)
        {
            Debug.LogError("VertexColor 쉐이더를 찾을 수 없습니다.");
            vertexColorShader = Shader.Find("Unlit/Color");
        }

        _meshRenderer.material = new Material(vertexColorShader);

        _chunkMesh = new Mesh();
        _chunkMesh.name = "VoxelChunk";

        _chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;          // 128넘어가도 잘 동작하게 하기 위해
    }

    // 3D perline noise로 동굴 생성
    private bool IsCaveAt(int x, int y, int z)
    {
        float caveScale = 0.05f;
        float caveThreshold = 0.55f;

        float cave1 = Mathf.PerlinNoise(x * caveScale, z * caveScale);
        float cave2 = Mathf.PerlinNoise(x * caveScale + 100, y * caveScale * 0.5f);
        float cave3 = Mathf.PerlinNoise(y * caveScale * 0.5f, z * caveScale + 200f);

        float caveValue = (cave1 + cave2 + cave3) / 3f;

        return caveValue > caveThreshold;
    }

    // 돌 & 광맥
    private BlockType GetStoneWithOre(int x, int y, int z)
    {
        float oreNoise = Mathf.PerlinNoise(x * 0.1f + 500f, z * 0.1f + 500f);

        // 높이에 따라 괭막 종류 다름
        if (y < 10)
        {
            // 깊은곳
            if (oreNoise > 0.95f) return BlockType.DiamondOre;
        }
        if (y < 20)
        {
            if (oreNoise > 0.92f) return BlockType.GoldOre;
        }
        if (y < 35)
        {
            if (oreNoise > 0.85f) return BlockType.IronOre;
        }

        if (oreNoise > 0.75f) return BlockType.CoalOre;

        return BlockType.Stone;
    }

    // Perlin Noise 지형 높이 계산
    private int GetTerrainHegiht(int worldX, int worldZ)
    {
        float amplitude = 1.0f;                         // 진폭
        float frequency = 1.0f;                         // 주파수
        float noiseHeight = 0f;                         // 누적 시킨 노이즈 높이 값

        // 여러 옥타브 합성
        for (int i = 0; i < octaves; i++)
        {
            float sampleX = worldX * noiseScale * frequency;
            float sampleZ = worldZ * noiseScale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
            noiseHeight += perlinValue * amplitude;                         // 진폭을 적용해서 누적

            amplitude *= persistence;                                       // 다음 옥타브로 갈 수록 진폭 줄인다
            frequency *= persistence;                                       // 주파수를 조정
        }

        // 높이 범위 조정
        int height = groundLevel + Mathf.RoundToInt(noiseHeight * heightVariation);     // 기본 지면 높이 적용
        return Mathf.Clamp(height, 1, chunkHeight - 1);                                 // 높이를 최소 1에서 쵀대 사이로 제한
    }

    public void GenerateChunk()
    {
        _blocks = new BlockType[chunkSize, chunkHeight, chunkSize];

        // 물 높이
        int warterLevel = 28;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // 월드 좌표
                int worldX = chunkPosition.x * chunkSize + x;
                int worldZ = chunkPosition.y * chunkSize + z;

                // Perlin Noise 로 높이 조정
                int height = GetTerrainHegiht(worldX, worldZ);

                for (int y = 0; y < chunkHeight; y++)
                {
                    // 동굴 생성
                    bool isCave = IsCaveAt(worldX, y, worldZ);

                    if (y == 0)
                    {
                        // 맨 아래는 기반암
                        _blocks[x, y, z] = BlockType.Bedrock;
                    }
                    else if (isCave && y > 5 && y < height - 1)
                    {
                        // 동굴은 비움
                        _blocks[x, y, z] = BlockType.Air;
                    }
                    else if (y < height - 4)
                    {
                        // 깊은 곳 돌
                        _blocks[x, y, z] = GetStoneWithOre(worldX, y, worldZ);
                    }
                    else if (y < height - 1)
                    {
                        // 표면 아래는 흙
                        _blocks[x, y, z] = BlockType.Dirt;
                    }
                    else if (y == height -1)
                    {
                        if (height > warterLevel + 1)
                        {
                            _blocks[x, y, z] = BlockType.Grass;
                        }
                        else
                        {
                            _blocks[x, y, z] = BlockType.Sand;
                        }
                    }
                    else if (y < warterLevel)
                    {
                        _blocks[x, y, z] = BlockType.Water;
                    }
                    else
                    {
                        _blocks[x, y, z] = BlockType.Air;
                    }
                }
            }
        }
    }

    // 한 면 추가
    private void AddFace(int x, int y, int z, Vector3 direction, Color color, List<Vector3> vertices, List<int> traingles, List<Color> colors)
    {
        int vertCount = vertices.Count;
        Vector3 pos = new Vector3(x, y, z);

        // 방향에 따라 정점 배치
        if (direction == Vector3.up)
        {
            vertices.Add(pos + new Vector3(0, 1, 0));
            vertices.Add(pos + new Vector3(0, 1, 1));
            vertices.Add(pos + new Vector3(1, 1, 1));
            vertices.Add(pos + new Vector3(1, 1, 0));
        }
        else if (direction == Vector3.down)
        {
            vertices.Add(pos + new Vector3(0, 0, 0));
            vertices.Add(pos + new Vector3(1, 0, 0));
            vertices.Add(pos + new Vector3(1, 0, 1));
            vertices.Add(pos + new Vector3(0, 0, 1));
        }
        else if (direction == Vector3.forward)
        {
            vertices.Add(pos + new Vector3(1, 0, 0));
            vertices.Add(pos + new Vector3(0, 0, 0));
            vertices.Add(pos + new Vector3(0, 1, 0));
            vertices.Add(pos + new Vector3(1, 1, 0));
        }
        else if (direction == Vector3.back)
        {
            vertices.Add(pos + new Vector3(1, 0, 0));
            vertices.Add(pos + new Vector3(0, 0, 0));
            vertices.Add(pos + new Vector3(0, 1, 0));
            vertices.Add(pos + new Vector3(1, 1, 0));
        }
        else if (direction == Vector3.right)
        {
            vertices.Add(pos + new Vector3(1, 0, 0));
            vertices.Add(pos + new Vector3(1, 1, 0));
            vertices.Add(pos + new Vector3(1, 1, 1));
            vertices.Add(pos + new Vector3(1, 0, 1));
        }
        else if (direction == Vector3.left)
        {
            vertices.Add(pos + new Vector3(0, 0, 1));
            vertices.Add(pos + new Vector3(0, 1, 1));
            vertices.Add(pos + new Vector3(0, 1, 0));
            vertices.Add(pos + new Vector3(0, 0, 0));
        }

        // 삼각형
        traingles.Add(vertCount + 0);
        traingles.Add(vertCount + 1);
        traingles.Add(vertCount + 2);
        traingles.Add(vertCount + 0);
        traingles.Add(vertCount + 2);
        traingles.Add(vertCount + 3);

        // 색상
        for (int i = 0; i < 4; i++)
        {
            colors.Add(color);
        }
    }

    // 특정 위치가 투명인지 체크
    private bool IsTransparent(int x, int y, int z)
    {
        if (x < 0 || x >= chunkSize || y < 0 || y >= chunkHeight || z < 0 || z >= chunkSize) return true;

        return _blocks[x, y, z] == BlockType.Air;
    }

    // 블록의 보이는 면만 추가
    private void AddBlockFaces(int x, int y, int z, BlockType block, List<Vector3> vertices, List<int> traingles, List<Color> colors)
    {
        BlockData blockData = new BlockData(block);

        //위
        if (IsTransparent(x, y + 1, z))
        {
            AddFace(x,y,z, Vector3.up, blockData.blockColor, vertices, traingles, colors);
        }
        // 아래
        if (IsTransparent(x, y - 1, z))
        {
            AddFace(x, y, z, Vector3.down, blockData.blockColor, vertices, traingles, colors);
        }
        // 앞
        if (IsTransparent(x, y, z + 1))
        {
            AddFace(x, y, z, Vector3.forward, blockData.blockColor, vertices, traingles, colors);
        }
        // 뒤
        if (IsTransparent(x, y, z - 1))
        {
            AddFace(x, y, z, Vector3.back, blockData.blockColor, vertices, traingles, colors);
        }
        // 오른쪽
        if (IsTransparent(x + 1, y, z))
        {
            AddFace(x, y, z, Vector3.right, blockData.blockColor, vertices, traingles, colors);
        }
        // 왼쪽
        if (IsTransparent(x - 1, y, z))
        {
            AddFace(x, y, z, Vector3.left, blockData.blockColor, vertices, traingles, colors);
        }
    }

    public void BuildMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> traingles = new List<int>();
        List<Color> colors = new List<Color>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    BlockType block = _blocks[x, y, z];
                    if (block == BlockType.Air) continue;

                    //6면 체크
                    AddBlockFaces(x, y, z, block, vertices, traingles, colors);
                }
            }
        }

        _chunkMesh.Clear();
        _chunkMesh.vertices = vertices.ToArray();
        _chunkMesh.triangles = traingles.ToArray();
        _chunkMesh.colors = colors.ToArray();
        _chunkMesh.RecalculateNormals();

        _meshFilter.mesh = _chunkMesh;
        _meshCollider.sharedMesh = _chunkMesh;
    }
}
