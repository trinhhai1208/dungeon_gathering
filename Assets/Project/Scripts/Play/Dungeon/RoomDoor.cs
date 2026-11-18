using DG.Tweening;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Chướng ngại vật chặn lối đi phòng: cụm cột đá Structure.
    /// Khi phòng chiến đấu kích hoạt: các cột đá từ trên trần rơi rầm xuống chặn lối đi (blocker bật).
    /// Khi phòng hoàn thành: các cột đá lún êm xuống mặt đất (blocker tắt).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomDoor : MonoBehaviour
    {
        [SerializeField] BoxCollider2D blocker;
        [SerializeField] Transform[] pillars = new Transform[3];
        [SerializeField] SpriteRenderer[] renderers = new SpriteRenderer[3];
        [SerializeField] float dropHeight = 5f;
        [SerializeField] float dropDuration = 0.22f;
        [SerializeField] float sinkDuration = 0.35f;
        [SerializeField] float sinkDepth = 1.8f;

        public bool IsClosed { get; private set; }

        readonly Vector3[] restPositions = new Vector3[3];
        bool isInitialized;

        void Awake()
        {
            if (blocker == null) blocker = GetComponent<BoxCollider2D>();
            EnsurePillars();
            CacheRestPositions();
            SetClosed(false, true);
        }

        void EnsurePillars()
        {
            if (pillars != null && pillars.Length == 3 && pillars[0] != null)
            {
                if (renderers == null || renderers.Length != 3 || renderers[0] == null)
                {
                    renderers = new SpriteRenderer[3];
                    for (int i = 0; i < 3; i++)
                        if (pillars[i] != null) renderers[i] = pillars[i].GetComponent<SpriteRenderer>();
                }
                return;
            }

            pillars = new Transform[3];
            renderers = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var child = transform.Find($"Pillar_{i}") ?? (transform.childCount > i ? transform.GetChild(i) : null);
                if (child != null)
                {
                    pillars[i] = child;
                    renderers[i] = child.GetComponent<SpriteRenderer>();
                }
            }
        }

        void CacheRestPositions()
        {
            for (int i = 0; i < 3; i++)
            {
                if (pillars != null && i < pillars.Length && pillars[i] != null)
                    restPositions[i] = pillars[i].localPosition;
            }
        }

        /// <summary>
        /// Thiết lập hướng cửa:
        /// North / South (ngang): 3 cột xếp ngang (-1, 0, 1), collider (3, 1).
        /// East / West (dọc): 3 cột xếp dọc (1, 0, -1), collider (1, 3).
        /// Tất cả sprite đều đứng thẳng (upright).
        /// </summary>
        public void SetupOrientation(DoorMask d)
        {
            EnsurePillars();

            bool isVertical = d is DoorMask.East or DoorMask.West;
            // Cửa có thể nằm trên mép ô (N/S: mép dưới hàng tường, E/W: mép trái cột tường).
            // Quy về TÂM ô tường chứa cửa để cột & collider khớp lưới tile (1 ô = 16px).
            Vector2 doorPos = transform.position;
            Vector2 cellCenter = new(Mathf.Floor(doorPos.x + 0.001f) + 0.5f, Mathf.Floor(doorPos.y + 0.001f) + 0.5f);
            Vector2 cellOffset = cellCenter - doorPos;
            if (blocker != null)
            {
                blocker.size = isVertical ? new Vector2(1f, 3f) : new Vector2(3f, 1f);
                blocker.offset = cellOffset;
            }

            for (int i = 0; i < 3; i++)
            {
                if (pillars[i] == null) continue;
                float offset = i - 1; // -1, 0, 1
                // Ô chân cột: N/S trải ngang (x = offset), E/W xếp dọc (i=0 trên, i=2 dưới)
                Vector2 baseCell = cellOffset + (isVertical ? new Vector2(0f, -offset) : new Vector2(offset, 0f));
                // Sprite cột 16×32, pivot giữa → tâm cao hơn tâm ô chân 0.5 để chân cột chạm mép dưới ô
                Vector3 pos = new(baseCell.x, baseCell.y + 0.5f, 0f);
                pillars[i].localPosition = pos;
                pillars[i].localRotation = Quaternion.identity; // luôn đứng thẳng!

                if (renderers != null && i < renderers.Length && renderers[i] != null)
                {
                    renderers[i].sortingLayerName = "Characters";
                    renderers[i].sortingOrder = isVertical ? 10 + i : 10;
                }
            }

            CacheRestPositions();
            isInitialized = true;
            SetClosed(IsClosed, true);
        }

        public void SetClosed(bool closed, bool instant = false)
        {
            if (IsClosed == closed && !instant) return;
            IsClosed = closed;

            if (blocker != null) blocker.enabled = closed;

            EnsurePillars();
            if (!isInitialized) CacheRestPositions();

            for (int i = 0; i < 3; i++)
            {
                var p = pillars != null && i < pillars.Length ? pillars[i] : null;
                if (p == null) continue;
                p.DOKill();
                var sr = renderers != null && i < renderers.Length ? renderers[i] : null;

                Vector3 rest = restPositions[i];

                if (closed)
                {
                    p.gameObject.SetActive(true);
                    if (sr != null)
                    {
                        DOTween.Kill(sr);
                        sr.color = Color.white;
                    }

                    if (instant)
                    {
                        p.localPosition = rest;
                    }
                    else
                    {
                        // Rơi từ trên trần xuống
                        p.localPosition = rest + new Vector3(0f, dropHeight, 0f);
                        float delay = i * 0.05f;
                        int pillarIndex = i;
                        p.DOLocalMoveY(rest.y, dropDuration)
                            .SetLink(p.gameObject)
                            .SetDelay(delay)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                            {
                                p.DOPunchPosition(new Vector3(0f, 0.15f, 0f), 0.18f, 8, 1f).SetLink(p.gameObject);
                                if (pillarIndex == 0) Sfx.Play(SfxId.DoorClose);
                            });
                    }
                }
                else
                {
                    if (instant)
                    {
                        p.localPosition = rest - new Vector3(0f, sinkDepth, 0f);
                        if (sr != null)
                        {
                            DOTween.Kill(sr);
                            var c = sr.color; c.a = 0f; sr.color = c;
                        }
                        p.gameObject.SetActive(false);
                    }
                    else
                    {
                        if (i == 0) Sfx.Play(SfxId.DoorOpen);
                        float delay = i * 0.04f;
                        if (sr != null)
                        {
                            sr.DOKill();
                            // SetTarget để DOTween.Kill(sr) / sr.DOKill() thật sự hủy được tween này; SetLink: tự hủy khi bị Destroy
                            DOTween.To(() => sr.color, c => sr.color = c, new Color(1f, 1f, 1f, 0f), sinkDuration)
                                .SetTarget(sr)
                                .SetLink(sr.gameObject)
                                .SetDelay(delay)
                                .SetEase(Ease.InQuad);
                        }
                        p.DOLocalMoveY(rest.y - sinkDepth, sinkDuration)
                            .SetLink(p.gameObject)
                            .SetDelay(delay)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                            {
                                p.gameObject.SetActive(false);
                            });
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (pillars == null) return;
            for (int i = 0; i < pillars.Length; i++)
            {
                if (pillars[i] != null) pillars[i].DOKill();
                if (renderers != null && i < renderers.Length && renderers[i] != null) DOTween.Kill(renderers[i]);
            }
        }
    }
}
