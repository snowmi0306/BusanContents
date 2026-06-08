using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [Tooltip("따라다닐 대상 (메인 카메라를 넣어주세요)")]
    public Transform target;

    private void LateUpdate()
    {
        if (target != null)
        {
            // 이펙트의 위치를 매 프레임마다 카메라의 위치와 똑같이 맞춤
            transform.position = target.position;
        }
    }
}