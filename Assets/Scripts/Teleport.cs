using UnityEngine;
using System.Collections;

public class Teleport : MonoBehaviour
{
    AudioSource door;
    [SerializeField] private AudioClip doorSound;
    //传送冷却时间 0.5 秒，传送后 0.5 秒内不能再次传送
    [SerializeField] private float teleportCooldown = 0.5f;
    private GameObject currentTeleporter;
    private bool _inCooldown;

    void Start()
    {
        door = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Mathf.Abs(Time.timeScale) < 0.001f) return;
        if (Input.GetKeyDown(KeyCode.W) && currentTeleporter != null && !_inCooldown)
        {
            StartCoroutine(DoTeleport());
        }
    }

    private IEnumerator DoTeleport()
    {
        _inCooldown = true;//协程开始，立刻上锁：标记进入冷却，防止连续按 W 重复传送
        bool teleportSuccess = false;//创建标记，记录本次传送是否成功，默认 false（失败）

        if (doorSound != null)
        {
            door.clip = doorSound;
            door.Play();
        }

        Portal portal = currentTeleporter.GetComponent<Portal>();
        if (portal != null && portal.GetDestination() != null)
        {
            transform.position = portal.GetDestination().position;//直接把玩家瞬移到目标坐标
            teleportSuccess = true;//传送成功
        }

        // 传送失败立刻解除冷却，不需要等待
        if (!teleportSuccess)
        {
            _inCooldown = false;
            yield break;
        }

        yield return new WaitForSeconds(teleportCooldown);
        _inCooldown = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConst.Tag_Teleporter))
        {
            currentTeleporter = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConst.Tag_Teleporter))
        {
            if (collision.gameObject == currentTeleporter)
            {
                currentTeleporter = null;
            }
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
