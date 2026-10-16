using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class PopupTypeWriter : MonoBehaviour
{
    // 打开弹窗计数，用来拦截ESC
    //static int PopupOpenCount 静态变量，整个游戏全局共享
    //弹窗打开一次PopupOpenCount++；关闭一次PopupOpenCount--
    //暂停菜单脚本（MenuList）判断：if(PopupTypeWriter.PopupOpenCount>0) → 代表有弹窗正在显示，ESC 按键无效，不能打开暂停菜单，防止弹窗和暂停菜单重叠
    //{get; private set;}：别的脚本只能读取这个数字，不能随便修改，只能在本脚本内部修改
    //get：读取。任何别的脚本都可以读PopupTypeWriter.PopupOpenCount的值
    //private set：赋值只有本类内部可以修改这个变量的值。外面的脚本不能写
    public static int PopupOpenCount { get; private set; }

    [Header("弹窗文本组件")]
    public Text targetText;
    [Header("完整显示的文字内容")]
    public string fullText;
    [Header("每个字间隔时间(秒)")]
    public float charWait = 0.14f;
    [Header("true=直接显示全部文字，false=逐字打字")]
    public bool instantShowAllText = false;

    [Header("【0号按钮】例如：重新开始 / 下一关 / 关闭")]
    public Button btnFirst;
    [Header("【1号按钮】例如：返回关卡选择，可留空不用")]
    public Button btnSecond;

    public Action OnBtnFirstClick;
    public Action OnBtnSecondClick;

    private Coroutine _typingCor;//打字协程的句柄，可以用来停止打字
    private bool _buttonClicked = false;//防止玩家疯狂连续点击按钮，重复执行回调函数。点一次之后标记为 true，再次点击直接拦截
    private bool _typingCancelFlag; //协程取消标记，用来在循环里面提前终止打字循环

    public void ShowPopup()
    {
        _buttonClicked = false;
        _typingCancelFlag = false; //打开弹窗重置取消标记
        PopupOpenCount++;//弹窗计数+1，全局标记现在有弹窗打开
        gameObject.SetActive(true);//把弹窗物体激活，弹窗显示出来
        if (targetText != null)
            targetText.text = "";

        // 清空按钮监听、隐藏按钮
        if (btnFirst != null)
        {
            btnFirst.gameObject.SetActive(false);//先隐藏按钮，文字没打完不能点
            btnFirst.onClick.RemoveAllListeners();//清除旧的按钮事件，防止上次弹窗残留回调
        }
        if (btnSecond != null)
        {
            btnSecond.gameObject.SetActive(false);
            btnSecond.onClick.RemoveAllListeners();
        }

        // 停止旧打字协程：如果上一次打字还没跑完，先停掉，防止多个打字协程同时跑乱字
        if (_typingCor != null)
        {
            StopCoroutine(_typingCor);
            _typingCor = null;
        }

        // 直接全部文字显示
        if (instantShowAllText)
        {
            if (targetText != null)
                targetText.text = fullText;//直接把完整文字一次性放上去
            SetupButtons();//文字准备好了，直接显示按钮
            return;
        }

        // 启动打字协程
        _typingCor = StartCoroutine(TypeCoroutine());
    }

    private IEnumerator TypeCoroutine()
    {
        if (targetText != null)
            targetText.text = "";

        if (string.IsNullOrEmpty(fullText))//如果文本是空字符串
        {
            SetupButtons();
            _typingCor = null;
            yield break;//协程直接结束
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            // 检测取消标记，立刻终止协程
            if (_typingCancelFlag)
            {
                _typingCor = null;
                yield break;
            }

            if (targetText != null)
            {
                // Substring(起始位置, 长度)
                // fullText.Substring(0, i+1)：从第0个字符，截取 i+1 个字符
                // i=0 → 取第1个字；i=1 →取前2个字；i=2 →前3个字，以此类推
                targetText.text = fullText.Substring(0, i + 1);
            }
            // WaitForSecondsRealtime：不受Time.timeScale影响，哪怕游戏暂停 timeScale=0，打字依旧会继续跑
            //WaitForSeconds：受游戏时间缩放Time.timeScale影响，通关/死亡时timeScale=0暂停游戏，等待就卡住不动
            //WaitForSecondsRealtime：按真实世界时间计时，游戏暂停也照样打字，死亡弹窗、通关弹窗刚好需要这个特性
            yield return new WaitForSecondsRealtime(charWait);
        }
        //循环跑完，文字全部打完
        SetupButtons();
        _typingCor = null;
    }

    private void SetupButtons()
    {
        if (btnFirst != null)
        {
            btnFirst.gameObject.SetActive(true);
            btnFirst.onClick.RemoveAllListeners();
            btnFirst.onClick.AddListener(OnFirstButtonClicked);//绑定本脚本内部的点击函数
        }

        if (btnSecond != null)
        {
            btnSecond.gameObject.SetActive(true);
            btnSecond.onClick.RemoveAllListeners();
            btnSecond.onClick.AddListener(OnSecondButtonClicked);
        }
    }

    private void OnFirstButtonClicked()
    {
        if (_buttonClicked) return;//已经点过一次，拦截重复点击
        _buttonClicked = true;
        Action callback = OnBtnFirstClick;//把外部传入的回调函数拿出来存一下
        HidePopup();
        callback?.Invoke();//执行外部传进来的函数（比如重新开始游戏）
    }

    private void OnSecondButtonClicked()
    {
        if (_buttonClicked) return;
        _buttonClicked = true;

        Action callback = OnBtnSecondClick;// 一次性快照，拿到当前引用
        HidePopup();
        callback?.Invoke();// 只用这个快照，就算原变量被置null，callback已经拿到副本
    }

    public void HidePopup()
    {
        _typingCancelFlag = true; //关闭弹窗，立刻标记终止打字协程
        if (PopupOpenCount > 0)
            PopupOpenCount--;
        if (_typingCor != null)
        {
            StopCoroutine(_typingCor);
            _typingCor = null;
        }
        if (targetText != null)
            targetText.text = "";
        if (btnFirst != null)
            btnFirst.onClick.RemoveAllListeners();//清除按钮监听，防止残留
        if (btnSecond != null)
            btnSecond.onClick.RemoveAllListeners();
        gameObject.SetActive(false);//关闭弹窗物体
    }

    private void OnDestroy()
    {
        _typingCancelFlag = true; //物体销毁终止协程，防止幽灵协程
        StopAllCoroutines();

        OnBtnFirstClick = null;
        OnBtnSecondClick = null;

        if (PopupOpenCount > 0)
            PopupOpenCount--;
    }
}
