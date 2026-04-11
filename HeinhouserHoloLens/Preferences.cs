using MelonLoader;
using RumbleModdingAPI.RMAPI;
using RumbleModUI;

namespace HeinhouserHoloLens
{
	public class Preferences
	{
		private const string CONFIG_FILE = "config.cfg";
		private const string USER_DATA = "UserData/HeinhouserHoloLens/";

		internal static MelonPreferences_Category HoloLensCategory;
		internal static MelonPreferences_Entry<bool> PrefEnable;
		internal static MelonPreferences_Entry<bool> PrefShowInGame;
		internal static MelonPreferences_Entry<bool> PrefRevertToFps;
		internal static MelonPreferences_Entry<int> PrefHeadChance;

		internal static MelonPreferences_Category ParkSpectateCategory;
		internal static MelonPreferences_Entry<bool> PrefParkSpectate;
		internal static MelonPreferences_Entry<int> PrefParkPlayer1;
		internal static MelonPreferences_Entry<int> PrefParkPlayer2;

		internal static MelonPreferences_Category CameraMovementCategory;
		internal static MelonPreferences_Entry<float> PrefCameraMoveSpeed;
		internal static MelonPreferences_Entry<float> PrefCameraOrbitSpeed;
		internal static MelonPreferences_Entry<float> PrefCamPosCorrection;
		internal static MelonPreferences_Entry<float> PrefMaxCenterDist;

		internal static MelonPreferences_Category CameraPositionCategory;
		internal static MelonPreferences_Entry<float> PrefPlayerCenterSmoothing;
		internal static MelonPreferences_Entry<float> PrefCamPosIncrease;
		internal static MelonPreferences_Entry<float> PrefCamPosFalloff;
		internal static MelonPreferences_Entry<float> PrefCamPosBuffer;
		internal static MelonPreferences_Entry<float> PrefAllowedHeightScaler;

		internal static void InitPrefs()
		{
			if (!Directory.Exists(USER_DATA))
				Directory.CreateDirectory(USER_DATA);

			//General settings
			HoloLensCategory = MelonPreferences.CreateCategory("HoloLens", "Settings");
			HoloLensCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			PrefEnable = HoloLensCategory.CreateEntry("EnableHoloLens", true, "Enable Holo Lens", "Toggles HoloLens Camera On/Off when entering Matchmaking");
			PrefShowInGame = HoloLensCategory.CreateEntry("ShowInGame", true, "Show In Game", "Toggles Camera Visual On/Off");
			PrefRevertToFps = HoloLensCategory.CreateEntry("RevertToFps", true, "Revert To 1st Person", "Reverts to 1st Person View whenever entering the Gym or Spectating Stops");
			PrefHeadChance = HoloLensCategory.CreateEntry("Head Camera Chance", 1, "Head Camera Chance Percent", $"Changes the % chance to replace the HoloLens with a Player Head");

			//Park settings
			ParkSpectateCategory = MelonPreferences.CreateCategory("Park", "Park Settings");
			ParkSpectateCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));
			
			PrefParkSpectate = ParkSpectateCategory.CreateEntry("ParkSpectate", false, "Park Spectate Activate", "Toggles On/Off Park Spectate");
			PrefParkPlayer1 = ParkSpectateCategory.CreateEntry("ParkPlayer1", 0, "Park Player 1", $"Selects what Player to Spectate as Player 1 (0 = You, 1 = Oldest Remote Player, 2 = 2nd Oldest Remote Player, etc){Environment.NewLine}Defaults to Local Player if invalid");
			PrefParkPlayer2 = ParkSpectateCategory.CreateEntry("ParkPlayer2", 1, "Park Player 2", $"Selects what Player to Spectate as Player 2 (0 = You, 1 = Oldest Remote Player, 2 = 2nd Oldest Remote Player, etc){Environment.NewLine}Defaults to Local Player if invalid");

			//Camera Movement Settings
			CameraMovementCategory = MelonPreferences.CreateCategory("Camera Movement", "Camera Movement");
			CameraMovementCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));
			
			PrefCameraMoveSpeed = CameraMovementCategory.CreateEntry("CameraMoveSpeed", 6.5f, "Move Speed", $"Controls how fast the Camera moves Forwards/Backwards and Up/Down{Environment.NewLine}Default: 6.5");
			PrefCameraOrbitSpeed = CameraMovementCategory.CreateEntry("CameraOrbitSpeed", 6f, "Orbit Speed", $"Controls how fast the Camera Orbits around the Viewing Point{Environment.NewLine}Default: 6");
			PrefCamPosCorrection = CameraMovementCategory.CreateEntry("CamPosCorrection", 0.01f, "Position Correction Scaler", $"Controls how fast the Camera moves when Orbiting and getting away from Walls{Environment.NewLine}Default: 0.01");
			PrefMaxCenterDist = CameraMovementCategory.CreateEntry("MaxCenterDist", 10.25f, "Max Center Distance", $"Controls how far the Camera can move from the Center of the Matchmaking Map{Environment.NewLine}Default: 10.25");

			//Camera Position Settings
			CameraPositionCategory = MelonPreferences.CreateCategory("Camera Position", "Camera Position");
			CameraPositionCategory.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			PrefPlayerCenterSmoothing = CameraPositionCategory.CreateEntry("PlayerCenterSmoothing", 0.1f, "Player Center Smoothing", $"Controls how much the Camera Smooths out the Player's Position when trying to Center on them{Environment.NewLine}Default: 0.1");
			PrefCamPosIncrease = CameraPositionCategory.CreateEntry("CamPosIncrease", 2f, "Camera Position Increase", $"Controls how far away the Camera is from the Player's Center Point{Environment.NewLine}Default: 2");
			PrefCamPosFalloff = CameraPositionCategory.CreateEntry("CamPosFalloff", 0.25f, "Camera Position Falloff", $"Controls how fast the Camera Position Increase amount falls off{Environment.NewLine}Default: 0.25");
			PrefCamPosBuffer = CameraPositionCategory.CreateEntry("CamPosBuffer", 0.1f, "Camera Position Buffer", $"Determines if the Camera needs to move (small but not 0 is best){Environment.NewLine}Default: 0.1");
			PrefAllowedHeightScaler = CameraPositionCategory.CreateEntry("AllowedHeightScaler", 1f, "Allowed Height Scaler", $"Controls the maximum allowed height for the Camera{Environment.NewLine}Default: 1");

			//MelonPreferences.OnPreferencesSaved.Subscribe(Save);

			PrefParkSpectate.ResetToDefault(); //Ignore saved setting to emulate ModUI DoNotSave tag;
			StoreLastSavedPrefs();
		}



		internal static Dictionary<MelonPreferences_Entry, object> LastSavedValues = new();
		internal static void StoreLastSavedPrefs()
		{
			List<MelonPreferences_Entry> prefs = new();
			prefs.AddRange(HoloLensCategory.Entries);
			prefs.AddRange(ParkSpectateCategory.Entries);
			prefs.AddRange(CameraMovementCategory.Entries);
			prefs.AddRange(CameraPositionCategory.Entries);

			foreach (MelonPreferences_Entry entry in  prefs)
			{
				LastSavedValues[entry] = entry.BoxedValue;
			}
		}

		public static bool AnyPrefsChanged()
		{
			foreach (KeyValuePair<MelonPreferences_Entry, object> pair in LastSavedValues)
			{
				if (!pair.Key.BoxedValue.Equals(pair.Value))
					return true;
			}
			return false;
		}

		public static bool IsPrefChanged(MelonPreferences_Entry entry)
		{
			if (LastSavedValues.TryGetValue(entry, out object? lastValue))
			{
				return !entry.BoxedValue.Equals(lastValue);
			}
			return false;
		}


		private static void Save(string categoryPath)
		{
		//	if (!categoryPath.Contains("HeinhouserHoloLens"))
		//		return;
		//	bool pastHoloLensEnabled = modEnabled;
		//	bool pastShowCameraInGame = showHoloLensInGame;
		//	bool pastParkSpectateActive = parkSpectateActive;
		//	modEnabled = (bool)settings[0].SavedValue;
		//	showHoloLensInGame = (bool)settings[1].SavedValue;
		//	parkSpectateActive = (bool)settings[2].SavedValue;
		//	revertTo1stPerson = (bool)settings[3].SavedValue;
		//	CameraControl.Save(modEnabled != pastHoloLensEnabled, showHoloLensInGame != pastShowCameraInGame, parkSpectateActive != pastParkSpectateActive);
		}

	}
}