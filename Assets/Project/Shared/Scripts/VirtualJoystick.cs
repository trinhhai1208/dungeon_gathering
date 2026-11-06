using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonGathering.UI
{
    /// <summary>
    /// Joystick nổi (UI_SYSTEM §6.2): vùng chạm nửa trái màn, tâm nhảy tới điểm chạm, ghi VirtualInput.Move.
    /// Đặt trên RectTransform vùng chạm (trong suốt, raycastTarget = true).
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;   // joy_base
        [SerializeField] RectTransform knob;         // joy_knob
        [SerializeField] float radius = 54f;
        [SerializeField] bool floating = true;
        [SerializeField] bool hideWhenIdle = true;
        [SerializeField, Range(0, 1)] float deadZone = 0.12f;

        RectTransform area;
        Vector3 restPosition;
        int pointerId = -100;

        void Awake()
        {
            area = (RectTransform)transform;
            if (background != null)
            {
                restPosition = background.localPosition;
                if (hideWhenIdle) background.gameObject.SetActive(false);
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != -100) return;
            pointerId = e.pointerId;
            if (background != null)
            {
                if (hideWhenIdle) background.gameObject.SetActive(true);
                if (floating && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)background.parent, e.position, e.pressEventCamera, out var local))
                {
                    background.localPosition = new Vector3(local.x, local.y, 0f);
                }
            }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId || background == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out var local)) return;
            Vector2 v = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = v;
            Vector2 n = v / radius;
            VirtualInput.Move = n.magnitude < deadZone ? Vector2.zero : n;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            Release();
        }

        void Release()
        {
            pointerId = -100;
            VirtualInput.Move = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (background != null)
            {
                if (hideWhenIdle) background.gameObject.SetActive(false);
                else if (floating) background.localPosition = restPosition;
            }
        }

        void OnDisable() => Release();
    }
}
