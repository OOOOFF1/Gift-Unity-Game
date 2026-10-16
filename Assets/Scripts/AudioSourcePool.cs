using UnityEngine;
using System.Collections.Generic;//提供Queue、HashSet、Dictionary容器
using System.Collections;//用于协程IEnumerator
using System.Linq;//用来.OrderBy()做排序，抢占的时候找出播放时间最早的音源

public class AudioSourcePool : MonoBehaviour
{
    public static AudioSourcePool Instance;
    [Tooltip("音效池最大同时播放数量")]
    public int poolSize = 8;

    //Queue<AudioSource>队列，存放空闲、没在干活的喇叭
    //先进先出，拿喇叭从头部 Dequeue，归还喇叭往尾部 Enqueue
    private Queue<AudioSource> _idlePool;
    //存放正在播放音效、正在忙碌的喇叭
    //查找Contains()速度极快，用来快速判断某个喇叭是不是正在工作
    //Add(obj)：放进去；Remove(obj)：拿出来；Contains(obj)：判断这个东西是否在盒子里面
    private HashSet<AudioSource> _busySet;
    //Dictionary<key,value>字典，key：AudioSource 喇叭，value：float Time.time，这个喇叭是什么时间点开始播放音效的
    //记录音源开始播放时间，用来抢占最早播放的音效
    //dict[key]=value：写入；dict[key]：根据 key 取出对应 value；ContainsKey(key)：判断字典里有没有这把钥匙
    private Dictionary<AudioSource, float> _playStartTimeDict;

    //每个音源的使用序号，防止旧协程误归还被抢占的音源
    private Dictionary<AudioSource, int> _usageTokenDict;
    private int _nextToken = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _idlePool = new Queue<AudioSource>();
        _busySet = new HashSet<AudioSource>();
        _playStartTimeDict = new Dictionary<AudioSource, float>();
        _usageTokenDict = new Dictionary<AudioSource, int>();

        for (int i = 0; i < poolSize; i++)
        {
            //动态新建物体，名字叫AudioSource_0、AudioSource_1...
            GameObject go = new GameObject($"AudioSource_{i}");
            //把新建的喇叭物体，设置成当前 AudioSourcePool 物体的子物体
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            //物体生成的时候不要自动播放声音
            //playOnAwake是 AudioSource 组件的勾选框
            src.playOnAwake = false;
            _idlePool.Enqueue(src);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public AudioSource GetIdleSource()
    {
        if (_idlePool.Count > 0)
        {
            //Dequeue()：从队列头部取出一个空闲喇叭，从空闲池移除
            AudioSource s = _idlePool.Dequeue();
            _busySet.Add(s);
            _playStartTimeDict[s] = Time.time;
            _usageTokenDict[s] = ++_nextToken;
            return s;
        }

        // 池子满：抢占最早开始播放的音源
        if (_busySet.Count > 0)
        {
            //.First()取出排在第一位的喇叭
            AudioSource steal = _busySet.OrderBy(kv => _playStartTimeDict[kv]).First();

            steal.Stop();
            _busySet.Remove(steal);
            _playStartTimeDict.Remove(steal);
            _busySet.Add(steal);
            _playStartTimeDict[steal] = Time.time;
            _usageTokenDict[steal] = ++_nextToken;
            return steal;
        }
        //没有空闲喇叭，也没有任何忙碌喇叭（池子 size=0 异常情况），返回 null，上层 PlayOneShot 检测 null，就不播放音效
        return null;
    }

    //用完喇叭，归还回空闲池
    public void ReturnSource(AudioSource src)
    {
        if (src == null) return;
        //Contains(obj)：判断这个东西是否在盒子里面
        if (!_busySet.Contains(src)) return;

        src.Stop();
        src.clip = null;//清空 AudioClip 音频资源
        //音量恢复默认 1，下一次用的时候不受上一次音量影响
        src.volume = 1f;

        _busySet.Remove(src);
        _playStartTimeDict.Remove(src);
        _usageTokenDict.Remove(src);
        _idlePool.Enqueue(src);
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource src = GetIdleSource();
        if (src != null)
        {
            src.clip = clip;
            src.volume = volume;
            src.Play();
           
            int token = _usageTokenDict.ContainsKey(src) ? _usageTokenDict[src] : -1;
            //启动协程！把喇叭 src和本次的 token 小票一起传给协程
            StartCoroutine(WaitAudioFinish(src, token));
        }
    }

    private IEnumerator WaitAudioFinish(AudioSource src, int token)
    {
        if (src == null || src.clip == null)
        {
            ReturnSource(src);
            yield break;//立刻终止协程
        }
        if (src.clip.length <= 0)
        {
            ReturnSource(src);
            yield break;
        }
        //协程在这里等待等于音效完整时长的秒数
        yield return new WaitForSeconds(src.clip.length);

        //TryGetValue(键, out 变量)字典专用方法,TryGetValue：尝试去字典里面，根据src（喇叭）找对应的 token 数字
        //找到：返回 true，并且把数字塞到currentToken变量里面；找不到：返回 false，currentToken 给默认 0
        if (!_usageTokenDict.TryGetValue(src, out int currentToken) || currentToken != token)
        {
            yield break;
        }

        ReturnSource(src);
    }
}