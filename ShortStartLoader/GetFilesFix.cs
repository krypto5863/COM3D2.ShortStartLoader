using HarmonyLib;
using System.IO;
using System.Linq;

namespace ShortStartLoader
{
	internal static class GetFilesFix
	{
		[HarmonyPatch(typeof(Directory), "GetFiles", typeof(string), typeof(string), typeof(SearchOption))]
		[HarmonyPrefix]
		public static bool DirGetFilesCache(ref string __0, ref string __1, ref SearchOption searchOption, ref string[] __result)
		{
			if (__1.Equals("*"))
			{
				return true;
			}

			var reg = WildcardToRegex.WildcardToRegexp("^" + __1 + "$");

			__result = Directory.GetFiles(__0, "*", searchOption).Where(path => reg.IsMatch(Path.GetFileName(path))).ToArray();
#if DEBUG
			ShortStartLoader.PluginLogger.LogDebug($"Returning: {__result.Count()} files... Found with reg {reg} converted from wildcard {__1}.");
#endif

			return false;
		}
	}
}