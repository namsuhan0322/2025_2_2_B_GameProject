using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BildBoard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);                            // �ؽ�Ʈ ������ ����
        }
    }
}
