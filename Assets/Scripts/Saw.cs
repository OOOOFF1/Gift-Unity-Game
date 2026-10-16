using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Saw : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    void Update()
    {
        //如果游戏暂停，直接 return，不执行下面旋转代码 → 电锯停止转动
        if (Mathf.Abs(Time.timeScale) < 0.001f) return;
        //Time.unscaledDeltaTime：不受 timeScale 影响的帧时间
        transform.Rotate(0, 0, 360 * speed * Time.unscaledDeltaTime);
    }
}
