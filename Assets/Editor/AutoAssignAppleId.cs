using UnityEngine;
using UnityEditor;//编辑器专用 API,只有编辑器模式能用，打包会报错
using System.Linq;//用来.Where()做筛选过滤

//EditorWindow：Unity 编辑器自定义窗口基类。可以在顶部菜单栏增加按钮，属于编辑器扩展
public class AutoAssignAppleId : EditorWindow
{
    //在 Unity 顶部菜单栏生成菜单：Tools → AutoAssignAppleId，点击这个菜单就会执行下面AssignAppleID()静态函数
    //注意：标记static，菜单点击只能调用静态方法
    [MenuItem("Tools/AutoAssignAppleId")]
    static void AssignAppleID()
    {
        //在当前打开的场景里面，找出所有挂载了 MonoBehaviour 脚本的物体，返回数组allObjects
        Object[] allObjects = Object.FindObjectsOfType<MonoBehaviour>();
        //x代表数组里面每一个 MonoBehaviour 脚本组件；x.GetType()拿到这个脚本的类；
        //.GetField("appleId")：去这个类里面查找名字叫appleId的公开字段；
        //如果找到了appleId字段 → 返回 true，保留这个组件；找不到就过滤丢掉
        //.ToArray()：把筛选结果转成数组appleList，里面全部是场景里的苹果 PickApple 组件
        var appleList = allObjects.Where(x => x.GetType().GetField("appleId") != null).ToArray();

        int id = 1;
        //foreach专门用来遍历数组、List 集合,var = 类型自动推断,var只能局部变量
        foreach (var mono in appleList)
        {
            //注册 Undo 撤销记录（支持 Ctrl+Z 回滚）,告诉 Unity 编辑器：我马上要修改这个组件mono的数据，请把修改前的状态存进撤销栈，支持 Ctrl+Z 撤销
            Undo.RecordObject(mono, "分配AppleId");
            //SerializedObject so：序列化对象包装器
            // Unity 编辑器修改组件 Inspector 上的变量，不建议直接mono.appleId = id赋值；要用SerializedObject / SerializedProperty这套编辑器专用 API
            // 好处：支持 Undo 撤销、支持 Prefab 预制体修改、自动标记脏需要保存
            SerializedObject so = new SerializedObject(mono);
            //FindProperty("字段名字符串")：在这个包装对象里面，根据字段名字，找到对应的 Inspector 属性
            //SerializedProperty 就代表 Inspector 面板上那一个输入框，你可以读写它的值
            SerializedProperty prop = so.FindProperty("appleId");
            //先把当前 id 赋值给 appleId，赋值完 id 再加 1
            prop.intValue = id++;
            //把刚刚对 prop 属性做的修改，真正提交、写入到原始脚本组件上
            so.ApplyModifiedProperties();
            //告诉 Unity这个物体 / 组件数据被改动过了，标记为 “脏”，需要保存到场景文件
            //EditorUtility 是 UnityEditor命名空间下的静态工具类
            EditorUtility.SetDirty(mono);
        }
        Debug.Log($"✅一共给{appleList.Length}个苹果分配AppleID，记得Ctrl+S保存场景！");
    }
}
