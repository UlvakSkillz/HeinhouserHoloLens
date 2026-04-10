using HeinhouserHoloLens;
using MelonLoader;
using BuildInfo = HeinhouserHoloLens.BuildInfo;

[assembly: MelonInfo(typeof(HeinhouserHoloLens.Core), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 195, 0, 255)]
[assembly: MelonAuthorColor(255, 195, 0, 255)]
[assembly: VerifyLoaderVersion(0, 7, 2, true)]

