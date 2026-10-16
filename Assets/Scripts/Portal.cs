using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    //写成 public别的脚本可以随便写portal.destination = xxx，外部代码能直接修改目标，容易被意外篡改
    //现在写成private，外部只能通过函数读取，不能直接修改 destination，数据更安全
    [SerializeField] private Transform destination;//destination 里面存的信息：目标的位置、旋转

    public Transform GetDestination()
    {
        return destination;
    }
    //安全校验，防止传送的时候拿空物体直接报错
    public bool HasDestination()
    {
        return destination != null;
    }

}
