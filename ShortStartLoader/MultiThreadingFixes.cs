using HarmonyLib;

namespace ShortStartLoader
{
	public class MultiThreadingFixes
	{
		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetList))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetList))]
		[HarmonyPrefix]
		public static void MultiThreadLocker(ref FileSystemWindows __instance)
		{
			System.Threading.Monitor.TryEnter(__instance);
		}

		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetNameMapLastArchive))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetList))]
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetList))]
		[HarmonyFinalizer]
		public static void MultiThreadUnlocker(ref FileSystemWindows __instance)
		{
			System.Threading.Monitor.Exit(__instance);
		}
	}
}