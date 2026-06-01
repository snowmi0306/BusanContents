using UnityEngine;

public class LimitedParallax : MonoBehaviour
{
    [Header("���� ����")]
    [Tooltip("���� ī�޶� �־��ּ���.")]
    public Transform mainCamera;

    [Tooltip("�÷��̾� ������Ʈ(Rigidbody2D�� �ִ�)�� �־��ּ���.")]
    public Rigidbody2D playerRb;

    [Header("�з����� ����")]
    [Tooltip("����� �и��� ���� (�ٰ��� ũ��, ������ �۰� ����)")]
    public float parallaxFactor = 0.05f;

    [Tooltip("���� �ڸ��� ���ƿ��� �ӵ� (���� Ŭ���� ���� ���ƿ�)")]
    public float smoothSpeed = 5f;

    // ī�޶�� ��� ������ �ʱ� �Ÿ� ����
    private Vector3 offsetFromCamera;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
        }

        // ���� ���� ��, ī�޶�� ���� ����� ������ ����صӴϴ�.
        // (����� �������̹Ƿ� �⺻�����δ� ī�޶� ����ٳ�� ���� ������ �ʽ��ϴ�.)
        offsetFromCamera = transform.position - mainCamera.position;
    }

    void LateUpdate()
    {
        if (playerRb == null || mainCamera == null) return;

        // 1. �⺻ ��ġ: ī�޶� ����ٴϴ� ���� ��ġ
        Vector3 basePosition = mainCamera.position + offsetFromCamera;

        // 2. ���� ������: �÷��̾��� ���� �ӵ�(Velocity)�� ����Ͽ� �ݴ� ����(-)���� �з����� ��
        // �÷��̾ ���߰ų� ���� �����ؼ� �ӵ��� 0�� �Ǹ� �� ���� 0�� �˴ϴ�.
        Vector3 dynamicOffset = new Vector3(-playerRb.linearVelocity.x, -playerRb.linearVelocity.y, 0f) * parallaxFactor;

        // 3. ���� ��ǥ ��ġ
        Vector3 targetPosition = basePosition + dynamicOffset;

        // 4. �ε巴�� �̵� (Lerp�� ����Ͽ� ������ó�� ���� �ڸ��� ������ ���ƿ��� ȿ��)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }
}