using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("武器物体fire")]
    public GameObject fire;
    void Start()
    {
        // 游戏刚进入，武器默认关闭（隐蔽）
        if (fire != null)
        {
            fire.SetActive(false);
        }
    }

    void Update()
    {
        // 任意弹窗激活时，屏蔽鼠标右键切换武器
        if (PopupTypeWriter.PopupOpenCount > 0) return;

        //加 fire 判空，防止 Inspector 未赋值时右键报空引用
        if (fire == null)
        {
            return;
        }

        // 鼠标右键按下：切换武器显示/隐藏
        if (Input.GetMouseButtonDown(1))
        {
            bool targetState = !fire.activeSelf;
            fire.SetActive(targetState);
            Debug.Log($"武器状态更新：{targetState}");
        }
    }
}