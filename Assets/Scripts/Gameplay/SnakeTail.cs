using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Train-and-carts style tail that follows the snake head.
    /// Each segment traces the exact path the head took, creating
    /// natural sway on turns.
    /// </summary>
    public class SnakeTail : MonoBehaviour
    {
        [Header("Segments")]
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private int segmentCount = 5;
        [SerializeField] private float segmentSpacing = 0.85f;
        [SerializeField] private float segmentScale = 0.45f;
        [SerializeField] private Color segmentColor = Color.white;
        [Tooltip("Sprite used for body segments (overrides prefab sprite)")]
        [SerializeField] private Sprite bodySprite;
        [Tooltip("Sprite used for the tail tip (last segment)")]
        [SerializeField] private Sprite tailTipSprite;

        [Header("Belly Overlay")]
        [SerializeField] private Sprite bellyFullSprite;

        [Header("Follow Settings")]
        [SerializeField] private float historyStepDistance = 0.05f;
        [SerializeField] private float rotationSmoothing = 10f;

        private List<Transform> _segments = new List<Transform>();
        private List<SpriteRenderer> _overlays = new List<SpriteRenderer>();
        private List<Vector3> _positionHistory = new List<Vector3>();

        private void Start()
        {
            // Seed history trailing behind the head so tail is visible immediately
            Vector3 behind = -transform.up; // opposite of facing direction
            int steps = segmentCount * Mathf.CeilToInt(segmentSpacing / historyStepDistance) + 10;
            for (int i = steps; i >= 0; i--)
            {
                _positionHistory.Add(transform.position + behind * (i * historyStepDistance));
            }

            SpawnSegments();
            PlaceSegments();
        }

        private void LateUpdate()
        {
            RecordHistory();
            PlaceSegments();
        }

        private void SpawnSegments()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                GameObject seg = Instantiate(segmentPrefab, transform.position, Quaternion.identity);
                seg.transform.localScale = Vector3.one * segmentScale;
                var sr = seg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    bool isTip = (i == segmentCount - 1);
                    if (isTip && tailTipSprite != null)
                        sr.sprite = tailTipSprite;
                    else if (bodySprite != null)
                        sr.sprite = bodySprite;
                    sr.color = segmentColor;
                }

                // Create belly overlay child (hidden by default)
                var overlayObj = new GameObject("BellyOverlay");
                overlayObj.transform.SetParent(seg.transform, false);
                overlayObj.transform.localPosition = Vector3.zero;
                overlayObj.transform.localScale = Vector3.one;
                var overlaySr = overlayObj.AddComponent<SpriteRenderer>();
                overlaySr.sprite = bellyFullSprite;
                overlaySr.sortingOrder = sr != null ? sr.sortingOrder + 1 : 1;
                overlaySr.enabled = false;
                _overlays.Add(overlaySr);

                _segments.Add(seg.transform);
            }
        }

        /// <summary>
        /// Called by SnakeController to update how many segments show as full.
        /// Fills front-to-back (segment 0 = closest to head fills first).
        /// </summary>
        public void SetFilledCount(int count)
        {
            for (int i = 0; i < _overlays.Count; i++)
            {
                _overlays[i].enabled = i < count;
            }
        }

        private void RecordHistory()
        {
            if (_positionHistory.Count == 0)
            {
                _positionHistory.Add(transform.position);
                return;
            }

            Vector3 lastRecorded = _positionHistory[0];
            float dist = Vector3.Distance(transform.position, lastRecorded);

            if (dist >= historyStepDistance)
            {
                _positionHistory.Insert(0, transform.position);

                // Keep history buffer reasonable
                int maxHistory = segmentCount * Mathf.CeilToInt(segmentSpacing / historyStepDistance) + 50;
                if (_positionHistory.Count > maxHistory)
                    _positionHistory.RemoveRange(maxHistory, _positionHistory.Count - maxHistory);
            }
        }

        private void PlaceSegments()
        {
            float accumulatedDist = 0f;
            int historyIndex = 0;

            for (int segIdx = 0; segIdx < _segments.Count; segIdx++)
            {
                float targetDist = (segIdx + 1) * segmentSpacing;

                // Walk along history to find the right position
                while (historyIndex < _positionHistory.Count - 1)
                {
                    float stepDist = Vector3.Distance(
                        _positionHistory[historyIndex],
                        _positionHistory[historyIndex + 1]);

                    if (accumulatedDist + stepDist >= targetDist)
                    {
                        float remainder = targetDist - accumulatedDist;
                        float frac = stepDist > 0.001f ? remainder / stepDist : 0f;
                        Vector3 pos = Vector3.Lerp(
                            _positionHistory[historyIndex],
                            _positionHistory[historyIndex + 1],
                            frac);

                        _segments[segIdx].position = pos;

                        // Face the segment toward the one in front
                        Vector3 lookTarget = segIdx == 0
                            ? transform.position
                            : _segments[segIdx - 1].position;
                        Vector3 dir = lookTarget - pos;
                        if (dir.sqrMagnitude > 0.001f)
                        {
                            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                            _segments[segIdx].rotation = Quaternion.Lerp(
                                _segments[segIdx].rotation,
                                targetRot,
                                rotationSmoothing * Time.deltaTime);
                        }
                        break;
                    }

                    accumulatedDist += stepDist;
                    historyIndex++;
                }

                // If we ran out of history, place at last known position
                if (historyIndex >= _positionHistory.Count - 1 && _positionHistory.Count > 0)
                {
                    _segments[segIdx].position = _positionHistory[_positionHistory.Count - 1];
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var seg in _segments)
            {
                if (seg != null)
                    Destroy(seg.gameObject);
            }
        }
    }
}
