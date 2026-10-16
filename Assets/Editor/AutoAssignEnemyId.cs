using UnityEngine;
using UnityEditor;
using System.Linq;

public class AutoAssignEnemyId : EditorWindow
{
    [MenuItem("Tools/AutoAssignEnemyId")]
    static void AssignEnemyID()
    {
        // 找到场景上所有MonoBehaviour，过滤拥有 enemyId 字段的对象（就是Enemy脚本）
        Object[] allObjects = Object.FindObjectsOfType<MonoBehaviour>();
        var enemyList = allObjects.Where(x => x.GetType().GetField("enemyId") != null).ToArray();

        int id = 1;
        foreach (var mono in enemyList)
        {
            Undo.RecordObject(mono, "分配EnemyId");
            SerializedObject so = new SerializedObject(mono);
            SerializedProperty prop = so.FindProperty("enemyId");
            prop.intValue = id++;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mono);
        }
        Debug.Log($"✅一共给{enemyList.Length}个敌人分配EnemyID，记得Ctrl+S保存场景！");
    }
}
