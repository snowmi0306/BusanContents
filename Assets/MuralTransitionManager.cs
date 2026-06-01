using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 네임스페이스 필수
using System; // Action을 사용하기 위해 필수 추가

public class MuralTransitionManager : MonoBehaviour
{
    [Header("전환 연출 UI")]
    [Tooltip("아까 만든 셰이더가 들어간 RawImage")]
    public RawImage transitionRawImage;

    [Tooltip("RawImage에 들어있는 매테리얼")]
    public Material maskMaterial;

    [Header("카메라 세팅")]
    public Camera mainCamera;
    public Camera muralCamera; // 렌더 텍스처를 굽고 있는 서브 카메라

    [Header("연출 설정")]
    public float transitionDuration = 1.5f; // 원이 퍼져나가는 시간

    void Start()
    {
        // 시작할 때는 전환 UI를 꺼둡니다.
        transitionRawImage.gameObject.SetActive(false);
    }

    // 단디가 벽화 앞에서 상호작용할 때 이 함수를 호출해주세요!
    // 매개변수 muralTransform에는 상호작용하는 '해당 벽화의 Transform'을 넘겨줍니다.
    // Action onComplete를 통해 연출이 다 끝난 시점을 상호작용 스크립트에 알려줍니다.
    public void StartTransition(Transform muralTransform, Action onComplete = null)
    {
        // 1. 벽화의 월드 좌표를 화면(스크린) 픽셀 좌표로 변환
        Vector3 screenPos = mainCamera.WorldToScreenPoint(muralTransform.position);

        // 2. 셰이더는 0~1 사이의 값을 쓰므로 화면 해상도로 나누어 정규화(Normalize) 해줍니다.
        Vector2 normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

        // 3. 셰이더의 _CenterPos(중심점) 변수에 값 전달
        maskMaterial.SetVector("_CenterPos", new Vector4(normalizedPos.x, normalizedPos.y, 0, 0));

        // 4. 원의 크기(_Radius)를 0으로 초기화하고 RawImage 활성화
        maskMaterial.SetFloat("_Radius", 0f);
        transitionRawImage.gameObject.SetActive(true);

        // 5. DOTween을 이용해 _Radius 값을 0에서 1.5(화면을 다 덮는 크기)로 부드럽게 키웁니다.
        // 마리오 원더처럼 끝부분에서 살짝 튕기는 텐션을 주려면 Ease.OutBack을 씁니다.
        maskMaterial.DOFloat(1.5f, "_Radius", transitionDuration).SetEase(Ease.OutBack).OnComplete(() =>
        {
            // ❌ 버그를 일으키는 카메라 마스크 조작 코드를 과감히 삭제합니다!
            // mainCamera.cullingMask = LayerMask.GetMask(...); 

            // ⭕ 연출용 UI와 서브 카메라는 그대로 꺼줍니다.
            transitionRawImage.gameObject.SetActive(false);
            muralCamera.gameObject.SetActive(false);

            // ⭕ 셰이더 연출 완료 콜백 실행 -> 이때 MuralInteractTrigger가 배경과 발판을 안전하게 스왑합니다.
            onComplete?.Invoke();
        });
    }
}