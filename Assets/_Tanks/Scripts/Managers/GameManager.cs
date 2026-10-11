using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {
        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }

        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }
        
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game.
        public float m_RoundDuration = 90f;

        public int RoundNumber => m_RoundNumber;
        public int PlayerCount => m_PlayerCount;
        public bool HasStarted => m_CurrentState == GameState.Game;
        public float RemainingTime { get; private set; }
        public bool IsRoundPlaying { get; private set; }
        public bool IsMatchOver => m_GameWinner != null;
        public TankManager RoundWinner => m_RoundWinner;
        public TankManager GameWinner => m_GameWinner;
        public event Action MatchCompleted;
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.

        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;            // The Prefab used by the tank in Slot 1 of the Menu
        public GameObject m_Tank2Prefab;            // The Prefab used by the tank in Slot 2 of the Menu
        public GameObject m_Tank3Prefab;            // The Prefab used by the tank in Slot 3 of the Menu
        public GameObject m_Tank4Prefab;            // The Prefab used by the tank in Slot 4 of the Menu
        
        [FormerlySerializedAs("m_Tanks")] 
        public TankManager[] m_SpawnPoints;         // A collection of managers for enabling and disabling different aspects of the tanks.
        
        private GameState m_CurrentState;
        
        private int m_RoundNumber;                  // Which round the game is currently on.
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

        private PlayerData[] m_TankData;            // Data passed from the menu about each selected tank (at least 2, max 4)
        private int m_PlayerCount = 0;              // The number of players (2 to 4), decided from the number of PlayerData passed by the menu
        private TextMeshProUGUI m_TitleText;        // The text used to display game message. Automatically found as part of the Menu prefab
        private GameObject m_AnnouncementPanel;

        private void Start()
        {
            m_CurrentState = GameState.MainMenu;
            EnsureCameraControl();
            EnsureTitleText();
        }

        private void EnsureCameraControl()
        {
            if (m_CameraControl == null)
            {
                m_CameraControl = FindAnyObjectByType<CameraControl>(FindObjectsInactive.Include);
                if (m_CameraControl == null)
                {
                    var mainCam = Camera.main;
                    if (mainCam != null)
                        m_CameraControl = mainCam.GetComponentInParent<CameraControl>() ?? mainCam.GetComponent<CameraControl>() ?? mainCam.gameObject.AddComponent<CameraControl>();
                }
            }
        }

        private void EnsureTitleText()
        {
            if (m_TitleText != null) return;
            // Keep announcements on their own canvas so replacing a menu or changing
            // language cannot leave the match text attached to an obsolete UI root.
            var canvasGo = new GameObject("Round Announcements", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            m_AnnouncementPanel = new GameObject("Round message", typeof(RectTransform), typeof(Image));
            m_AnnouncementPanel.transform.SetParent(canvasGo.transform, false);
            var panelRect = m_AnnouncementPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1040, 300);
            panelRect.anchoredPosition = new Vector2(0, 35);
            var panelImage = m_AnnouncementPanel.GetComponent<Image>();
            panelImage.color = new Color(0.045f, 0.085f, 0.09f, 0.94f);
            panelImage.raycastTarget = false;

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(m_AnnouncementPanel.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.sizeDelta = new Vector2(0, 7);
            accent.GetComponent<Image>().color = new Color(0.96f, 0.65f, 0.23f);
            accent.GetComponent<Image>().raycastTarget = false;

            var announcerObj = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            announcerObj.transform.SetParent(m_AnnouncementPanel.transform, false);
            var rt = announcerObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(44, 24);
            rt.offsetMax = new Vector2(-44, -24);

            m_TitleText = announcerObj.GetComponent<TextMeshProUGUI>();
            var interfaceFont = Resources.Load<TMP_FontAsset>("UI/Inter SDF");
            if (interfaceFont != null) m_TitleText.font = interfaceFont;
            m_TitleText.fontSize = 52;
            m_TitleText.enableAutoSizing = true;
            m_TitleText.fontSizeMin = 31;
            m_TitleText.fontSizeMax = 52;
            m_TitleText.textWrappingMode = TextWrappingModes.Normal;
            m_TitleText.overflowMode = TextOverflowModes.Truncate;
            m_TitleText.alignment = TextAlignmentOptions.Center;
            m_TitleText.fontStyle = FontStyles.Bold;
            m_TitleText.color = new Color(0.95f, 0.98f, 1f, 1f);
            m_TitleText.raycastTarget = false;
            TankDuelLocalization.EnsureTurkishSupport(m_TitleText.font);
            m_TitleText.text = "";
            m_AnnouncementPanel.SetActive(false);
        }

        private void ShowAnnouncement(string message)
        {
            EnsureTitleText();
            m_TitleText.text = message;
            m_AnnouncementPanel.SetActive(!string.IsNullOrEmpty(message));
        }

        void GameStart()
        {
            // Create the delays so they only have to be made once.
            m_EndWait = new WaitForSeconds (m_EndDelay);

            SpawnAllTanks();
            SetCameraTargets();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            StartCoroutine (GameLoop ());
        }

        void ChangeGameState(GameState newState)
        {
            m_CurrentState = newState;

            switch (m_CurrentState)
            {
                case GameState.Game:
                    GameStart();
                    break;
            }
        }

        // Called by the menu, passing along the data from the selection made by the player in the menu
        public void StartGame(PlayerData[] playerData)
        {
            if (HasStarted) return;
            if (playerData == null || playerData.Length < 2)
            {
                Debug.LogError("Cannot start match: player data is null or fewer than 2 players.");
                return;
            }

            EnsureCameraControl();
            EnsureTitleText();
            if (m_CameraControl == null || m_TitleText == null)
            {
                Debug.LogError("Cannot start match: missing camera or round announcement text.");
                return;
            }
            var interfaceFont = Resources.Load<TMP_FontAsset>("UI/Inter SDF");
            if (interfaceFont != null) m_TitleText.font = interfaceFont;
            m_TitleText.enableAutoSizing = true;
            m_TitleText.fontSizeMin = 31f;
            m_TitleText.fontSizeMax = 52f;
            m_TitleText.textWrappingMode = TextWrappingModes.Normal;
            m_TitleText.overflowMode = TextOverflowModes.Truncate;

            if (m_SpawnPoints == null || m_SpawnPoints.Length < playerData.Length)
            {
                var allObjs = FindObjectsByType<Transform>(FindObjectsInactive.Include);
                var foundSpawns = new System.Collections.Generic.List<Transform>();
                foreach (var tr in allObjs)
                {
                    if (tr.name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0 && tr.name.IndexOf("Point", StringComparison.OrdinalIgnoreCase) >= 0)
                        foundSpawns.Add(tr);
                }
                if (foundSpawns.Count >= playerData.Length)
                {
                    m_SpawnPoints = new TankManager[foundSpawns.Count];
                    for (int i = 0; i < foundSpawns.Count; i++)
                    {
                        m_SpawnPoints[i] = new TankManager { m_SpawnPoint = foundSpawns[i] };
                    }
                }
            }

            if (m_SpawnPoints == null || m_SpawnPoints.Length < playerData.Length)
            {
                Debug.LogError("Cannot start match: missing spawn points.");
                return;
            }

            for (int i = 0; i < playerData.Length; i++)
            {
                if (m_SpawnPoints[i] == null || m_SpawnPoints[i].m_SpawnPoint == null)
                {
                    Debug.LogError($"Cannot start match: player {i + 1} has no spawn point.");
                    return;
                }
                var fallback = i == 0 ? m_Tank1Prefab : i == 1 ? m_Tank2Prefab : m_Tank1Prefab;
                var prefab = playerData[i]?.UsedPrefab != null ? playerData[i].UsedPrefab : fallback;
                if (prefab == null || prefab.GetComponent<TankMovement>() == null ||
                    prefab.GetComponent<TankShooting>() == null || prefab.GetComponent<TankHealth>() == null)
                {
                    Debug.LogError($"Cannot start match: player {i + 1} has an invalid tank prefab.");
                    return;
                }
                playerData[i].UsedPrefab = prefab;
            }

            m_CurrentState = GameState.MainMenu;

            m_TankData = playerData;
            m_PlayerCount = m_TankData.Length;
            ChangeGameState(GameState.Game);
        }


        private void SpawnAllTanks()
        {
            // For all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                var playerData = m_TankData[i];
                var prefabToSpawn = playerData.UsedPrefab;
                if (prefabToSpawn == null)
                {
                    if (i == 0 && m_Tank1Prefab != null) prefabToSpawn = m_Tank1Prefab;
                    else if (i == 1 && m_Tank2Prefab != null) prefabToSpawn = m_Tank2Prefab;
                    else if (m_Tank1Prefab != null) prefabToSpawn = m_Tank1Prefab;
                    else if (m_Tank2Prefab != null) prefabToSpawn = m_Tank2Prefab;
                }

                if (prefabToSpawn == null)
                {
                    Debug.LogError($"GameManager: Cannot spawn tank {i + 1} - no prefab available.");
                    continue;
                }

                // ... create them, set their player number and references needed for control.
                m_SpawnPoints[i].m_Instance =
                    Instantiate(prefabToSpawn, m_SpawnPoints[i].m_SpawnPoint.position, m_SpawnPoints[i].m_SpawnPoint.rotation) as GameObject;

                //this guard against possible user error : if they created a prefab with Is Computer Control set to true
                //then all of those prefab would be bots. So we ensure it's to false (the IsComputer from player data
                //will re-enable this if needed when the game start)
                var mov = m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();
                mov.m_IsComputerControlled = false;
                
                m_SpawnPoints[i].m_PlayerNumber = i + 1;
                m_SpawnPoints[i].ControlIndex = playerData.ControlIndex;
                m_SpawnPoints[i].m_PlayerColor = playerData.TankColor;
                m_SpawnPoints[i].m_ComputerControlled = playerData.IsComputer;
            }

            //we delayed setup after all tanks are created as they expect to have access to all other tanks in the manager
            for (int i = 0; i < m_PlayerCount; i++)
            {
                var tank = m_SpawnPoints[i];
                if(tank == null || tank.m_Instance == null)
                    continue;
                
                tank.Setup(this);
            }
        }


        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[m_PlayerCount];

            // For each of these transforms...
            for (int i = 0; i < targets.Length; i++)
            {
                // ... set it to the appropriate tank transform.
                targets[i] = m_SpawnPoints[i].m_Instance.transform;
            }

            // These are the targets the camera should follow.
            m_CameraControl.m_Targets = targets;
            m_CameraControl.m_FollowFirstTarget = TankDuelData.AIModeEnabled;
        }


        // This is called from start and will run each phase of the game one after another.
        private IEnumerator GameLoop ()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundStarting ());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found.
            if (m_GameWinner != null)
            {
                ShowAnnouncement(string.Empty);
                MatchCompleted?.Invoke();
            }
            else
            {
                // If there isn't a winner yet, restart this coroutine so the loop continues.
                // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
                StartCoroutine (GameLoop ());
            }
        }


        private IEnumerator RoundStarting ()
        {
            // As soon as the round starts reset the tanks and make sure they can't move.
            ResetAllTanks ();
            DisableTankControl ();

            // Snap the camera's zoom and position to something appropriate for the reset tanks.
            m_CameraControl.SetStartPositionAndSize ();

            // Increment the round number and display text showing the players what round it is.
            RemainingTime = Mathf.Max(1f, m_RoundDuration);
            m_RoundNumber++;
            float countdown = Mathf.Max(0f, m_StartDelay);
            int shown = -1;
            while (countdown > 0f)
            {
                int seconds = Mathf.CeilToInt(countdown);
                if (seconds != shown)
                {
                    shown = seconds;
                    ShowAnnouncement((TankDuelLocalization.IsTurkish ? "RAUND " : "ROUND ") +
                        m_RoundNumber + "\n" + seconds);
                }
                yield return null;
                countdown -= Time.deltaTime;
            }
        }


        private IEnumerator RoundPlaying ()
        {
            RemainingTime = Mathf.Max(1f, m_RoundDuration);
            IsRoundPlaying = true;
            EnableTankControl ();
            ShowAnnouncement(string.Empty);

            while (!OneTankLeft() && RemainingTime > 0f)
            {
                yield return null;
                RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            }

            IsRoundPlaying = false;
        }


        private IEnumerator RoundEnding ()
        {
            // Stop tanks from moving.
            DisableTankControl ();

            // Clear the winner from the previous round.
            m_RoundWinner = null;

            // See if there is a winner now the round is over.
            m_RoundWinner = GetRoundWinner ();

            // If there is a winner, increment their score.
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game.
            m_GameWinner = GetGameWinner ();

            // Get a message based on the scores and whether or not there is a game winner and display it.
            string message = EndMessage ();
            ShowAnnouncement(message);

            // Wait for the specified length of time until yielding control back to the game loop.
            yield return m_EndWait;
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end.
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero.
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // ... and if they are active, increment the counter.
                if (m_SpawnPoints[i].m_Instance != null && m_SpawnPoints[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false.
            return numTanksLeft <= 1;
        }


        // This function finds a surviving tank, or compares remaining health when time runs out.
        private TankManager GetRoundWinner()
        {
            if (RemainingTime <= 0f && !OneTankLeft())
            {
                TankManager healthiest = null;
                float bestHealth = -1f;
                bool tied = false;

                for (int i = 0; i < m_PlayerCount; i++)
                {
                    var tank = m_SpawnPoints[i];
                    if (tank.m_Instance == null || !tank.m_Instance.activeSelf)
                        continue;

                    var healthComp = tank.m_Instance.GetComponent<TankHealth>();
                    float health = healthComp != null ? healthComp.CurrentHealth : 0f;
                    if (health > bestHealth + 0.01f)
                    {
                        healthiest = tank;
                        bestHealth = health;
                        tied = false;
                    }
                    else if (Mathf.Abs(health - bestHealth) <= 0.01f)
                    {
                        tied = true;
                    }
                }

                return tied ? null : healthiest;
            }

            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i].m_Instance != null && m_SpawnPoints[i].m_Instance.activeSelf)
                    return m_SpawnPoints[i];
            }

            return null;
        }


        // This function is to find out if there is a winner of the game.
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < m_PlayerCount; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it.
                if (m_SpawnPoints[i].m_Wins == m_NumRoundsToWin)
                    return m_SpawnPoints[i];
            }

            // If no tanks have enough rounds to win, return null.
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw.
            string message = TankDuelLocalization.IsTurkish ? "BERABERE!" : "DRAW!";

            // If there is a winner then change the message to reflect that.
            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + (TankDuelLocalization.IsTurkish ? " RAUNDU KAZANDI!" : " WINS THE ROUND!");

            // Add some line breaks after the initial message.
            message += "\n";

            // Go through all the tanks and add each of their scores to the message.
            for (int i = 0; i < m_PlayerCount; i++)
            {
                message += m_SpawnPoints[i].m_ColoredPlayerText + ": " + m_SpawnPoints[i].m_Wins + (TankDuelLocalization.IsTurkish ? " ZAFER\n" : " WINS\n");
            }

            // If there is a game winner, change the entire message to reflect that.
            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + (TankDuelLocalization.IsTurkish ? " OYUNU KAZANDI!" : " WINS THE GAME!");

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties.
        private void ResetAllTanks()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i] != null)
                    m_SpawnPoints[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i] != null)
                    m_SpawnPoints[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < m_PlayerCount; i++)
            {
                if (m_SpawnPoints[i] != null)
                    m_SpawnPoints[i].DisableControl();
            }
        }
    }
}
