using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Captures all interaction events with timestamps, writes to CSV in real time,
    /// and handles player progress persistence to JSON.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableLogging = true;

        private string _csvPath;
        private string _progressPath;
        private string _sessionId;
        private StreamWriter _csvWriter;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _csvPath = Path.Combine(Application.persistentDataPath, "belly_full_log.csv");
            _progressPath = Path.Combine(Application.persistentDataPath, "player_progress.json");
        }

        private void Start()
        {
            if (!enableLogging) return;

            InitCSV();
            SubscribeEvents();
            Debug.Log($"[DataLogger] Logging to {_csvPath}");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            _csvWriter?.Flush();
            _csvWriter?.Close();
        }

        private void InitCSV()
        {
            bool fileExists = File.Exists(_csvPath);
            _csvWriter = new StreamWriter(_csvPath, append: true, encoding: Encoding.UTF8);

            if (!fileExists)
            {
                _csvWriter.WriteLine("session_id,player_id,timestamp,event_type,data1,data2,data3,data4");
            }
        }

        private void SubscribeEvents()
        {
            GameEvents.OnObjectEaten += LogObjectEaten;
            GameEvents.OnEquationGenerated += LogEquationGenerated;
            GameEvents.OnEquationSolved += LogEquationSolved;
            GameEvents.OnBellyAcheStarted += LogBellyAche;
            GameEvents.OnDodgeAttempt += LogDodgeAttempt;
            GameEvents.OnBallBlastEnded += LogBallBlast;
            GameEvents.OnCrownAwarded += LogCrown;
            GameEvents.OnEnergyBarFull += LogBarFull;
            GameEvents.OnGameWon += LogGameWon;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnObjectEaten -= LogObjectEaten;
            GameEvents.OnEquationGenerated -= LogEquationGenerated;
            GameEvents.OnEquationSolved -= LogEquationSolved;
            GameEvents.OnBellyAcheStarted -= LogBellyAche;
            GameEvents.OnDodgeAttempt -= LogDodgeAttempt;
            GameEvents.OnBallBlastEnded -= LogBallBlast;
            GameEvents.OnCrownAwarded -= LogCrown;
            GameEvents.OnEnergyBarFull -= LogBarFull;
            GameEvents.OnGameWon -= LogGameWon;
        }

        private void WriteRow(string playerId, string eventType, string d1 = "", string d2 = "", string d3 = "", string d4 = "")
        {
            if (_csvWriter == null) return;
            string timestamp = Time.time.ToString("F3");
            _csvWriter.WriteLine($"{_sessionId},{playerId},{timestamp},{eventType},{d1},{d2},{d3},{d4}");
            _csvWriter.Flush();
        }

        // --- Event Handlers ---

        private void LogObjectEaten(PlayerIndex p, FieldObjectType type, int bellyCount)
        {
            WriteRow(p.ToString(), "object_eaten", type.ToString(), bellyCount.ToString());
        }

        private void LogEquationGenerated(PlayerIndex p, int current, int target, EquationType type)
        {
            WriteRow(p.ToString(), "equation_generated", type.ToString(), current.ToString(), target.ToString());
        }

        private void LogEquationSolved(PlayerIndex p)
        {
            var belly = MathSystem.Instance != null ? MathSystem.Instance.GetCurrentBelly(p).ToString() : "";
            WriteRow(p.ToString(), "equation_solved", belly);
        }

        private void LogBellyAche(PlayerIndex p, int overshoot)
        {
            WriteRow(p.ToString(), "belly_ache", overshoot.ToString());
        }

        private void LogDodgeAttempt(PlayerIndex p, FieldObjectType type)
        {
            WriteRow(p.ToString(), "dodge_attempt", type.ToString());
        }

        private void LogBallBlast(int p1Count, int p2Count)
        {
            WriteRow("both", "ball_blast_ended", p1Count.ToString(), p2Count.ToString());
        }

        private void LogCrown(PlayerIndex p, int totalCrowns)
        {
            WriteRow(p.ToString(), "crown_awarded", totalCrowns.ToString());
        }

        private void LogBarFull()
        {
            WriteRow("shared", "energy_bar_full");
        }

        private void LogGameWon(PlayerIndex p)
        {
            WriteRow(p.ToString(), "game_won");
        }

        // --- Player Progress (JSON) ---

        [Serializable]
        private class PlayerProgress
        {
            public string lastSessionId;
            public int difficultyTier;
            public int totalSessions;
            public float lifetimeAccuracy;
        }

        public void SaveProgress()
        {
            var progress = new PlayerProgress
            {
                lastSessionId = _sessionId,
                difficultyTier = (int)(MathSystem.Instance != null ? MathSystem.Instance.CurrentTier : DifficultyTier.Tier1),
                totalSessions = 1, // TODO: increment from loaded
                lifetimeAccuracy = MathSystem.Instance != null ? MathSystem.Instance.GetAccuracy() : 0f
            };

            string json = JsonUtility.ToJson(progress, true);
            File.WriteAllText(_progressPath, json);
            Debug.Log($"[DataLogger] Progress saved to {_progressPath}");
        }

        public DifficultyTier LoadTier()
        {
            if (!File.Exists(_progressPath)) return DifficultyTier.Tier1;

            try
            {
                string json = File.ReadAllText(_progressPath);
                var progress = JsonUtility.FromJson<PlayerProgress>(json);
                return (DifficultyTier)progress.difficultyTier;
            }
            catch
            {
                return DifficultyTier.Tier1;
            }
        }
    }
}
