using MelonLoader;
using RumbleModdingAPI.RMAPI;
using RumbleModUI;

namespace HeinhouserHoloLens
{
    public static class BuildInfo
    {
        public const string ModName = "HeinhouserHoloLens";
        public const string ModVersion = "1.0.1";
        public const string Author = "UlvakSkillz";
    }

    public class Core : MelonMod
    {
        internal static MelonMod instance;
        internal static string currentScene = "Loader";
        private readonly Mod HeinhouserHoloLens = new();
        internal static readonly List<ModSetting> settings = new();
        internal static bool modEnabled = true;
        internal static bool showHoloLensInGame = true;
        internal static bool parkSpectateActive = false;
        internal static bool revertTo1stPerson = true;
        internal static bool isMatchmaking = false;
        internal static Random random = new();

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            isMatchmaking = currentScene.Contains("Map");
            if (settings.Count > 3)
            {
                settings[3].Value = false;
                settings[3].SavedValue = false;
            }
            CameraControl.OnSceneWasLoaded();
        }

        public override void OnLateInitializeMelon()
        {
            instance = this;
            CameraControl.OnLateInitializeMelon();
            Actions.onMapInitialized += CameraControl.MapLoaded;
            UI.instance.UI_Initialized += UIInit;
        }

        private void UIInit()
        {
            HeinhouserHoloLens.ModName = BuildInfo.ModName;
            HeinhouserHoloLens.ModVersion = BuildInfo.ModVersion;
            HeinhouserHoloLens.SetFolder(BuildInfo.ModName);
            settings.Add(HeinhouserHoloLens.AddToList("Enable HoloLens", true, 0, "Toggles HoloLens Camera On/Off", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Show HoloLens in Game", true, 0, "Toggles Camera Visuals On/Off", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Revert To 1st Person", true, 0, "Reverts to 1st Person View whenever Entering the Gym or Spectating Stops", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Park Spectate Activate", false, 0, "Toggles On/Off Park Spectate", new Tags { DoNotSave = true }));
            settings.Add(HeinhouserHoloLens.AddToList("Park Player 1", 0, $"Selects what Player to Spectate as Player 1 (0 = You, 1 = Oldest Remote Player, 2 = 2nd Oldest Remote Player, etc){Environment.NewLine}Defaults to Local Player if invalid", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Park Player 2", 1, $"Selects what Player to Spectate as Player 2 (0 = You, 1 = Oldest Remote Player, 2 = 2nd Oldest Remote Player, etc){Environment.NewLine}Defaults to Local Player if invalid", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Move Speed", 6.5f, $"Controls how fast the Camera moves Forwards/Backwards and Up/Down{Environment.NewLine}Default: 6.5", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Orbit Speed", 6f, $"Controls how fast the Camera Orbits around the Viewing Point{Environment.NewLine}Default: 6", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Position Correction Scaler", 0.01f, $"Controls how fast the Camera moves when Orbiting and getting away from Walls{Environment.NewLine}Default: 0.01", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Max Camera Distance From Map Center", 10.25f, $"Controls how far the Camera can move from the Center of the Matchmaking Map{Environment.NewLine}Default: 10.25", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Player Center Smoothing", 6f, $"Controls how Smooth the Look at Center of the Players moves{Environment.NewLine}Default: 6", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Position Increase", 2f, $"Controls how far away the Camera is from the Player's Center Point{Environment.NewLine}Default: 2", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Position Falloff", 0.25f, $"Controls how fast the Camera Position Increase amount falls off{Environment.NewLine}Default: 0.25", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Camera Position Buffer", 0.1f, $"Determines if the Camera needs to move (small but not 0 is best){Environment.NewLine}Default: 0.1", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Allowed Height Scaler", 1.5f, $"Scaled the Allowed Height of the Camera in comparison to the Player's Center Point Height (smaller = higher, bigger = lower){Environment.NewLine}Default: 1.5", new Tags { }));
            settings.Add(HeinhouserHoloLens.AddToList("Head Camera Chance Percent", 1, $"Changes the % chance to replace the HoloLens with a Player Head", new Tags { }));
            HeinhouserHoloLens.GetFromFile();
            HeinhouserHoloLens.ModSaved += Save;
            UI.instance.AddMod(HeinhouserHoloLens);
            Save();
        }

        private void Save()
        {
            bool pastHoloLensEnabled = modEnabled;
            bool pastShowCameraInGame = showHoloLensInGame;
            bool pastParkSpectateActive = parkSpectateActive;
            modEnabled = (bool)settings[0].SavedValue;
            showHoloLensInGame = (bool)settings[1].SavedValue;
            parkSpectateActive = (bool)settings[2].SavedValue;
            revertTo1stPerson = (bool)settings[3].SavedValue;
            CameraControl.Save(modEnabled != pastHoloLensEnabled, showHoloLensInGame != pastShowCameraInGame, parkSpectateActive != pastParkSpectateActive);
        }
    }
}
