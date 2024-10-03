using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShortStartLoader
{
	public class FileOpOptimize
	{
		public static readonly string Com3d2Path = Paths.GameRootPath;
		public static readonly string ModPath = Com3d2Path + "\\Mod";
		private static IEnumerable<string> _filePathInfoList;

		// the credit for the whole thing goes to はてな (twitter @hatena_37 )
		//I further rewrote this and made it more simple for BepInEx.

		//Speeds up fetching of .menu files from arcs by using a smarter heuristic. Only applies to menu files as they're the common file.

		//Release handlers as it seems they remain open in the normal implementation causing a slow down.

		//The original function has been deemed pretty much useless with the function of the above.
		[HarmonyPatch(typeof(FileSystemArchive), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPrefix]
		public static bool FileSystemArchiveGetFileListAtExtension(ref string[] __result, ref string __0)
		{
			GameUty.FileSystem.GetList("", AFileSystemBase.ListType.TopFolder);

			__result = null;
			if (__0 == ".menu")
			{
				__result = GameUty.FileSystem
					.GetList("menu", AFileSystemBase.ListType.AllFile)
					.Concat(
						Array.FindAll(GameUty.FileSystem.GetList("parts", AFileSystemBase.ListType.AllFile), i => i.EndsWith(".menu", StringComparison.OrdinalIgnoreCase))
					)
					.ToArray();
			}
			return __result == null;
		}

		[HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.AddFolder))]
		[HarmonyPostfix]
		public static void FileSystemWindowsAddFolderPost(ref FileSystemWindows __instance) //inject at start; pass invoke
		{
			__instance.AddAutoPathForAllFolder(true);
			while (!__instance.IsFinishedAddAutoPathJob(true))
			{
			}
			__instance.ReleaseAddAutoPathJob();
		}

		[HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.AddAutoPath))]
		[HarmonyPrefix]
		public static bool FileSystemWindowsAddAutoPath(ref bool __result) // inject at start; modify return
		{
			__result = true;
			return false;
		}

		[HarmonyPatch(typeof(FileSystemWindows), nameof(AFileSystemBase.GetFileListAtExtension))]
		[HarmonyPrefix]
		public static bool FileSystemWindowsGetFileListAtExtension(ref string[] __result, ref string __0) // inject at start; pass parameters; modify return
		{
			if (_filePathInfoList == null)
			{
				_filePathInfoList = Directory.GetFiles(ModPath, "*", SearchOption.AllDirectories)
					.Select(x => Path.GetFileName(x).ToLower());
			}

			var loc0 = __0;

			__result = _filePathInfoList
				.Where(x => x.EndsWith(loc0, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			return false;
		}

		[HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.AddFolder))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.AddAutoPath))]
		[HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.AddAutoPathForAllFolder))]
		[HarmonyPrefix]
		public static void ResetCache()
		{
			_filePathInfoList = null;
		}
	}
}