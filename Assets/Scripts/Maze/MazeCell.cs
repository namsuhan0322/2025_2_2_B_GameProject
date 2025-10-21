using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject bottomWall;
    public GameObject topWall;
    public GameObject floor;

    public bool visited = false;
    public int x, z;

    //셀 초기화
    public void Initialize(int xPos, int zPos)              
    {
        x = xPos;
        z = zPos;
        visited = false;
        ShowAllWalls();
    }

    //모든 벽 표시
    public void ShowAllWalls()                      
    {
        leftWall.SetActive(true);
        rightWall.SetActive(true);
        bottomWall.SetActive(true);
        topWall.SetActive(true);
        floor.SetActive(true);
    }

    //특정 방향 벽 제거
    public void RemoveWall(string direction)               
    {
        switch (direction)
        {
            case "left":
                leftWall.SetActive(false);
                break;
            case "right":
                rightWall.SetActive(false);
                break;
            case "top":
                topWall.SetActive(false);
                break;
            case "bottom":
                bottomWall.SetActive(false);
                break;
        }
    }

    //셀 색상 변경 (경로 표시용)
    public void SetColor(Color color)               
    {
        floor.GetComponent<Renderer>().material.color = color;
    }
}