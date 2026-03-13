using UnityEngine;
using Board.Input;

namespace BellyFull
{
    /// <summary>
    /// Interfaces with the Board SDK to track arrow token positions in real time.
    /// Uses a single shared camera for the full-screen shared field.
    /// </summary>
    public class TokenInputManager : MonoBehaviour
    {
        public static TokenInputManager Instance { get; private set; }

        [Header("Player Token Glyph IDs")]
        [Tooltip("Board SDK glyph ID for Player 1's arrow token")]
        [SerializeField] private int player1GlyphId = 0;
        [Tooltip("Board SDK glyph ID for Player 2's arrow token")]
        [SerializeField] private int player2GlyphId = 1;

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("Keyboard Fallback (Editor Only)")]
        [SerializeField] private bool useKeyboardFallback = true;
        [SerializeField] private float keyboardSpeed = 8f;

        // Current world-space positions for each player's snake head
        public Vector2 Player1Position { get; private set; }
        public Vector2 Player2Position { get; private set; }
        public bool Player1Active { get; private set; }
        public bool Player2Active { get; private set; }

        // Keyboard fallback positions (world space)
        private Vector2 _kb1Pos = new Vector2(-3f, 0f);
        private Vector2 _kb2Pos = new Vector2(3f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void Update()
        {
            PollBoardInput();

#if UNITY_EDITOR
            if (useKeyboardFallback)
                PollKeyboardFallback();
#endif
        }

        private void PollBoardInput()
        {
            Player1Active = false;
            Player2Active = false;

            BoardContact[] glyphs = BoardInput.GetActiveContacts(BoardContactType.Glyph);

            foreach (var contact in glyphs)
            {
                if (!contact.phase.IsActive()) continue;

                if (contact.glyphId == player1GlyphId)
                {
                    Player1Position = ScreenToWorldPosition(contact.screenPosition);
                    Player1Active = true;
                }
                else if (contact.glyphId == player2GlyphId)
                {
                    Player2Position = ScreenToWorldPosition(contact.screenPosition);
                    Player2Active = true;
                }
            }
        }

        private Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (mainCamera == null) return Vector2.zero;

            // Board SDK gives screen coords already in Unity-compatible orientation
            Vector2 unityScreenPos = new Vector2(screenPos.x, screenPos.y);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(unityScreenPos.x, unityScreenPos.y, mainCamera.nearClipPlane + 1f));
            return new Vector2(worldPos.x, worldPos.y);
        }

#if UNITY_EDITOR
        private void PollKeyboardFallback()
        {
            // Player 1: WASD
            Vector2 p1Input = Vector2.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) p1Input.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) p1Input.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) p1Input.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) p1Input.x += 1f;

            if (p1Input.sqrMagnitude > 0)
            {
                _kb1Pos += p1Input.normalized * keyboardSpeed * Time.deltaTime;
                _kb1Pos.x = Mathf.Clamp(_kb1Pos.x, -8f, 8f);
                _kb1Pos.y = Mathf.Clamp(_kb1Pos.y, -4.5f, 4.5f);
                Player1Position = _kb1Pos;
                Player1Active = true;
            }

            // Player 2: Arrow keys
            Vector2 p2Input = Vector2.zero;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) p2Input.y += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) p2Input.y -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) p2Input.x -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) p2Input.x += 1f;

            if (p2Input.sqrMagnitude > 0)
            {
                _kb2Pos += p2Input.normalized * keyboardSpeed * Time.deltaTime;
                _kb2Pos.x = Mathf.Clamp(_kb2Pos.x, -8f, 8f);
                _kb2Pos.y = Mathf.Clamp(_kb2Pos.y, -4.5f, 4.5f);
                Player2Position = _kb2Pos;
                Player2Active = true;
            }
        }
#endif

        public Vector2 GetPlayerPosition(PlayerIndex player)
        {
            return player == PlayerIndex.Player1 ? Player1Position : Player2Position;
        }

        public bool IsPlayerActive(PlayerIndex player)
        {
            return player == PlayerIndex.Player1 ? Player1Active : Player2Active;
        }
    }
}
