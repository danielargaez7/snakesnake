using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Logs all gameplay events to CSV for data analysis.
    /// </summary>
    public class DataLogger : MonoBehaviour
    {
        public static DataLogger Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableLogging = true;

        private string _csvPath;
        private string _sessionId;
        private StreamWriter _csvWriter;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _csvPath = Path.Combine(Application.persistentDataPath, "belly_full_log.csv");
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
                _csvWriter.WriteLine("session_id,player_id,timestamp,event_type,data1,data2,data3");
            }
        }

        private void SubscribeEvents()
        {
            GameEvents.OnHedgehogCaught    += LogHedgehogCaught;
            GameEvents.OnHoleDelivered     += LogHoleDelivered;
            GameEvents.OnNumberCompleted   += LogNumberCompleted;
            GameEvents.OnNumberAdvanced    += LogNumberAdvanced;
            GameEvents.OnAllNumbersComplete+= LogAllNumbersComplete;
            GameEvents.OnBlastBallEaten    += LogBlastBallEaten;
            GameEvents.OnBallBlastEnded    += LogBallBlastEnded;
            GameEvents.OnCrownAwarded      += LogCrown;
            GameEvents.OnGameWon           += LogGameWon;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnHedgehogCaught    -= LogHedgehogCaught;
            GameEvents.OnHoleDelivered     -= LogHoleDelivered;
            GameEvents.OnNumberCompleted   -= LogNumberCompleted;
            GameEvents.OnNumberAdvanced    -= LogNumberAdvanced;
            GameEvents.OnAllNumbersComplete-= LogAllNumbersComplete;
            GameEvents.OnBlastBallEaten    -= LogBlastBallEaten;
            GameEvents.OnBallBlastEnded    -= LogBallBlastEnded;
            GameEvents.OnCrownAwarded      -= LogCrown;
            GameEvents.OnGameWon           -= LogGameWon;
        }

        private void WriteRow(string playerId, string eventType, string d1 = "", string d2 = "", string d3 = "")
        {
            if (_csvWriter == null) return;
            string timestamp = Time.time.ToString("F3");
            _csvWriter.WriteLine($"{_sessionId},{playerId},{timestamp},{eventType},{d1},{d2},{d3}");
            _csvWriter.Flush();
        }

        // --- Event Handlers ---

        private void LogHedgehogCaught(PlayerIndex p)
            => WriteRow(p.ToString(), "hedgehog_caught");

        private void LogHoleDelivered(PlayerIndex p, int holeIdx, int filled, int total)
            => WriteRow(p.ToString(), "hole_delivered", holeIdx.ToString(), filled.ToString(), total.ToString());

        private void LogNumberCompleted(PlayerIndex p, int number)
            => WriteRow(p.ToString(), "number_completed", number.ToString());

        private void LogNumberAdvanced(PlayerIndex p, int newNumber)
            => WriteRow(p.ToString(), "number_advanced", newNumber.ToString());

        private void LogAllNumbersComplete(PlayerIndex p)
            => WriteRow(p.ToString(), "all_numbers_complete");

        private void LogBlastBallEaten(PlayerIndex p)
            => WriteRow(p.ToString(), "blast_ball_eaten");

        private void LogBallBlastEnded(int p1Count, int p2Count)
            => WriteRow("both", "ball_blast_ended", p1Count.ToString(), p2Count.ToString());

        private void LogCrown(PlayerIndex p, int totalCrowns)
            => WriteRow(p.ToString(), "crown_awarded", totalCrowns.ToString());

        private void LogGameWon(PlayerIndex p)
            => WriteRow(p.ToString(), "game_won");
    }
}
