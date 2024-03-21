using HarmonyLib;

namespace ShortStartLoader
{
	public class MultiThreadingFixes
	{
		[HarmonyPatch(typeof(FileSystemArchive))]
		[HarmonyPatch(typeof(FileSystemWindows))]
		[HarmonyPatch(nameof(AFileSystemBase.GetList))]
		[HarmonyPatch(nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyPrefix]
		public static void MultiThreadLocker(ref FileSystemWindows __instance)
		{
			System.Threading.Monitor.TryEnter(__instance);
		}

		[HarmonyPatch(typeof(FileSystemArchive))]
		[HarmonyPatch(typeof(FileSystemWindows))]
		[HarmonyPatch(nameof(AFileSystemBase.GetList))]
		[HarmonyPatch(nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyFinalizer]
		public static void MultiThreadUnlocker(ref FileSystemWindows __instance)
		{
			System.Threading.Monitor.Exit(__instance);
		}
	}
}