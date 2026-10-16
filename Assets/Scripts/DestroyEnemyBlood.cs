using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyEnemyBlood : MonoBehaviour
{
    [SerializeField] private float time = 0.2f;
    void Start()
    {
        //Destroy(要删除的物体, 延迟多少秒之后删除)
        Destroy(gameObject,time);
    }
}
