using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    [Header("포탈 연결")]
    [Tooltip("이동할 반대편 우체통(포탈)의 위치를 넣어주세요.")]
    public Transform destination;

    [Header("사운드")]
    [SerializeField] private string portalEnterSceneName = "Stage3";
    [SerializeField] private string portalEnterSfxName = "sfx_portal_enter";

    [Header("이펙트 설정")]
    [Tooltip("도착 지점에서 터질 이펙트 프리팹을 넣어주세요.")]
    public GameObject teleportEffectPrefab;

    [Tooltip("이펙트가 화면에 머물 시간(초)입니다. 재생이 끝나면 삭제됩니다.")]
    public float effectDestroyTime = 1.5f;

    // 🔥 무한 텔레포트 방지용 전역 쿨타임 (모든 포탈이 이 시간을 공유합니다)
    private static float lastTeleportTime = -1f;
    private static float teleportCooldown = 0.5f; // 0.5초 동안은 다시 포탈을 탈 수 없음

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 쿨타임 체크 (도착하자마자 반대편 포탈을 다시 타버리는 버그 완벽 방지)
            if (Time.time < lastTeleportTime + teleportCooldown)
            {
                return;
            }

            // 2. 쿨타임 갱신
            lastTeleportTime = Time.time;
            PlayPortalEnterSfxIfNeeded();

            // 3. 플레이어 이동 (도착지로 순간이동)
            other.transform.position = destination.position;

            // 4. 도착 위치에 이펙트 생성 (1회 발동)
            if (teleportEffectPrefab != null)
            {
                // 도착지 위치에 이펙트 프리팹 생성
                GameObject effect = Instantiate(teleportEffectPrefab, destination.position, Quaternion.identity);

                // 설정한 시간이 지나면 이펙트 찌꺼기 삭제 (메모리 최적화)
                Destroy(effect, effectDestroyTime);
            }

            Debug.Log("우체통 포탈 이동 및 이펙트 발동 완료!");
        }
    }

    private void PlayPortalEnterSfxIfNeeded()
    {
        if (string.IsNullOrEmpty(portalEnterSfxName))
            return;

        if (!string.IsNullOrEmpty(portalEnterSceneName) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != portalEnterSceneName)
            return;

        AudioManager.PlaySfx(portalEnterSfxName);
    }
}
