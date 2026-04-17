using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UIFramework;

namespace HeinhouserHoloLens
{
    public static class BuildInfo
    {
        public const string ModName = "HeinhouserHoloLens";
        public const string ModVersion = "1.1.6";
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
			UI.Register((MelonBase)this, Preferences.HoloLensCategory, Preferences.CameraSpectateCategory, Preferences.CameraMovementCategory, Preferences.CameraPositionCategory).OnModSaved += Save;
		}

		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			currentScene = sceneName;
			isMatchmaking = currentScene is "Map0" or "Map1";
			Preferences.PrefCamSpectate.Value = false;
            CameraControl.OnSceneWasLoaded();
		}

        public override void OnLateInitializeMelon()
        {
            instance = this;
            CameraControl.OnLateInitializeMelon();
            Actions.onMapInitialized += CameraControl.MapLoaded;
        }

		private void Save()
		{
            CameraControl.Save(Preferences.IsPrefChanged(Preferences.PrefEnable), Preferences.IsPrefChanged(Preferences.PrefShowInGame), Preferences.IsPrefChanged(Preferences.PrefCamSpectate));
			Preferences.StoreLastSavedPrefs();
		}
	}
}
