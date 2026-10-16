using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthPonitBar : MonoBehaviour
{
    public Slider sli;

    public void setMaxHealth(float health)
    {
        if (sli == null) return;
        sli.maxValue = health;//让这个滑块从最大的健康值开始
        sli.value = health;//当前血量直接等于最大血量，也就是满血
    }

    //更新当前血量
    public void setHealthBar(float health)
    {
        sli.value = health;
    }
}
