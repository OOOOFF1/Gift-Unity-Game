using UnityEngine;
using UnityEngine.UI;//使用 UI：Text文本、GameObject 对话框

public class Dialogue : MonoBehaviour
{
    public Animator ani;
    public GameObject dialogueBox;
    public Text dialogueText;

    public string[] dialogueLines;
    private int currentLineIndex = 0;//记录现在读到第几句对话
    private bool playerNpc;//玩家是否站在 NPC 对话触发范围内
    private bool isDialogueFinished = false;//所有台词是否全部读完
    private bool hasVideoPlayed = false;//过场视频是否已经播放过一次，防止重复触发视频
    private string playerName;//存玩家输入的名字（StartGame 输入名字，存在 PlayerPrefs）

    void Start()
    {
        ani = GetComponent<Animator>();
        dialogueBox.SetActive(false);
        //从持久化存储PlayerPrefs读取玩家输入的名字；如果玩家没有输入名字，默认值："无名玩家"
        playerName = PlayerPrefs.GetString("PlayerName", "无名玩家");
    }

    void Update()
    {
        //玩家松开 F 按键（KeyUp 松开，不是按下）,防止按住 F 连续疯狂跳对话
        if (playerNpc && Input.GetKeyUp(KeyCode.F))
        {
            if (!isDialogueFinished && !hasVideoPlayed)
            {
                //显示下一句台词
                ShowNextDialogue();
            }
            else if (isDialogueFinished)
            {
                HideDialogue();
            }
        }
    }
    //玩家进入触发器碰撞范围，检测标签是 Player → playerNpc=true，现在允许按 F 对话
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player))
        {
            playerNpc = true;
        }
    }

    //玩家走远离开触发器，playerNpc=false，按 F 不再响应对话
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(GameConst.Tag_Player))
        {
            playerNpc = false;
        }
    }

    //显示下一句对话
    void ShowNextDialogue()
    {
        if (currentLineIndex < dialogueLines.Length)
        {
            //取出数组里面当前这一句原始台词,如果这句台词文字里面包含我是这两个字。功能替换：把台词里 “我是” 后面替换成玩家自己输入的名字
            string rawText = dialogueLines[currentLineIndex];
            if (rawText.Contains("我是"))
            {
                dialogueText.text = $"我是{playerName}";
            }
            else
            {
                dialogueText.text = rawText;
            }
            currentLineIndex++;
        }
        else
        {
            isDialogueFinished = true;
            dialogueText.text = "对话结束，按F键继续";
        }
        dialogueBox.SetActive(true);
        ani.SetBool("act", true);
    }

    void HideDialogue()
    {
        dialogueBox.SetActive(false);
        ani.SetBool("act", false);
        currentLineIndex = 0;
        isDialogueFinished = false;
        hasVideoPlayed = true;

        //DialogueFinishedEvent：事件数据包（类），专门用来装 “对话结束” 这个事件要传递的数据
        //new DialogueFinishedEvent()：创建一个事件数据包对象，变量名叫evt
        DialogueFinishedEvent evt = new DialogueFinishedEvent();
        //在「EVENT_DIALOGUE_FINISHED」这个广播频道，向外广播一条消息：对话已经结束！附带空数据包 evt
        //Publish(事件名字, 数据包) → 广播（发布事件）,Subscribe(事件名字, 回调函数)→ 订阅（收听这个广播）
        EventBus.Publish(EventBus.EVENT_DIALOGUE_FINISHED, evt);
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
