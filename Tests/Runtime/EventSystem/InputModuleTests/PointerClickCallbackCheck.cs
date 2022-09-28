using UnityEngine;
using UnityEngine.EventSystems;
public class PointerClickCallbackCheck : MonoBehaviour, IPointerDownHandler
{
    public bool pointerDown = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
    }
}
