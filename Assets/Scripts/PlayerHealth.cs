using System.Collections;
using UnityEngine;
using Cinemachine;

public class PlayerHealth : MonoBehaviour
{
    [Header("血量UI")]
    public HealthPonitBar healthponitbar;
    public float maxHealth = 4;
    private float currentHealth;
    public float currentHP => currentHealth;//只读属性，外部别的脚本可以读取当前血量，但不能直接修改

    [Header("受伤相机震动")]
    public CinemachineVirtualCamera vCam;
    public float shakeIntensity = 0.08f;//震动幅度
    public float shakeDuration = 0.1f;//震动持续时间

    [Header("受伤红色闪烁")]
    public SpriteRenderer playerSprite;//玩家渲染图片组件
    public Color hurtFlashColor = new Color(1.25f, 0.7f, 0.7f, 1f);//受伤闪的红色
    public float flashInterval = 0.07f;//闪烁间隔

    [Header("受伤无敌时长")]
    public float invincibleTime = 0.5f;//受伤之后无敌0.5秒，防止连续被怪物一瞬间扣多滴血

    [Header("拖拽赋值：本物体上的PlayerStatus组件")]
    public PlayerStatus playerStatus;

    private Coroutine flashCoroutine;//保存受伤闪烁协程的引用，方便随时停止
    private Color playerOriginColor;//记录玩家原本的颜色，闪烁结束要恢复
    private bool isInvincible = false;//无敌标记 true=受伤无敌，不会再扣血

    private void Awake()
    {
        currentHealth = maxHealth;

        // 如果没有手动拖 PlayerStatus，就自动获取
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }

        if (healthponitbar != null)
        {
            healthponitbar.setMaxHealth(maxHealth);
        }
        //保存玩家原本的颜色
        if (playerSprite != null)
        {
            playerOriginColor = playerSprite.color;
        }
    }

    private void Start()
    {
        UpdateHealthUI();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // 存档加载专用，只设置血量，不触发受伤特效
    public void SetHealthForLoadSave(float hp)
    {
        //Mathf.Clamp(值，最小值，最大值) 把血量限制 0~maxHealth之间，不会出现负数或者超量血量
        currentHealth = Mathf.Clamp(hp, 0f, maxHealth);
        UpdateHealthUI();//只更新血条，不会播放受伤闪红、震动
    }

    // 外部统一受伤入口
    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;//伤害<=0，直接返回，不扣血
        }

        if (isInvincible)
        {
            return;//处于受伤无敌时间，不吃伤害
        }

        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }

        if (playerStatus == null)
        {
            Debug.LogError("PlayerHealth 找不到 PlayerStatus！");
            return;
        }
        //已经死亡 / 已经通关，不再受伤
        if (playerStatus.isDead || playerStatus.isWin)
        {
            return;
        }

        // 扣血
        currentHealth -= damage;
        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }
        isInvincible = true;
        UpdateHealthUI();

        // 如果上一次受伤的闪烁协程还在跑，先停止，颜色恢复原色
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            if (playerSprite != null)
            {
                playerSprite.color = playerOriginColor;
            }
        }

        StartCoroutine(PlayerHitShake(shakeIntensity,shakeDuration));//相机震动
        flashCoroutine = StartCoroutine(HurtFlashCoroutine(invincibleTime));//角色闪红

        //血量归0 → 调用PlayerStatus.Death()执行死亡逻辑
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            UpdateHealthUI();
            playerStatus.Death();
        }
        else
        {
            //没死，开启无敌倒计时，时间结束取消无敌
            StartCoroutine(InvincibleCooldown());
        }
    }

    private IEnumerator InvincibleCooldown()
    {
        yield return new WaitForSeconds(invincibleTime);
        isInvincible = false;//无敌时间结束，可以再次受伤
    }

    // 红色闪烁
    private IEnumerator HurtFlashCoroutine(float totalTime)
    {
        if (playerSprite == null)
        {
            flashCoroutine = null;
            yield break;
        }

        float timer = 0f;
        // 在整个无敌时间内，反复切换颜色：红→原色→红→原色
        while (timer < totalTime)
        {
            playerSprite.color = hurtFlashColor;
            yield return new WaitForSeconds(flashInterval);
            playerSprite.color = playerOriginColor;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval * 2f;
        }
        playerSprite.color = playerOriginColor;//结束保证回到原色

        flashCoroutine = null;
    }

    // 死亡 / 通关时强制关闭红闪,人物颜色恢复正常
    public void ForceStopFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (playerSprite != null)
        {
            playerSprite.color = playerOriginColor;
        }
        isInvincible = false;
    }

    // 受伤相机震动
    private IEnumerator PlayerHitShake(float maxIntensity,float dur)
    {
        if (vCam == null)
        {
            yield break;//协程直接终止，后面所有代码不再跑，不报错，直接不执行震动
        }

        // Cinemachine想要做屏幕震动，必须给虚拟相机额外挂上组件：CinemachineBasicMultiChannelPerlin，俗称 “噪音组件”
        CinemachineBasicMultiChannelPerlin noise =vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise == null)
        {
            yield break;
        }
        float t = 0f;
        while (t < dur)
        {
            float ratio = t / dur;//0~1，时间进度,代表震动已经完成的百分比
            // 震动强度从最大值慢慢衰减到0，震动慢慢平息，不是突然消失
            //m_AmplitudeGain：噪音振幅，就是震动的摇晃幅度。数值越大画面抖得越狠
            noise.m_AmplitudeGain =maxIntensity * (1f - ratio);
            t += Time.deltaTime;
            //协程在这里暂停，等到下一帧再回来继续执行while 循环。每一帧更新一次震动强度
            yield return null;
        }
        //防止因为浮点误差，循环退出的时候m_AmplitudeGain没有完全等于 0，残留一点点微弱抖动。直接赋值 0 保证相机完全不抖
        noise.m_AmplitudeGain = 0f;//震动结束，彻底关闭震动
    }

    // 更新血条
    private void UpdateHealthUI()
    {
        if (healthponitbar != null)
        {
            healthponitbar.setHealthBar(currentHealth);
        }
    }

    // 撞敌人 / 陷阱
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
        if (playerStatus == null)
        {
            return;
        }
        // 已经死亡、通关、无敌，不受伤
        if (playerStatus.isDead ||playerStatus.isWin ||isInvincible)
        {
            return;
        }
        if (collision.collider.CompareTag(GameConst.Tag_Enemy))
        {
            TakeDamage(1f);
        }
        else if (collision.collider.CompareTag(GameConst.Tag_Trap))
        {
            TakeDamage(0.5f);
        }
    }

    // 掉入死亡线
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
        if (playerStatus == null)
        {
            return;
        }
        if (playerStatus.isDead ||playerStatus.isWin)
        {
            return;
        }
        if (collision.CompareTag(GameConst.Tag_DieLine))
        {
            TakeDamage(maxHealth);//掉入死亡线，直接扣满全部血量，直接死亡
        }
    }
}