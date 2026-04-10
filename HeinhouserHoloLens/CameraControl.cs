using HarmonyLib;
using Il2CppLiv.Lck;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Recording.LCK;
using Il2CppRUMBLE.Recording.LCK.Extensions;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using System.Collections;
using UnityEngine;

namespace HeinhouserHoloLens
{
    internal static class CameraSettings
    {
        internal static int parkPlayer1 = 0;
        internal static int parkPlayer2 = 1;
        internal static float cameraMoveSpeed = 6.5f;
        internal static float cameraOrbitSpeed = 6f;
        internal static float cameraPositionCorrectionScaler = 0.01f;
        internal static float cameraDistanceFromMapCenterAllowed = 10.25f;
        internal static float playerCenterSmoothing = 6f;
        internal static float cameraPositionIncrease = 2f;
        internal static float cameraPositionFalloff = 0.25f;
        internal static float cameraPositionBuffer = 0.1f;
        internal static float allowedHeightScaler = 1.5f;

        internal static void SetStats(int parkPlayer1, int parkPlayer2, float cameraMoveSpeed, float cameraOrbitSpeed, float cameraPositionCorrectionScaler, float cameraDistanceFromMapCenterAllowed, float playerCenterSmoothing, float cameraPositionIncrease, float cameraPositionFalloff, float cameraPositionBuffer, float allowedHeightScaler)
        {
            CameraSettings.parkPlayer1 = parkPlayer1;
            CameraSettings.parkPlayer2 = parkPlayer2;
            CameraSettings.cameraMoveSpeed = cameraMoveSpeed;
            CameraSettings.cameraOrbitSpeed = cameraOrbitSpeed;
            CameraSettings.cameraPositionCorrectionScaler = cameraPositionCorrectionScaler;
            CameraSettings.cameraDistanceFromMapCenterAllowed = cameraDistanceFromMapCenterAllowed;
            CameraSettings.playerCenterSmoothing = playerCenterSmoothing;
            CameraSettings.cameraPositionIncrease = cameraPositionIncrease;
            CameraSettings.cameraPositionFalloff = cameraPositionFalloff;
            CameraSettings.cameraPositionBuffer = cameraPositionBuffer;
            CameraSettings.allowedHeightScaler = allowedHeightScaler;
        }

        internal static string SettingsToString()
        {
            return $"{(Core.isMatchmaking ? $"player1 = 0, player2 = 1, " : (Core.currentScene == "Park" ? $"parkPlayer1 = {parkPlayer1}, parkPlayer2 = {parkPlayer2}, " : ""))}"
                + $"cameraMoveSpeed = {cameraMoveSpeed}"
                + $", cameraOrbitSpeed = {cameraOrbitSpeed}"
                + $", cameraPositionCorrectionScaler = {cameraPositionCorrectionScaler}"
                + $", CameraDistanceFromMapCenterAllowed = {cameraDistanceFromMapCenterAllowed}"
                + $", playerCenterSmoothing = {playerCenterSmoothing}"
                + $", cameraPositionIncrease = {cameraPositionIncrease}"
                + $", cameraPositionFalloff = {cameraPositionFalloff}"
                + $", cameraPositionBuffer = {cameraPositionBuffer}"
                + $", allowedHeightScaler = {allowedHeightScaler}";
        }
    }

    internal class CameraControl
    {
        private static bool cameraIsBeingControlled = false;
        private static GameObject ddolHoloLens = null;
        private static GameObject activeHoloLens = null;
        private static Transform camera = null;
        private static bool IsRecording = false;

        [HarmonyPatch(typeof(LCKTabletUtility), nameof(LCKTabletUtility.OnRecordingStarted), new Type[] { typeof(LckResult) })]
        public static class LCKTabletUtilityOnRecordingStartedPatch
        {
            //runs after the player starts recording
            private static void Postfix(LckResult result)
            {
                IsRecording = true;
                UpdateRecordingLens();
            }
        }

        [HarmonyPatch(typeof(LCKTabletUtility), nameof(LCKTabletUtility.OnRecordingStopped), new Type[] { typeof(LckResult) })]
        public static class LCKTabletUtilityOnRecordingStoppedPatch
        {
            //runs after the player stops recording
            private static void Postfix(LckResult result)
            {
                IsRecording = false;
                UpdateRecordingLens();
            }
        }

        internal static void OnLateInitializeMelon()
        {
            //load and instantiate the GameObject in the asset bundle because it will be deloaded by the API automatically
            ddolHoloLens = GameObject.Instantiate(AssetBundles.LoadAssetFromStream<GameObject>(Core.instance, "HeinhouserHoloLens.hololens", "HoloLens"));
            ddolHoloLens.name = "HoloLens";
            ddolHoloLens.SetActive(false);
            GameObject.DontDestroyOnLoad(ddolHoloLens);
        }

        internal static void OnSceneWasLoaded() { cameraIsBeingControlled = false; } //immediately stops all camera controls on scene load

        internal static void MapLoaded(string map)
        {
            //on map load, start spectating or return to 1st person if needed
            if (Core.modEnabled && Core.isMatchmaking) { StartSpectate(); }
            else if (Core.modEnabled && Core.revertTo1stPerson && (Core.currentScene == "Gym")) { PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.lckCameraController.CameraModeChanged(CameraMode.Selfie); }
        }

        internal static void Save(bool holoLensEnabledChanged, bool showCameraInGameChanged, bool parkSpectateActiveChanged)
        {
            //store old values to check if they changed since last save
            int oldPlayer1 = CameraSettings.parkPlayer1;
            int oldPlayer2 = CameraSettings.parkPlayer2;
            float oldCameraMoveSpeed = CameraSettings.cameraMoveSpeed;
            float oldCameraOrbitSpeed = CameraSettings.cameraOrbitSpeed;
            float oldCameraPositionCorrectionScaler = CameraSettings.cameraPositionCorrectionScaler;
            float oldCameraDistanceFromMapCenterAllowed = CameraSettings.cameraDistanceFromMapCenterAllowed;
            float oldPlayerCenterSmoothing = CameraSettings.playerCenterSmoothing;
            float oldCameraPositionIncrease = CameraSettings.cameraPositionIncrease;
            float oldCameraPositionFalloff = CameraSettings.cameraPositionFalloff;
            float oldCameraPositionBuffer = CameraSettings.cameraPositionBuffer;
            float oldAllowedHeightScaler = CameraSettings.allowedHeightScaler;

            //update values
            CameraSettings.SetStats(
                (int)Core.settings[4].SavedValue,
                (int)Core.settings[5].SavedValue,
                (float)Core.settings[6].SavedValue,
                (float)Core.settings[7].SavedValue,
                (float)Core.settings[8].SavedValue,
                (float)Core.settings[9].SavedValue,
                (float)Core.settings[10].SavedValue,
                (float)Core.settings[11].SavedValue,
                (float)Core.settings[12].SavedValue,
                (float)Core.settings[13].SavedValue,
                (float)Core.settings[14].SavedValue);

            //check if the players to watch changed since last save
            bool playersChanged =
                (oldPlayer1 != CameraSettings.parkPlayer1)
                || (oldPlayer2 != CameraSettings.parkPlayer2);

            //check if any of the camera settings changed since last save
            bool settingsChanged =
                (oldCameraMoveSpeed != CameraSettings.cameraMoveSpeed)
                || (oldCameraOrbitSpeed != CameraSettings.cameraOrbitSpeed)
                || (oldCameraPositionCorrectionScaler != CameraSettings.cameraPositionCorrectionScaler)
                || (oldCameraDistanceFromMapCenterAllowed != CameraSettings.cameraDistanceFromMapCenterAllowed)
                || (oldPlayerCenterSmoothing != CameraSettings.playerCenterSmoothing)
                || (oldCameraPositionIncrease != CameraSettings.cameraPositionIncrease)
                || (oldCameraPositionFalloff != CameraSettings.cameraPositionFalloff)
                || (oldCameraPositionBuffer != CameraSettings.cameraPositionBuffer)
                || (oldAllowedHeightScaler != CameraSettings.allowedHeightScaler);

            if (settingsChanged)
            {//if camera settings changed, restart spectator camera to apply new settings if currently spectating
                if (Core.modEnabled && Core.isMatchmaking && cameraIsBeingControlled) { StartSpectate(); }
                else if (Core.modEnabled && cameraIsBeingControlled && (Core.currentScene == "Park")) { StartSpectate(CameraSettings.parkPlayer1, CameraSettings.parkPlayer2); }
            }
            else
            {//if settings are the same
                if (Core.isMatchmaking) //and we are in matchmaking
                {
                    //if the show camera in game setting changed, create/destroy the holo lens as needed
                    if (Core.modEnabled && showCameraInGameChanged)
                    {
                        if (Core.showHoloLensInGame && (activeHoloLens == null)) { CreateHoloLens(); }
                        else if (!Core.showHoloLensInGame && (activeHoloLens != null))
                        {
                            GameObject.Destroy(activeHoloLens);
                            activeHoloLens = null;
                        }
                    }
                    //if the holo lens enabled setting changed, start/stop spectating as needed
                    if (holoLensEnabledChanged)
                    {
                        if (Core.modEnabled) { StartSpectate(); }
                        else { cameraIsBeingControlled = false; }
                    }
                }
                else if (Core.currentScene == "Park") //and we are in the park
                {
                    //if the players to watch changed, start spectating with new players
                    if (playersChanged && Core.parkSpectateActive && Core.modEnabled) { StartSpectate(CameraSettings.parkPlayer1, CameraSettings.parkPlayer2); }
                    //if the park spectate active setting changed, start/stop spectating as needed
                    if (parkSpectateActiveChanged)
                    {
                        if (Core.parkSpectateActive) { StartSpectate(CameraSettings.parkPlayer1, CameraSettings.parkPlayer2); }
                        else { cameraIsBeingControlled = false; }
                    }
                }
            }
        }

        private static void UpdateRecordingLens()
        {
            //runs after the player starts/stops recording
            if (activeHoloLens != null)
            {
                activeHoloLens.transform.GetChild(0).GetChild(0).gameObject.SetActive(!IsRecording); //recording off (green)
                activeHoloLens.transform.GetChild(0).GetChild(1).gameObject.SetActive(IsRecording); //recording on (red)
            }
        }

        private static void CreateHoloLens()
        {
            if (activeHoloLens == null)
            {
                activeHoloLens = GameObject.Instantiate(ddolHoloLens);
                activeHoloLens.name = "HoloLens";
                activeHoloLens.SetActive(true);
                activeHoloLens.transform.SetParent(camera);
                activeHoloLens.transform.localPosition = Vector3.zero;
                activeHoloLens.transform.localRotation = Quaternion.identity;
                UpdateRecordingLens();
            }
        }

        //method to start the Setup Coroutine
        private static void StartSpectate(int player1 = 0, int player2 = 1) { MelonCoroutines.Start(StartSpectateCoroutine(player1, player2)); }

        //the Setup Coroutine to prep the camera for spectating
        private static IEnumerator StartSpectateCoroutine(int player1 = 0, int player2 = 1)
        {
            PlayerManager.instance.localPlayer.Controller.PlayerLIV.ShowTablet();
            yield return new WaitForSeconds(0.25f);

            MelonCoroutines.Start(RunSpectateCamera(player1, player2));
            yield return new WaitForSeconds(0.5f);

            LCKCameraController lckCameraController = PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.lckCameraController;
            lckCameraController.CameraModeChanged(CameraMode.Selfie);
            lckCameraController.SelfieFOVDoubleButton.ApplySavedData(90);
            lckCameraController.SelfieSmoothingDoubleButton.ApplySavedData(0);
            if (lckCameraController.CurrentCameraOrientation == LckCameraOrientation.Portrait) { lckCameraController._orientationButton.RPC_OnPressed(); }
            yield return new WaitForSeconds(0.25f);

            PlayerManager.instance.localPlayer.Controller.PlayerLIV.HideTablet();
            yield break;
        }

        //THE Coroutine that actually controls the camera
        private static IEnumerator RunSpectateCamera(int player1 = 0, int player2 = 1)
        {
            Melon<Core>.Logger.Msg($"Playing Spectate Mode: {CameraSettings.SettingsToString()}");
            //turn off for a tick in case of another camera control is being done
            if (cameraIsBeingControlled)
            {
                cameraIsBeingControlled = false;
                yield return new WaitForFixedUpdate();
            }
            cameraIsBeingControlled = true;

            //setup variables
            Transform selectedPlayer1 = PlayerManager.instance.AllPlayers.Count > player1 ? PlayerManager.instance.AllPlayers[player1].Controller.transform.FindChild("Visuals/Skelington/Bone_Pelvis/Bone_Spine_A/Bone_Chest") : PlayerManager.instance.localPlayer.Controller.transform.FindChild("Visuals/Skelington/Bone_Pelvis/Bone_Spine_A/Bone_Chest");
            Transform selectedPlayer2 = PlayerManager.instance.AllPlayers.Count > player2 ? PlayerManager.instance.AllPlayers[player2].Controller.transform.FindChild("Visuals/Skelington/Bone_Pelvis/Bone_Spine_A/Bone_Chest") : PlayerManager.instance.localPlayer.Controller.transform.FindChild("Visuals/Skelington/Bone_Pelvis/Bone_Spine_A/Bone_Chest");
            camera = PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.selfieCamera.gameObject.transform.parent;
            Transform cameraRotationControl = new GameObject("SmoothCameraControl").transform;
            Transform originalParent = PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.selfieCamera.gameObject.transform.parent.parent;
            Vector3 originalLocalPos = camera.localPosition;
            Quaternion originalLocalRot = camera.localRotation;
            float localDistance = Vector3.Distance(camera.position, selectedPlayer1.position);
            float remoteDistance = Vector3.Distance(camera.position, selectedPlayer2.position);
            float playerCenterLerpSpot = remoteDistance / (localDistance + remoteDistance);
            Vector3 smoothedPlayersCenter = Vector3.Lerp(selectedPlayer2.position, selectedPlayer1.position, playerCenterLerpSpot);

            //setup initializations
            camera.SetParent(null);
            bool isOrbitingRight = true;
            if (Core.showHoloLensInGame) { CreateHoloLens(); }

            //mimick current location
            cameraRotationControl.position = camera.position;
            cameraRotationControl.rotation = camera.rotation;

            //starting of the loop that controls the camera until spectating is turned off or we change scenes
            while (cameraIsBeingControlled && (selectedPlayer1 != null) && (selectedPlayer2 != null) && (camera != null) && (cameraRotationControl != null))
            {
                //persistent variable updates
                localDistance = Vector3.Distance(camera.position, selectedPlayer1.position);
                remoteDistance = Vector3.Distance(camera.position, selectedPlayer2.position);
                playerCenterLerpSpot = remoteDistance / (localDistance + remoteDistance);
                Vector3 rawPlayersCenter = Vector3.Lerp(selectedPlayer2.position, selectedPlayer1.position, playerCenterLerpSpot);
                smoothedPlayersCenter = Vector3.Lerp(smoothedPlayersCenter, rawPlayersCenter, CameraSettings.playerCenterSmoothing * Time.fixedDeltaTime);

                //new variable calculations
                float distanceToPlayersCenter = Vector3.Distance(smoothedPlayersCenter, camera.position);
                float distanceToCenter;
                float baseDistance = Vector3.Distance(selectedPlayer1.position, selectedPlayer2.position) / 2f;
                float scaledIncrease = CameraSettings.cameraPositionIncrease / (1f + baseDistance * CameraSettings.cameraPositionFalloff);
                float idealCameraDistance = baseDistance + scaledIncrease;
                float allowedHeight = Math.Min(10f, Vector3.Distance(selectedPlayer1.position, selectedPlayer2.position) / CameraSettings.allowedHeightScaler);

                //moves camera forwards/backwards as needed smoothly
                if (Mathf.Abs(distanceToPlayersCenter - idealCameraDistance) > CameraSettings.cameraPositionBuffer)
                {
                    Vector3 dirFromCenter = (cameraRotationControl.position - smoothedPlayersCenter).normalized;
                    cameraRotationControl.position = Vector3.Lerp(cameraRotationControl.position, smoothedPlayersCenter + dirFromCenter * idealCameraDistance, Time.fixedDeltaTime * CameraSettings.cameraMoveSpeed);
                }

                //move up/down as needed smoothly
                float clampedY = Mathf.Clamp(cameraRotationControl.position.y, smoothedPlayersCenter.y, smoothedPlayersCenter.y + allowedHeight);
                Vector3 clampedYVector = new(cameraRotationControl.position.x, clampedY, cameraRotationControl.position.z);
                cameraRotationControl.position = Vector3.Lerp(cameraRotationControl.position, clampedYVector, Time.fixedDeltaTime * CameraSettings.cameraMoveSpeed);

                if (Core.isMatchmaking)
                {
                    distanceToCenter = Vector2.Distance(Vector2.zero, new Vector2(cameraRotationControl.position.x, cameraRotationControl.position.z));
                    //move forwards when too far from center in Matchmaking smoothly
                    while ((Vector2.Distance(Vector2.zero, new Vector2(smoothedPlayersCenter.x, smoothedPlayersCenter.z)) < CameraSettings.cameraDistanceFromMapCenterAllowed) && (distanceToCenter > CameraSettings.cameraDistanceFromMapCenterAllowed))
                    {
                        cameraRotationControl.position += cameraRotationControl.forward * CameraSettings.cameraPositionCorrectionScaler;
                        cameraRotationControl.LookAt(smoothedPlayersCenter);
                        distanceToCenter = Vector2.Distance(Vector2.zero, new Vector2(cameraRotationControl.position.x, cameraRotationControl.position.z));
                    }
                }

                //camera orbit
                if (Vector3.Distance(camera.position, cameraRotationControl.position) < CameraSettings.cameraPositionBuffer / CameraSettings.cameraDistanceFromMapCenterAllowed)
                {
                    Vector3 movementAmount = (isOrbitingRight ? 1 : -1) * cameraRotationControl.right * CameraSettings.cameraPositionCorrectionScaler;
                    Vector3 desiredOrbitPos = cameraRotationControl.position - movementAmount;
                    if (!Core.isMatchmaking || (Vector2.Distance(Vector2.zero, new Vector2(desiredOrbitPos.x, desiredOrbitPos.z)) <= CameraSettings.cameraDistanceFromMapCenterAllowed))
                    {
                        cameraRotationControl.RotateAround(smoothedPlayersCenter, Vector3.up, (isOrbitingRight ? 1f : -1f) * CameraSettings.cameraOrbitSpeed * Time.fixedDeltaTime);
                    }
                    else { isOrbitingRight = !isOrbitingRight; } //or change what way it orbits
                }

                //update the actual camera
                cameraRotationControl.LookAt(smoothedPlayersCenter);
                camera.position = cameraRotationControl.position;
                camera.rotation = cameraRotationControl.rotation;

                yield return new WaitForFixedUpdate();
            } //ending of while loop that controls the camera

            //cleanup extra GameObjects if they are still around
            if (activeHoloLens != null) { GameObject.Destroy(activeHoloLens); }
            if (cameraRotationControl != null) { GameObject.Destroy(cameraRotationControl.gameObject); }
            //restore camera if it's still around
            if (camera != null)
            {
                camera.SetParent(originalParent);
                camera.localPosition = originalLocalPos;
                camera.localRotation = originalLocalRot;
            }

            //return camera to 1st person mode since spectating is done
            if (Core.revertTo1stPerson) { PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.lckCameraController.CameraModeChanged(CameraMode.Selfie); }

            yield break;
        }
    }
}
