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
	[BepInPlugin("ShortStartLoader", "ShortStartLoader", "1.3.2")]
	[BepInDependency("BepInEx.SybarisLoader.Patcher", BepInDependency.DependencyFlags.SoftDependency)]
	public class ShortStartLoader : BaseUnityPlugin
	{
		public static ShortStartLoader Instance { get; private set; }
		public static ManualLogSource PluginLogger => Instance.Logger;

		public static ConfigEntry<bool> MultiThreadStartup { get; private set; }

		//public static ConfigEntry<bool> UseNewMethod { get; private set; }
		public static ConfigEntry<bool> GetFileFilterFix { get; private set; }

		public static ConfigEntry<bool> FileOpOptimize { get; private set; }

		private void Awake()
		{
			Instance = this;
			MultiThreadStartup = Config.Bind("General", "Multi-thread startup", true, "Can increase initial load times of the game, but could be unstable. If you experience crashes at startup, disable this.");
			//UseNewMethod = Config.Bind("General", "Use New Method", true, "Uses a new method that further incorporates optimization from the built in WSQO. Confers a nice speed boost but stability is unsure.");
			GetFileFilterFix = Config.Bind("General", "Fix GetFiles (Restart Required)", true, "Fixes an issue with GetFiles where using any search pattern would be significantly slower than fetching every file and filtering.");
			FileOpOptimize = Config.Bind("General", "Optimize File Operations", true, "Same functionality as ModMenuAccel or WSQO. It speeds up various file operations that were slow and had room for improvement.");

			var harmony = Harmony.CreateAndPatchAll(typeof(ShortStartLoader));

			if (FileOpOptimize.Value)
			{
				harmony.PatchAll(typeof(FileOpOptimize));
			}
			if (GetFileFilterFix.Value)
			{
				harmony.PatchAll(typeof(GetFilesFix));
			}
			if (MultiThreadStartup.Value)
			{
				//harmony.PatchAll(typeof(MultiThreadingFixes));
				harmony.PatchAll(typeof(StartupOptimize));
			}
		}
	}
}