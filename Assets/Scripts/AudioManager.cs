using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 单例，全局访问 AudioManager.Instance 调用音效
    public static AudioManager Instance;

    [Header("音效资源拖拽到这里")]
    public AudioClip hitEnemySound;
    public AudioClip playerHurtSound;
    public AudioClip applePickSound;
    public AudioClip footStepSound;
    public AudioClip jumpSound;
    public AudioClip doorSound;

    private AudioSource _audioSource;

    void Awake()
    {
        // 标准DDOL单例：第一次实例赋值，并且不随场景销毁，一般只给全局管理器用
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果已经存在一个AudioManager，把重复物体删掉
            Destroy(gameObject);
            return;
        }
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        // 如果销毁的是全局唯一实例，把静态Instance置空，防止拿到伪空
        if (Instance == this)
        {
            Instance = null;
        }
    }

    //【对外统一播放音效的入口】整个项目播放音效全部调用这个方法
    public void PlaySound(AudioClip clip)
    {
        // 防御判断：音效为空 / AudioSource为空，打印警告直接返回，避免报错
        if (clip == null || _audioSource == null)
        {
            DebugUtil.LogWarning($"AudioManager 音效clip为空");
            return;
        }
        //优先使用音效对象池
        // 对象池里面有多个空闲AudioSource，可以同时播放多个音效（脚步声、跳跃、挨打一起响）
        if (AudioSourcePool.Instance != null)
        {
            //.PlayOneShot(clip)：从对象池拿一个空闲的 AudioSource 音源，播放传入的音效片段 clip
            AudioSourcePool.Instance.PlayOneShot(clip);
            return;
        }
        // 兜底旧逻辑：对象池找不到/被销毁，就用自己身上这一个AudioSource播放
        // _audioSource只有一个！
        // 如果同时触发2个音效，后播放的会打断前一个音效！！
        _audioSource.PlayOneShot(clip);
    }

}
