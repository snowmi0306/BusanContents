using UnityEngine;
using UnityEngine.UI; // RawImage를 사용하기 위해 UI 네임스페이스 추가

public class Scroller : MonoBehaviour
{
    [SerializeField] private RawImage _img; // 제어할 Raw Image 컴포넌트
    [SerializeField] private float _x, _y;  // X축, Y축 스크롤 속도

    void Update()
    {
        // 1. 매 프레임마다 시간(Time.deltaTime)과 속도를 곱해 스크롤할 위치를 계산합니다.
        Vector2 nextPosition = _img.uvRect.position + new Vector2(_x, _y) * Time.deltaTime;

        // 2. 새로운 위치 값과 기존의 크기(size) 값을 조합해 uvRect를 업데이트합니다.
        _img.uvRect = new Rect(nextPosition, _img.uvRect.size);
    }
}