using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleUIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1f);
    [SerializeField] private Vector3 pressedScale = new Vector3(0.96f, 0.96f, 1f);

    public void OnPointerEnter(PointerEventData eventData) => transform.localScale = hoverScale;
    public void OnPointerExit(PointerEventData eventData) => transform.localScale = normalScale;
    public void OnPointerDown(PointerEventData eventData) => transform.localScale = pressedScale;
    public void OnPointerUp(PointerEventData eventData) => transform.localScale = hoverScale;

    private void OnDisable()
    {
        transform.localScale = normalScale;
    }
}
