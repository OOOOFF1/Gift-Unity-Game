using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireWeapon : MonoBehaviour
{
    public Transform shotPoint;//开火点
    public float startTimeBtwShots;//两次射击之间的冷却时间
    public float offset = -90;//加一个偏移，修正旋转，让枪口对准鼠标
    private float timeBtwShots;//冷却计时器

    private Camera _mainCamera;//存主相机的引用

    void Start()
    {
        Debug.Log("🔥 FireWeapon脚本启动成功");
        _mainCamera = Camera.main;
    }

    void Update()
    {
        // 修复：使用浮点数安全判断，兼容timeScale极小的情况
        //Time.timeScale是时间缩放，暂停游戏的时候 timeScale=0,如果游戏暂停，直接 return，后面代码不跑。枪不会旋转、不能开枪
        //Mathf.Abs取绝对值，防止极小数值带来判断 BUG
        if (Mathf.Abs(Time.timeScale) < 0.001f)
            return;

        if (_mainCamera == null)
        {
            return;
        }

        //把鼠标的屏幕坐标，转成游戏世界坐标，再减去武器的位置，算出从武器指向鼠标的向量
        //简单说：算出鼠标在武器的哪个方向,ScreenToWorldPoint：屏幕鼠标点 → 转换成游戏世界里面的点
        Vector3 difference = _mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        //Atan2：输入一个箭头 (x,y)，算出这个箭头的倾斜角度，但是这个算出来的数字叫弧度，不是我们平时说的 0~360 度，Unity 旋转面板看的是角度，不能直接用
        //Rad2Deg = 弧度转角度（Radian To Degree）把 Atan2 算出来的弧度 → 转成 0~360 的普通角度，保存到 rotZ
        //这一行，算出【武器指向鼠标】需要的 Z 轴角度。 2D 游戏里物体旋转，只需要改 Z 轴，X 和 Y 都是 0
        //Atan 分不清：右上、左下；左上、右下，会丢象限，角度算错。Atan2(y, x)会自动识别 4 个象限，360度全部算对，2D 游戏首选
        float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        //Unity 底层存旋转用的是四元数 Quaternion，我们不能直接给 transform.rotation 写数字
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ + offset);

        if (timeBtwShots > 0)
        {
            //Time.deltaTime = 上一帧到当前帧，过去了多少秒,如果计时器 > 0（还在冷却），每帧减去Time.deltaTime（这一帧消耗的时间），计时器慢慢减少
            timeBtwShots -= Time.deltaTime;
        }
        //Input.GetMouseButtonDown(0)：鼠标左键刚刚按下（按下瞬间触发，按住不会反复触发）
        if (Input.GetMouseButtonDown(0) && timeBtwShots <= 0)
        {
            GameObject bullet = BulletObjectPool.Instance?.GetBullet();
            if (bullet == null) return;

            bullet.transform.position = shotPoint.position;
            bullet.transform.rotation = shotPoint.rotation;
            // 直接计算飞行方向传给子弹
            Vector2 flyDir = shotPoint.up;//枪口指向鼠标的方向
            Projectile proj = bullet.GetComponent<Projectile>();
            if (proj != null)
            {
                //拿到子弹身上的Projectile子弹脚本，调用SetFlyDirection，把飞行方向传给子弹脚本。子弹拿到方向，就会朝着鼠标飞出去
                proj.SetFlyDirection(flyDir);
            }
            timeBtwShots = startTimeBtwShots;//开完枪重置冷却计时器
        }
    }
}