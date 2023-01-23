using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShortStartLoader
{
	[BepInPlugin("ShortStartLoader", "ShortStartLoader", "1.2")]
	[BepInDependency("BepInEx.SybarisLoader.Patcher", BepInDependency.DependencyFlags.SoftDependency)]
	public class SslMain : BaseUnityPlugin
	{
		public static ManualLogSource PubLogger;
		public static ConfigEntry<bool> UseNewMethod;
		public static ConfigEntry<bool> GetFileFilterFix;
		public static ConfigEntry<bool> FileOpOptimize;
		private static Harmony _harmony;

		private void Awake()
		{
			PubLogger = Logger;

			UseNewMethod = Config.Bind("General", "Use New Method", true, "Uses a new method that further incorporates optimization from the built in WSQO. Confers a nice speed boost but stability is unsure.");
			GetFileFilterFix = Config.Bind("General", "Fix GetFiles (Restart Required)", true, "Fixes an issue with GetFiles where using any search pattern would be significantly slower than fetching every file and filtering.");
			FileOpOptimize = Config.Bind("General", "Optimize File Operations", true, "Same functionality as ModMenuAccel or WSQO. It speeds up various file operations that were slow and had room for improvement.");

			_harmony = Harmony.CreateAndPatchAll(typeof(StartupOptimize));
			if (FileOpOptimize.Value)
			{
				_harmony.PatchAll(typeof(FileOpOptimize));
			}

			if (GetFileFilterFix.Value)
			{
				_harmony.PatchAll(typeof(GetFilesFix));
			}
		}
	}
}