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
		internal static string SettingsToString()
		{
			return $"{(Core.isMatchmaking ? $"player1 = 0, player2 = 1, " : (Core.currentScene == "Park" ? $"parkPlayer1 = {Preferences.PrefCamPlayer1.Value}, parkPlayer2 = {Preferences.PrefCamPlayer2.Value}, " : ""))}"
				+ $"cameraMoveSpeed = {Preferences.PrefCameraMoveSpeed.Value}"
				+ $", cameraOrbitSpeed = {Preferences.PrefCameraOrbitSpeed.Value}"
				+ $", cameraPositionCorrectionScaler = {Preferences.PrefCamPosCorrection.Value}"
				+ $", CameraDistanceFromMapCenterAllowed = {Preferences.PrefMaxCenterDist.Value}"
				+ $", playerCenterSmoothing = {Preferences.PrefPlayerCenterSmoothing.Value}"
				+ $", cameraPositionIncrease = {Preferences.PrefCamPosIncrease.Value}"
				+ $", cameraPositionFalloff = {Preferences.PrefCamPosFalloff.Value}"
				+ $", cameraPositionBuffer = {Preferences.PrefCamPosBuffer.Value}"
				+ $", allowedHeightScaler = {Preferences.PrefAllowedHeightScaler.Value}";
		}
	}

    internal class CameraControl
    {
        private static bool cameraIsBeingControlled = false;
        private static GameObject ddolHoloLens = null;
        private static GameObject activeHoloLens = null;
        private static Transform camera = null;
        private static bool IsRecording = false;
        private static bool grabbedMaterial = false;
        private static bool IsActiveHoloLensNormal = true;


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
            if (!grabbedMaterial && (Core.currentScene == "Gym"))
            {
                ddolHoloLens.transform.GetChild(0).GetChild(3).GetComponent<MeshRenderer>().material = GameObjects.Gym.TUTORIAL.Worldtutorials.CombatCarvings.CombatCarvingPosture.CarvingHeadParent.PostureCarvingHead.TposePlayer.GetGameObject().GetComponent<MeshRenderer>().material;
                grabbedMaterial = true;
            }
            if (Preferences.PrefEnable.Value && Core.isMatchmaking) { StartSpectate(); }
            else if (Preferences.PrefEnable.Value && Preferences.PrefRevertToFps.Value && (Core.currentScene == "Gym")) { PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.lckCameraController.CameraModeChanged(CameraMode.FirstPerson); }
        }

        internal static void Save(bool holoLensEnabledChanged, bool showCameraInGameChanged, bool spectateActiveChanged)
        {
            bool playersChanged = Preferences.IsPrefChanged(Preferences.PrefCamPlayer1) || Preferences.IsPrefChanged(Preferences.PrefCamPlayer2);

			//check if any of the camera settings changed since last save
			bool settingsChanged =
                Preferences.IsPrefChanged(Preferences.PrefCameraMoveSpeed)
                || Preferences.IsPrefChanged(Preferences.PrefCameraOrbitSpeed)
                || Preferences.IsPrefChanged(Preferences.PrefCamPosCorrection)
                || Preferences.IsPrefChanged(Preferences.PrefMaxCenterDist)
                || Preferences.IsPrefChanged(Preferences.PrefPlayerCenterSmoothing)
                || Preferences.IsPrefChanged(Preferences.PrefCamPosIncrease)
                || Preferences.IsPrefChanged(Preferences.PrefCamPosFalloff)
                || Preferences.IsPrefChanged(Preferences.PrefCamPosBuffer)
                || Preferences.IsPrefChanged(Preferences.PrefAllowedHeightScaler);

            if (settingsChanged)
			{//if camera settings changed, restart spectator camera to apply new settings if currently spectating
                if (Preferences.PrefEnable.Value && Core.isMatchmaking && cameraIsBeingControlled) { StartSpectate(); }
				else if (Preferences.PrefEnable.Value && cameraIsBeingControlled && Core.currentScene.Contains("Park")) { StartSpectate(Preferences.PrefCamPlayer1.Value, Preferences.PrefCamPlayer2.Value); }
			}
			else
            {//if settings are the same
                if (Core.isMatchmaking) //and we are in matchmaking
                {
                    //if the show camera in game setting changed, create/destroy the holo lens as needed
                    if (Preferences.PrefEnable.Value && showCameraInGameChanged)
                    {
                        if (Preferences.PrefShowInGame.Value && (activeHoloLens == null)) { CreateHoloLens(); }
						else if (!Preferences.PrefShowInGame.Value && (activeHoloLens != null))
                        {
                            GameObject.Destroy(activeHoloLens);
							activeHoloLens = null;
						}
					}
					//if the holo lens enabled setting changed, start/stop spectating as needed
					if (holoLensEnabledChanged)
                    {
                        if (Preferences.PrefEnable.Value) { StartSpectate(); }
						else { cameraIsBeingControlled = false; }
					}
				}
				else
				{
					//if the players to watch changed, start spectating with new players
					if (playersChanged && Preferences.PrefCamSpectate.Value && Preferences.PrefEnable.Value) { StartSpectate(Preferences.PrefCamPlayer1.Value, Preferences.PrefCamPlayer2.Value); }
					//if the spectate active setting changed, start/stop spectating as needed
					if (spectateActiveChanged)
					{
						if (Preferences.PrefCamSpectate.Value) { StartSpectate(Preferences.PrefCamPlayer1.Value, Preferences.PrefCamPlayer2.Value); }
						else { cameraIsBeingControlled = false; }
					}
				}
			}
		}

        private static void UpdateRecordingLens()
        {
            //runs after the player starts/stops recording
            if (IsActiveHoloLensNormal && (activeHoloLens != null))
            {
                activeHoloLens.transform.GetChild(0).GetChild(0).gameObject.SetActive(!IsRecording); //recording off (green)
                activeHoloLens.transform.GetChild(0).GetChild(1).gameObject.SetActive(IsRecording); //recording on (red)
            }
        }

        private static void CreateHoloLens()
        {
            if (activeHoloLens == null)
            {
                //create stored HoloLens
                activeHoloLens = GameObject.Instantiate(ddolHoloLens);
                activeHoloLens.name = "HoloLens";
                activeHoloLens.SetActive(true);
                activeHoloLens.transform.SetParent(camera.GetChild(0));
                activeHoloLens.transform.localPosition = Vector3.zero;
                activeHoloLens.transform.localRotation = Quaternion.identity;
                //if chance for HeadShot succeeds, change to head shown
                if (Core.random.Next(0, 100) < Preferences.PrefHeadChance.Value)
                {
                    IsActiveHoloLensNormal = false;
                    activeHoloLens.transform.GetChild(0).GetChild(0).gameObject.SetActive(false); //green lens
                    activeHoloLens.transform.GetChild(0).GetChild(1).gameObject.SetActive(false); //red lens
                    activeHoloLens.transform.GetChild(0).GetChild(2).gameObject.SetActive(false); //ring
                    activeHoloLens.transform.GetChild(0).GetChild(3).gameObject.SetActive(true); //head
                }
                else { UpdateRecordingLens(); } //otherwise update lens as normal
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
            lckCameraController.SelfieFOVDoubleButton.ApplySavedData(Preferences.PrefCamFOV.Value);
            lckCameraController.SelfieSmoothingDoubleButton.ApplySavedData(0); // 0 camera smoothing since we lerp 4 times per loop
            if (lckCameraController.CurrentCameraOrientation != Preferences.PrefCamMode.Value) { lckCameraController._orientationButton.RPC_OnPressed(); }
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
            if (Preferences.PrefShowInGame.Value) { CreateHoloLens(); }

            //mimick current location
            cameraRotationControl.position = camera.position;
            cameraRotationControl.rotation = camera.rotation;

            //starting of the loop that controls the camera until spectating == turned off or we change scenes
            while (cameraIsBeingControlled && (selectedPlayer1 != null) && (selectedPlayer2 != null) && (camera != null) && (cameraRotationControl != null))
            {
                //persistent variable updates
                localDistance = Vector3.Distance(camera.position, selectedPlayer1.position);
                remoteDistance = Vector3.Distance(camera.position, selectedPlayer2.position);
                playerCenterLerpSpot = remoteDistance / (localDistance + remoteDistance);
                Vector3 rawPlayersCenter = Vector3.Lerp(selectedPlayer2.position, selectedPlayer1.position, playerCenterLerpSpot);
                smoothedPlayersCenter = Vector3.Lerp(smoothedPlayersCenter, rawPlayersCenter, Preferences.PrefPlayerCenterSmoothing.Value * Time.fixedDeltaTime);

                //new variable calculations
                float distanceToPlayersCenter = Vector3.Distance(smoothedPlayersCenter, camera.position);
                float distanceToCenter;
                float baseDistance = Vector3.Distance(selectedPlayer1.position, selectedPlayer2.position) / 2f;
                float scaledIncrease = Preferences.PrefCamPosIncrease.Value / (1f + baseDistance * Preferences.PrefCamPosFalloff.Value);
                float idealCameraDistance = baseDistance + scaledIncrease;
                float allowedHeight = Math.Min(10f, Vector3.Distance(selectedPlayer1.position, selectedPlayer2.position) / Preferences.PrefAllowedHeightScaler.Value);

                //moves camera forwards/backwards as needed smoothly
                if (Mathf.Abs(distanceToPlayersCenter - idealCameraDistance) > Preferences.PrefCamPosBuffer.Value)
                {
                    Vector3 dirFromCenter = (cameraRotationControl.position - smoothedPlayersCenter).normalized;
                    cameraRotationControl.position = Vector3.Lerp(cameraRotationControl.position, smoothedPlayersCenter + dirFromCenter * idealCameraDistance, Time.fixedDeltaTime * Preferences.PrefCameraMoveSpeed.Value);
                }

                //move up/down as needed smoothly
                float clampedY = Mathf.Clamp(cameraRotationControl.position.y, smoothedPlayersCenter.y, smoothedPlayersCenter.y + allowedHeight);
                Vector3 clampedYVector = new(cameraRotationControl.position.x, clampedY, cameraRotationControl.position.z);
                cameraRotationControl.position = Vector3.Lerp(cameraRotationControl.position, clampedYVector, Time.fixedDeltaTime * Preferences.PrefCameraMoveSpeed.Value);

                if (Core.isMatchmaking)
                {
                    distanceToCenter = Vector2.Distance(Vector2.zero, new Vector2(cameraRotationControl.position.x, cameraRotationControl.position.z));
                    //move forwards when too far from center in Matchmaking smoothly
                    while ((Vector2.Distance(Vector2.zero, new Vector2(smoothedPlayersCenter.x, smoothedPlayersCenter.z)) < Preferences.PrefMaxCenterDist.Value) && (distanceToCenter > Preferences.PrefMaxCenterDist.Value))
                    {
                        cameraRotationControl.position += cameraRotationControl.forward * Preferences.PrefCamPosCorrection.Value;
                        cameraRotationControl.LookAt(smoothedPlayersCenter);
                        distanceToCenter = Vector2.Distance(Vector2.zero, new Vector2(cameraRotationControl.position.x, cameraRotationControl.position.z));
                    }
                }

                //camera orbit
                if (Vector3.Distance(camera.position, cameraRotationControl.position) < Preferences.PrefCamPosBuffer.Value / Preferences.PrefMaxCenterDist.Value)
                {
                    Vector3 movementAmount = (isOrbitingRight ? 1 : -1) * cameraRotationControl.right * Preferences.PrefCamPosCorrection.Value;
                    Vector3 desiredOrbitPos = cameraRotationControl.position - movementAmount;
                    if (!Core.isMatchmaking || (Vector2.Distance(Vector2.zero, new Vector2(desiredOrbitPos.x, desiredOrbitPos.z)) <= Preferences.PrefMaxCenterDist.Value))
                    {
                        cameraRotationControl.RotateAround(smoothedPlayersCenter, Vector3.up, (isOrbitingRight ? 1f : -1f) * Preferences.PrefCameraOrbitSpeed.Value * Time.fixedDeltaTime);
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
            IsActiveHoloLensNormal = true;
            if (activeHoloLens != null) { GameObject.Destroy(activeHoloLens); }
            if ((cameraRotationControl != null) && (cameraRotationControl.gameObject != null)) { GameObject.Destroy(cameraRotationControl.gameObject); }
            //restore camera if it's still around
            if (camera != null && originalParent != null)
            {
                camera.SetParent(originalParent);
                camera.localPosition = originalLocalPos;
                camera.localRotation = originalLocalRot;
            }
            //return camera to 1st person mode since spectating == done
            if (Preferences.PrefRevertToFps.Value && PlayerManager.instance.localPlayer?.Controller?.PlayerLIV?.LckTablet?.lckCameraController != null) { PlayerManager.instance.localPlayer.Controller.PlayerLIV.LckTablet.lckCameraController.CameraModeChanged(CameraMode.FirstPerson); }
            yield break;
		}
	}
}
