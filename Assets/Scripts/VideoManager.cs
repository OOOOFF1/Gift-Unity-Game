using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    [Header("视频UI")]
    public GameObject videoPanel;//放视频画面的 UI 面板（包含 RawImage 用来渲染视频）
    [Header("VideoPlayer")]
    public VideoPlayer videoPlayer;//Unity 自带视频播放器组件
    [Header("视频暂停按钮")]
    public Button pauseButton;//视频面板上的暂停 / 继续按钮
    [Header("玩家")]
    public Transform player;

    private Vector3 playerInitialPosition;//保存播放视频前玩家的坐标，视频结束后把玩家传送回这个位置
    public bool isVideoPlaying = false;//标记当前视频是否正在播放，对外可读，别的脚本可以读取这个状态
    private bool hasVideoPlayed = false;//标记视频是否已经播放过一次，一旦播放过，就不再重复播放视频（一次性剧情）

    private void Start()
    {
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);//先移除旧的监听，防止多次重复绑定
            pauseButton.onClick.AddListener(TogglePause);
        }
        if (videoPlayer != null)
        {
            //loopPointReached：VideoPlayer 自带事件 —— 视频播放到末尾、播放完成时触发
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        // 订阅对话结束事件
        EventBus.Subscribe<DialogueFinishedEvent>(EventBus.EVENT_DIALOGUE_FINISHED, OnDialogueEndPlayVideo);
    }

    private void OnDestroy()
    {
        //取消订阅
        EventBus.UnSubscribe<DialogueFinishedEvent>(EventBus.EVENT_DIALOGUE_FINISHED, OnDialogueEndPlayVideo);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }
        StopAllCoroutines();
    }

    //事件回调：对话结束触发播放视频 
    void OnDialogueEndPlayVideo(DialogueFinishedEvent evt)
    {
        PlayVideo();
    }

    public void PlayVideo()
    {
        if (hasVideoPlayed)
            return;
        if (videoPanel == null)
        {
            Debug.LogWarning("videoPanel 没有绑定！");
            return;
        }
        if (videoPlayer == null)
        {
            Debug.LogWarning("videoPlayer 没有绑定！");
            return;
        }
        if (player != null)
        {
            //记录玩家当前位置，存进 playerInitialPosition。视频结束之后玩家要回到这个坐标
            playerInitialPosition = player.position;
        }

        videoPanel.SetActive(true);
        videoPlayer.Stop();//先 Stop 停止播放器（防止上次残留视频还在跑）
        videoPlayer.Play();
        isVideoPlaying = true;
        Debug.Log("视频开始播放，videoPanel保持显示");
    }

    //暂停按钮执行的函数
    private void TogglePause()
    {
        if (!isVideoPlaying || videoPlayer == null)
        {
            return;
        }
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();//如果正在播放 → Pause () 暂停
            Debug.Log("视频暂停");
        }
        else
        {
            videoPlayer.Play();//如果已经暂停 → Play () 继续播放
            Debug.Log("视频继续播放");
        }
    }

    public void ExitVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
        if (player != null)
        {
            player.position = playerInitialPosition;
        }
        isVideoPlaying = false;
        hasVideoPlayed = true;
        Debug.Log("手动退出视频");
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
        if (player != null)
        {
            player.position = playerInitialPosition;
        }
        isVideoPlaying = false;
        hasVideoPlayed = true;
        Debug.Log("视频播放结束，关闭videoPanel");
    }
}
