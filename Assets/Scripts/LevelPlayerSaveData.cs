using System.Collections.Generic;

//序列化：把这个对象 → 转成文字，存进电脑本地文件（比如 Save.json）
//[System.Serializable]代表这个类可以被序列化，可以存成 JSON / 二进制，做存档
[System.Serializable]
//LevelPlayerSaveData 是关卡内玩家临时存档的数据类，专门用来保存当前这一关里面玩家的临时状态
public class LevelPlayerSaveData
{
    public float playerX;
    public float playerY;

    public float currentHP;

    public int appleCount;

    // 已经吃掉的苹果ID
    public List<int> eatenAppleIds = new List<int>();

    // 已经击杀的敌人ID
    public List<int> killedEnemyIds = new List<int>();
}