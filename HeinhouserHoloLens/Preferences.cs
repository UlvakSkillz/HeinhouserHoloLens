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
        internal static MelonPreferences_Entry<bool> PrefParkSpectate;
        internal static MelonPreferences_Entry<int> PrefParkPlayer1;
        internal static MelonPreferences_Entry<int> PrefParkPlayer2;

        internal static MelonPreferences_Entry<float> PrefCameraMoveSpeed;
        internal static MelonPreferences_Entry<float> PrefCameraOrbitSpeed;
        internal static MelonPreferences_Entry<float> PrefCamPosCorrection;
        internal static MelonPreferences_Entry<float> PrefMaxCenterDist;

        internal static MelonPreferences_Entry<float> PrefPlayerCenterSmoothing;
        internal static MelonPreferences_Entry<float> PrefCamPosIncrease;
        internal static MelonPreferences_Entry<float> PrefCamPosFalloff;
        internal static MelonPreferences_Entry<float> PrefCamPosBuffer;
        internal static MelonPreferences_Entry<float> PrefAllowedHeightScaler;


    }
}