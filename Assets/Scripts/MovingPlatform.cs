using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private GameObject[] points; // 平台移动的目标点
    [SerializeField] private float speed = 2f; // 平台移动速度

    private int pointNum = 0; // 当前目标点的索引
    public bool isMoving = false; // 是否正在移动

    // Update is called once per frame
    void Update()
    {
        //游戏暂停直接返回，平台停止运动
        if (Mathf.Abs(Time.timeScale) < 0.001f) return;

        //新增判空保护，数组为空直接return，避免越界报错
        if (points == null || points.Length == 0)
        {
            return;
        }
        // Vector2.MoveTowards( 当前位置 , 目标位置 , 每帧移动距离 )，让物体一步一步朝着目标点移动，不会瞬移
        //speed * Time.deltaTime：这一帧要移动多少距离，乘 deltaTime 保证不同帧率移动速度一致
        transform.position = Vector2.MoveTowards(transform.position, points[pointNum].transform.position, speed * Time.deltaTime);
        //Vector2.Distance(A,B)计算 A 和 B 两个点之间相隔多少像素距离
        //如果平台距离目标路标小于 0.1 单位 → 判定已经抵达这个点
        if (Vector2.Distance(transform.position, points[pointNum].transform.position) < 0.1f)
        {
            // 到达目标点后，切换到下一个目标点
            pointNum++;
            // 如果到达最后一个目标点，循环回到第一个目标点
            if (pointNum >= points.Length)
            {
                pointNum = 0;
            }
        }
        // 更新移动状态
        isMoving = pointNum != 0 || Vector2.Distance(transform.position, points[pointNum].transform.position) > 0.1f;
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

}