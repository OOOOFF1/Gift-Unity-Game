using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartGame : MonoBehaviour
{
    [Header("UI页面物体")]
    public GameObject dialogPage;      // 对话选择页面（Yes/No按钮、displayText1）
    public GameObject inputNamePage;   // 输入名字页面（InputField、confirm按钮、tipText）
    public GameObject startPage;       // 开始游戏页面（startButton）

    [Header("UI组件")]
    public Text displayText1;//对话文字文本
    public Text tipText;//提示文字（输入名字为空时提示）
    public InputField nameInputField;//输入框，玩家填名字
    public Button yesButton;
    public Button noButton;
    public Button confirmButton;
    public Button startButton;

    [Header("打字配置")]
    public string[] startTextWords;//打字机的文字数组，每一段文字
    public float wordShowInterval = 0.5f;//每段文字间隔0.5秒显示
    public float waitBeforeDialog = 1f;//游戏启动后，先等1秒再弹出对话页面

    [Header("淡入淡出设置")]
    public float fadeDuration = 0.5f; // 调长一点，更容易看清淡入淡出
    private int noChoiceCount = 0;
    private bool canSelect = false;//开关：是否允许玩家点击Yes/No，防止文字没打完就点按钮

    void Start()
    {
        // 初始化：所有页面默认关闭
        dialogPage.SetActive(false);
        inputNamePage.SetActive(false);
        startPage.SetActive(false);
        // 一次性绑定按钮监听，只执行一次
        yesButton.onClick.AddListener(ChooseYes);
        noButton.onClick.AddListener(ChooseNo);
        confirmButton.onClick.AddListener(ConfirmName);

        // 初始把Yes No按钮隐藏，等文字打完再显示
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        StartCoroutine(StartFlowCoroutine());//启动开场剧情协程
    }

    IEnumerator StartFlowCoroutine()
    {
        yield return new WaitForSeconds(waitBeforeDialog);//等待1秒
        // 打开对话页面 + 淡入
        yield return StartCoroutine(FadeInPage(dialogPage));
        displayText1.text = "";//清空文本
        // 打字机，循环打印数组里面每一段文字
        foreach (var word in startTextWords)
        {
            displayText1.text += word + " ";//追加文字
            yield return new WaitForSeconds(wordShowInterval);
        }
        //文字全部打印完成之后，再显示Yes No按钮
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
        canSelect = true;//开启选择权限，玩家可以点按钮/按Y、N
    }

    void Update()
    {
        if (!canSelect) return;//不允许选择的时候，直接退出，不检测按键
        if (Input.GetKeyDown(KeyCode.Y)) ChooseYes();//按Y = 选Yes
        if (Input.GetKeyDown(KeyCode.N)) ChooseNo();//按N = 选No
    }

    void ChooseYes()
    {
        if (!canSelect) return;
        canSelect = false;//关闭选择权限，防止重复点击
        //执行页面切换协程：淡出对话页面 → 淡入输入名字页面
        StartCoroutine(SwitchPageWithFade(dialogPage, inputNamePage));
    }


    void ChooseNo()
    {
        noChoiceCount++;
        yesButton.transform.localScale *= 1.2f;
    }

    void ConfirmName()
    {
        string name = nameInputField.text;//拿到输入框里面玩家写的名字
        if (!string.IsNullOrEmpty(name))//判断：字符串不为空
        {
            PlayerPrefs.SetString("PlayerName", name);//保存名字到本地持久化存储
            PlayerPrefs.Save();//立刻写入硬盘
            StartCoroutine(SwitchPageWithFade(inputNamePage, startPage));//切换到开始游戏页面
        }
        else
        {
            tipText.text = "请输入你的名字";
            tipText.gameObject.SetActive(true);//弹出提示文字
        }
    }

    public void LoadNextScene()
    {
        //点击开始按钮，加载当前场景 BuildIndex+1的下一个场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    /// <summary>
    /// 页面淡入：透明度从0→1
    /// </summary>
    IEnumerator FadeInPage(GameObject pageObj)
    {
        pageObj.SetActive(true);//先打开页面
        //Graphic：UI 图形基类，Text、Image、RawImage 都继承 Graphic
        //GetComponentsInChildren<Graphic>()：找到这个页面下所有子物体里的 UI 图像组件。这样可以一次性控制页面全部文字、图片的透明度
        Graphic[] graphics = pageObj.GetComponentsInChildren<Graphic>();
        //全部初始透明
        foreach (var g in graphics)
        {
            //Color 是 C# 的值类型（struct 结构体），不是引用类型
            //值类型：赋值的时候是拷贝一份副本，不是指向同一个对象
            //struct 不能直接改内部成员，必须取出→修改→写回
            Color c = g.color;
            c.a = 0;
            g.color = c;
        }
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            //timer/fadeDuration：算出进度 0~1；Clamp01限制数值在 0~1 之间，防止超出
            //Mathf.Clamp01(数值)作用：把传入的数字强制限制在 0 ~1 之间
            //如果数字 > 1 → 返回1，如果数字 < 0 → 返回0，如果在 0~1 之间 → 返回原来的数字
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            foreach (var g in graphics)
            {
                Color c = g.color;
                c.a = alpha;
                g.color = c;
            }
            yield return null;//yield return null等待下一帧继续循环，直到动画结束
        }
    }

    /// <summary>
    /// 页面淡出：透明度从1→0
    /// </summary>
    IEnumerator FadeOutPage(GameObject pageObj)
    {
        Graphic[] graphics = pageObj.GetComponentsInChildren<Graphic>();
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (timer / fadeDuration));
            foreach (var g in graphics)
            {
                Color c = g.color;
                c.a = alpha;
                g.color = c;
            }
            yield return null;
        }
    }

    /// <summary>
    /// 统一切换页面：旧页面淡出 → 旧页面关闭 → 新页面淡入
    /// </summary> 
    IEnumerator SwitchPageWithFade(GameObject oldPage, GameObject newPage)
    {
        yield return StartCoroutine(FadeOutPage(oldPage));
        oldPage.SetActive(false);
        yield return StartCoroutine(FadeInPage(newPage));
    }
}

