using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UIFramework;
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
		internal static bool isMatchmaking = false;
		internal static Random random = new();
		public override void OnInitializeMelon()
		{
			Preferences.InitPrefs();
			UI.Register(this,Preferences.HoloLensCategory, Preferences.ParkSpectateCategory, Preferences.CameraMovementCategory, Preferences.CameraPositionCategory);
			MelonPreferences.OnPreferencesSaved.Subscribe(Save);
		}
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			currentScene = sceneName;
			isMatchmaking = currentScene.Contains("Map");
			Preferences.PrefParkSpectate.Value = false;
			CameraControl.OnSceneWasLoaded();
		}

        public override void OnLateInitializeMelon()
        {
            instance = this;
            CameraControl.OnLateInitializeMelon();
            Actions.onMapInitialized += CameraControl.MapLoaded;
        }
		private void Save(string savePath)
		{
			if (!savePath.Contains("HeinhouserHoloLens"))
				return;//Return if the saved config isn't for this mod

			CameraControl.Save(Preferences.IsPrefChanged(Preferences.PrefEnable), Preferences.IsPrefChanged(Preferences.PrefShowInGame), Preferences.IsPrefChanged(Preferences.PrefParkSpectate));

			Preferences.StoreLastSavedPrefs();
			LoggerInstance.Msg("Saved config");

		}
	}
}
