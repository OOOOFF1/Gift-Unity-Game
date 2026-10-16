using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MenuList : MonoBehaviour
{
    [Header("暂停菜单面板")]
    public GameObject menuPanel;
    [Header("音量设置")]
    public Slider bgmSlider;
    public Slider videoSlider;
    [SerializeField]
    private AudioSource bgmSound;
    [Header("暂停菜单全屏遮罩BgDimmer")]
    public GameObject bgDimmer;
    [Header("说明书弹窗HelpPopup")]
    public PopupTypeWriter helpPopup;
    [Multiline(8)]//在 Inspector 面板变成多行文本框，方便编辑一大段操作说明文字
    public string helpText =
@"【游戏操作说明】
A/D：左右移动
空格：跳跃，支持二段跳
左Shift：冲刺
鼠标左键：射击
鼠标右键：切换武器显示
F：和NPC对话
ESC：打开暂停菜单";

    private bool isPaused = false;//标记：游戏是否处于暂停状态 true=已暂停
    private VideoManager _videoManager;//视频管理器引用（处理过场视频音量）

    private void Awake()
    {
        _videoManager = FindObjectOfType<VideoManager>();
    }

    // ✅获取玩家，动态Find，不再依赖拖拽
    private PlayerMovement GetPlayer()
    {
        return FindObjectOfType<PlayerMovement>();
    }

    private void Start()
    {
        //读取上次保存的BGM音量，没有存过默认0.8
        //PlayerPrefs：Unity 自带轻量本地持久化存储，存音量这类简单配置，游戏关掉数据还在
        //PlayerPrefs.GetFloat("键名", 默认值)：读取本地保存的数据，如果之前没有存过，返回第二个参数默认值
        float savedBgmVolume = PlayerPrefs.GetFloat("BgmVolume", 0.8f);
        float savedVideoVolume = PlayerPrefs.GetFloat("VideoVolume", 0.8f);

        if (bgmSound != null && bgmSlider != null)
        {
            //把BGM音源音量、滑动条的数值恢复成上次退出游戏保存的值
            bgmSound.volume = savedBgmVolume;//修改真实音源音量
            bgmSlider.value = savedBgmVolume;//把滑块UI位置对齐到这个音量
            //滑动条数值变化事件：拖动滑块，立刻修改BGM音量，并且存到本地
            //vol => { }→ Lambda 表达式（匿名函数）,等价于写一个普通函数
            //vol：就是滑块变化之后的新数值（形参名字，可以随便起，比如v也可以）
            //=>叫 lambda 运算符，读作 “去到”，后面大括号是函数体
            bgmSlider.onValueChanged.AddListener(vol =>
            {
                bgmSound.volume = vol;
                //PlayerPrefs.SetFloat("BgmVolume",vol)把音量写到内存；PlayerPrefs.Save()真正写到电脑硬盘
                PlayerPrefs.SetFloat("BgmVolume", vol);
                PlayerPrefs.Save();
            });
        }

        if (videoSlider != null)
        {
            videoSlider.value = savedVideoVolume;
            videoSlider.onValueChanged.AddListener(vol =>
            {
                if (_videoManager == null)
                {
                    _videoManager = FindObjectOfType<VideoManager>();
                }
                if (_videoManager != null && _videoManager.videoPlayer != null)
                {
                    //拿到视频的音频源，修改视频音量
                    //GetTargetAudioSource(0)：获取视频播放器的第 0 号音频输出音源。视频播放声音是由这个 AudioSource 控制
                    //0=第一条音频轨道（主音轨），游戏视频的声音全部放在第 0 轨道
                    AudioSource videoAudio = _videoManager.videoPlayer.GetTargetAudioSource(0);
                    if (videoAudio != null)
                    {
                        videoAudio.volume = vol;
                    }
                }
                PlayerPrefs.SetFloat("VideoVolume", vol);
                PlayerPrefs.Save();
            });
        }
        //游戏刚启动，暂停面板、遮罩默认隐藏
        if (menuPanel != null) menuPanel.SetActive(false);
        if (bgDimmer != null) bgDimmer.SetActive(false);
        isPaused = false;
        //把操作说明文字赋值给弹窗
        if (helpPopup != null)
        {
            helpPopup.fullText = helpText;
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void Update()
    {
        // PopupTypeWriter.PopupOpenCount是静态计数器，如果已经有别的弹窗（死亡弹窗、通关弹窗打开），直接return，ESC无效，防止冲突
        if (PopupTypeWriter.PopupOpenCount > 0) return;
        //按下ESC按键的瞬间
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) OpenPauseMenu();//没暂停 → 打开暂停菜单
            else ClosePauseMenu();//已经暂停 → 关闭暂停菜单恢复游戏
        }
    }

    private void OpenPauseMenu()
    {
        DebugUtil.Log("===打开暂停菜单===");
        isPaused = true;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (bgDimmer != null) bgDimmer.SetActive(true);
        Time.timeScale = 0;//游戏时间暂停，人物、怪物全部不动
        Cursor.visible = true;//鼠标光标显示出来（游戏中有时候隐藏鼠标）

        if (_videoManager != null && _videoManager.isVideoPlaying)
        {
            if (_videoManager.videoPlayer != null)
            {
                //Pause()：暂停视频播放，视频画面定格在当前帧，视频的声音也停止
                //对应的：.Play()就是继续播放视频
                _videoManager.videoPlayer.Pause();
            }
        }
    }

    public void ClosePauseMenu()
    {
        DebugUtil.Log("===关闭暂停菜单===");
        isPaused = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (bgDimmer != null) bgDimmer.SetActive(false);
        Time.timeScale = 1;
        Cursor.visible = true;

        //如果之前在播放视频，恢复视频播放
        if (_videoManager != null && _videoManager.isVideoPlaying)
        {
            if (_videoManager.videoPlayer != null)
            {
                _videoManager.videoPlayer.Play();
            }
        }
    }

    public void ShowHelpPopup()
    {
        if (helpPopup == null)
        {
            DebugUtil.LogWarning("helpPopup未赋值！");
            return;
        }
        Time.timeScale = 0;//保持游戏暂停
        if (menuPanel != null) menuPanel.SetActive(false);
        if (bgDimmer != null) bgDimmer.SetActive(false);

        //清空旧的按钮绑定，重新绑定关闭回调
        helpPopup.OnBtnFirstClick = null;
        helpPopup.OnBtnSecondClick = null;
        helpPopup.OnBtnFirstClick = () =>
        {
            helpPopup.HidePopup();
            if (menuPanel != null) menuPanel.SetActive(true);
            if (bgDimmer != null) bgDimmer.SetActive(true);
        };
        helpPopup.ShowPopup();
    }

    public void CloseHelpByMask()
    {
        if (helpPopup == null) return;
        helpPopup.HidePopup();
        if (menuPanel != null) menuPanel.SetActive(true);
        if (bgDimmer != null) bgDimmer.SetActive(true);
    }

    //强制关掉暂停 UI，不恢复 Time.timeScale。场景跳转转场之前清理 UI 用
    public void ForceCloseForPopup()
    {
        isPaused = false;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (bgDimmer != null) bgDimmer.SetActive(false);
    }

    public void OnReturn()
    {
        ClosePauseMenu();
    }

    public void OnBackToLevelSelect()
    {
        DebugUtil.Log("===【返回关卡选择】===");
        StartCoroutine(OnBackToLevelSelectCoroutine());
    }

    private IEnumerator OnBackToLevelSelectCoroutine()
    {
        PlayerMovement player = GetPlayer();
        if (player == null)
        {
            DebugUtil.LogError(" 找不到PlayerMovement！无法保存！");
        }
        else
        {
            PlayerStatus ps = player.GetComponent<PlayerStatus>();
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            //只有玩家活着，没有死亡、没有通关，才保存关卡进度
            if (ps != null && !ps.isDead && !ps.isWin)
            {
                SaveGameManager saveManager = SaveGameManager.Instance;
                DebugUtil.Log($"SaveGameManager：{(saveManager != null ? "存在" : "不存在")}");
                if (saveManager != null)
                {
                    //SceneManager.GetActiveScene() → 获取当前正在运行的场景对象
                    //.buildIndex：这个场景在Build Settings列表里面的编号（索引）
                    int curBuildIndex = SceneManager.GetActiveScene().buildIndex;
                    //创建临时存档数据包 LevelPlayerSaveData
                    LevelPlayerSaveData data = new LevelPlayerSaveData();
                    data.playerX = player.transform.position.x;
                    data.playerY = player.transform.position.y;
                    data.currentHP = ph != null ? ph.currentHP : 4;
                    data.appleCount = player.appleCount;
                    data.eatenAppleIds = new List<int>(player.EatenAppleIds);
                    data.killedEnemyIds = new List<int>(player.KilledEnemyIds);
                    //把玩家当前位置、血量、苹果、击杀敌人ID全部保存为本关临时存档
                    saveManager.SaveLevel(curBuildIndex, data);
                    DebugUtil.Log($"【中途保存成功】 BuildIndex={curBuildIndex} X={data.playerX:F2} Y={data.playerY:F2} HP={data.currentHP} Apples={data.appleCount}");
                }
            }
            else
            {
                DebugUtil.Log($" 当前状态：isDead={ps?.isDead} isWin={ps?.isWin} → 不保存");
            }
        }
        //把当前关卡编号存进PlayerPrefs（简单临时记录）
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        PlayerPrefs.SetInt("LastLevelIndex", currentSceneIndex);
        PlayerPrefs.Save();

        CloseAllPopup(); // 优先关闭暂停菜单、遮罩
        Time.timeScale = 1;//恢复游戏时间！！不恢复的话选关场景是暂停状态
        yield return null;//等待一帧，再跳转场景，UI清理完毕
        SceneUtil.Load(GameConst.Scene_Select);

    }

    public void OnExit()
    {
        DebugUtil.Log("===【退出游戏】===");
        StartCoroutine(OnExitCoroutine());
    }

    private IEnumerator OnExitCoroutine()
    {
        PlayerMovement player = GetPlayer();
        if (player != null)
        {
            PlayerStatus ps = player.GetComponent<PlayerStatus>();
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ps != null && !ps.isDead && !ps.isWin)
            {
                SaveGameManager saveManager = SaveGameManager.Instance;
                if (saveManager != null)
                {
                    int curBuildIndex = SceneManager.GetActiveScene().buildIndex;
                    LevelPlayerSaveData data = new LevelPlayerSaveData();
                    data.playerX = player.transform.position.x;
                    data.playerY = player.transform.position.y;
                    data.currentHP = ph != null ? ph.currentHP : 4;
                    data.appleCount = player.appleCount;
                    data.eatenAppleIds = new List<int>(player.EatenAppleIds);
                    data.killedEnemyIds = new List<int>(player.KilledEnemyIds);
                    saveManager.SaveLevel(curBuildIndex, data);
                    DebugUtil.Log($"【退出游戏保存成功】 BuildIndex={curBuildIndex}");
                }
            }
        }
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        //"LastLevelIndex"：存储的 key 名字（字符串键），currentSceneIndex：要存的数字，当前关卡 buildIndex
        PlayerPrefs.SetInt("LastLevelIndex", currentSceneIndex);
        PlayerPrefs.Save();
        yield return null;
        Time.timeScale = 1;
        //关闭整个游戏程序（编辑器模式不会退出，打包exe才生效）
        Application.Quit();
    }

    public void OnRestart()
    {
        DebugUtil.Log("===重新开始关卡，删除本关存档===");
        int curBuildIndex = SceneManager.GetActiveScene().buildIndex;
        SaveGameManager.Instance?.DeleteLevelSave(curBuildIndex);//删掉本关临时存档，重新玩
        ForceCloseForPopup();
        Time.timeScale = 1;
        SceneUtil.Load(curBuildIndex);//重新加载当前关卡
    }

    /// <summary>转场前关闭暂停菜单与弹窗</summary>
    public void CloseAllPopup()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        if (bgDimmer != null)
        {
            bgDimmer.SetActive(false);
        }
        isPaused = false;
        DebugUtil.Log("转场前关闭所有弹窗UI");
    }



#if UNITY_EDITOR
    //删除旧的OnValidate警告，不再需要拖拽player
#endif
}
