using HarmonyLib;
using I2.Loc;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ShortStartLoader
{
	public static class StartupOptimize
	{
		//private static Task _legacyLoadTask;
		private static Task _backgroundLoadTask;
		private static bool _normalArcLoaderDone;

		
		public static void WaitForLegacy()
		{
			_normalArcLoaderDone = true;

			if (!_backgroundLoadTask.IsCompleted)
			{
				//var backgroundStopWatch = new Stopwatch();
				//backgroundStopWatch.Start();

				//ShortStartLoader.PluginLogger.LogInfo("■■■■■■■■ Waiting for the background load thread to finish...");

				while (!_backgroundLoadTask.IsCompleted)
				{
				}

				//backgroundStopWatch.Stop();

				//ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Background load thread was awaited for: {backgroundStopWatch.Elapsed}");
			}

			if (_backgroundLoadTask.IsFaulted)
			{
				ShortStartLoader.PluginLogger.LogFatal("The background load thread encountered a fatal error!");

				if (_backgroundLoadTask.Exception?.InnerException != null)
				{
					throw _backgroundLoadTask.Exception.InnerException;
				}
			}

			_backgroundLoadTask.Dispose();
			_backgroundLoadTask = null;
		}

		[HarmonyPatch(typeof(GameUty), nameof(GameUty.Init))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
		{
			return new CodeMatcher(instructions)
				.MatchForward(false,
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GameUty), "UpdateFileSystemPathOld"))
				)
				.Set(OpCodes.Call, AccessTools.Method(typeof(StartupOptimize), nameof(WaitForLegacy)))
				.InstructionEnumeration();
		}
		
		/*
		[HarmonyPatch(typeof(GameUty), nameof(GameUty.Init))]
		[HarmonyPostfix]
		private static void WaitForThread()
		{
			if (!_backgroundLoadTask.IsCompleted)
			{
				var modStopWatch = new Stopwatch();
				modStopWatch.Start();

				ShortStartLoader.PluginLogger.LogInfo("■■■■■■■■ Waiting for Mods to finish loading...");

				while (!_backgroundLoadTask.IsCompleted)
				{
				}

				modStopWatch.Stop();

				ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Mods have finished loading: {modStopWatch.Elapsed}");
			}

			if (_backgroundLoadTask.IsFaulted)
			{
				ShortStartLoader.PluginLogger.LogFatal("The mod loader thread encountered a fatal error!");

				if (_backgroundLoadTask.Exception?.InnerException != null)
				{
					throw _backgroundLoadTask.Exception.InnerException;
				}
			}

			_backgroundLoadTask.Dispose();
			_backgroundLoadTask = null;
		}
		*/

		[HarmonyPatch(typeof(GameUty), nameof(GameUty.UpdateFileSystemPath))]
		[HarmonyPrefix]
		private static bool UpdateFileSystemPath()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			#region MethodLocalDeclarations

			Product.Initialize();
			var gamePath = UTY.gameProjectPath + "\\";
			var gameDataPath = "GameData";
			const string missingArc = "Missing Arc File (必用アーカイブがありません。) GameData\\";
			const int checkVerNo = 3;

			#endregion MethodLocalDeclarations

			#region InlineFunctions

			bool AddFolderOrArchive(string arcFile, string gameDataPathArg)
			{
				NDebug.Assert(arcFile != "parts_cas", "CC危険。" + arcFile);

				if (!GameUty.m_FileSystem.AddArchive(gameDataPathArg + "\\" + arcFile + ".arc"))
				{
					return false;
				}

				GameUty.loadArchiveList.Add(arcFile.ToLower());

				return true;
			}

			var addedLegacyArchives = new HashSet<string>();

			void AddLegacyArchive(string prefix)
			{
				foreach (var arcNames in GameUty.PathList)
				{
					var arcFile = prefix + "_" + arcNames;
					var arcFileAdded = AddFolderOrArchive(arcFile, gameDataPath);

					if (arcFileAdded && !addedLegacyArchives.Contains(arcFile))
					{
						addedLegacyArchives.Add(arcFile);
					}

					if (!arcFileAdded)
					{
						continue;
					}

					if (prefix.Equals("csv"))
					{
						GameUty.ExistCsvPathList.Add(arcNames);
					}

					for (var num12 = 2; num12 <= checkVerNo; num12++)
					{
						AddFolderOrArchive(arcFile + "_" + num12, gameDataPath);
					}
				}
			}

			void LoadAllArcOfPrefix(string prefix)
			{
				foreach (var arcNames in GameUty.PathList)
				{
					var arcFile = prefix + "_" + arcNames;
					var arcWasAdded = AddFolderOrArchive(arcFile, gameDataPath);

					if (!arcWasAdded && addedLegacyArchives.Contains(arcFile))
					{
						arcWasAdded = true;
					}

					if (!arcWasAdded)
					{
						continue;
					}

					if (prefix.Equals("csv"))
					{
						GameUty.ExistCsvPathList.Add(arcNames);
					}

					for (var num12 = 2; num12 <= checkVerNo; num12++)
					{
						AddFolderOrArchive(string.Concat(prefix, "_", arcNames, "_", num12), gameDataPath);
					}
				}
			}

			void LoadAllArcOfPrefix2(string prefix)
			{
				foreach (var arcName in GameUty.PathList)
				{
					var arcWasLoaded = AddFolderOrArchive(prefix + "_" + arcName, gameDataPath);

					if (!arcWasLoaded)
					{
						continue;
					}

					for (var l = 2; l <= checkVerNo; l++)
					{
						AddFolderOrArchive(string.Concat(prefix, "_", arcName, "_", l), gameDataPath);
					}
				}
			}

			#endregion InlineFunctions

			//GameUty.m_FileSystem.SetBaseDirectory(gamePath);
			//AddFolderOrArchive("product");
			//Product.Initialize(GameUty.m_FileSystem);

			_backgroundLoadTask = Task.Factory.StartNew(delegate
			{
				var stopwatch1 = new Stopwatch();
				stopwatch1.Start();

				//ShortStartLoader.PluginLogger.LogInfo("■■■■■■■■ SSL's Mod Loading Thread has begun...");

				if (Directory.Exists(gamePath + "Mod"))
				{
					GameUty.m_ModFileSystem = new FileSystemWindows();
					GameUty.m_ModFileSystem.SetBaseDirectory(gamePath);
					GameUty.m_ModFileSystem.AddFolder("Mod");

					var listOfDirsInModFolder =
							GameUty.m_ModFileSystem.GetList(string.Empty, AFileSystemBase.ListType.AllFolder);

					ShortStartLoader.PluginLogger.LogDebug($"{listOfDirsInModFolder.Count()} have been found in the Mod Folder.");

					//Supposedly it's useless since we're using AddFolder above. but maybe it isn't.
					for (var c = 0; c < listOfDirsInModFolder.Length; c++)
					{
						if (!GameUty.m_ModFileSystem.AddAutoPath(listOfDirsInModFolder[c]))
						{
							Debug.Log("m_ModFileSystemのAddAutoPathには既に " + listOfDirsInModFolder[c] + " がありました。");
						}
					}

					/* Modified original method. Commented and left for future reference or usage.
					var listOfMenusInModFolder =
						GameUty.m_ModFileSystem.GetList(string.Empty, AFileSystemBase.ListType.AllFile);
					GameUty.m_aryModOnlysMenuFiles = listOfMenusInModFolder.Where(strFile => strFile.EndsWith(".menu", StringComparison.OrdinalIgnoreCase)).ToArray();
					*/

					GameUty.m_aryModOnlysMenuFiles = GameUty.m_ModFileSystem?.GetFileListAtExtension(".menu") ?? new string[0];
					for (var r = 0; r < GameUty.m_aryModOnlysMenuFiles.Length; r++)
					{
#if DEBUG
						ShortStartLoader.PluginLogger.LogDebug($"Getting file name of {GameUty.m_aryModOnlysMenuFiles[r]}");
#endif
						GameUty.m_aryModOnlysMenuFiles[r] = Path.GetFileName(GameUty.m_aryModOnlysMenuFiles[r]);
					}

					if (GameUty.m_aryModOnlysMenuFiles?.Length != 0)
					{
						GameUty.ModPriorityToModFolderInfo = string.Empty;
						Debug.Log(GameUty.ModPriorityToModFolderInfo + "■MOD有り。MODフォルダ優先モード" + GameUty.ModPriorityToModFolder);
					}
				}

				ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading mods @ {stopwatch1.Elapsed}");
			})
				.ContinueWith(delegate
			{
				//var stopWatchLoc = new Stopwatch();
				//stopWatchLoc.Start();

				//ShortStartLoader.PluginLogger.LogInfo("■■■■■■■■ SSL's FileSystemOld Loading Thread has begun.");

				GameUty.UpdateFileSystemPathOld();

				//ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading legacy arcs {stopWatchLoc.Elapsed}");
			});

			/* Informative, off for now.
			UnityEngine.Debug.Log("IsEnabledCompatibilityMode:" + GameUty.IsEnabledCompatibilityMode.ToString());
			string gameTitle = Product.gameTitle;
			string CMStr = "Custom Maid 3D 2 (カスタムメイド3D 2)";
			UnityEngine.Debug.Log(string.Concat(new string[]
			{
				gameTitle,
				" GameVersion ",
				GameUty.GetGameVersionText(),
				"(BuildVersion : ",
				GameUty.GetBuildVersionText(),
				")"
			}));

			if (!string.IsNullOrEmpty(GameMain.Instance.CMSystem.CM3D2Path))
			{
				UnityEngine.Debug.Log(CMStr + " GameVersion " + GameUty.GetLegacyGameVersionText());
			}
			*/

			//If type isn't JP adult.
			if (Product.type > Product.Type.JpAdult)
			{
				GameUty.UpdateFileSystemPathToNewProduct();
			}
			else
			{
				#region CM Load

				if (GameUty.IsEnabledCompatibilityMode)
				{
					//UnityEngine.Debug.Log("■■■■■■■■ Archive Log[2.0] (CM3D2 GameData)");

					GameUty.m_FileSystem.SetBaseDirectory(GameMain.Instance.CMSystem.CM3D2Path);
					GameUty.PathList = GameUty.PathListOld;

					AddFolderOrArchive("material", gameDataPath);
					foreach (var arcName in GameUty.PathListOld)
					{
						const string prefix = "material";
						if (arcName.Equals("denkigai2015wTowelR"))
						{
							AddFolderOrArchive(prefix + "_denkigai2015wTowel", gameDataPath);
						}

						var arcFile = prefix + "_" + arcName;
						var arcFileLoaded = AddFolderOrArchive(arcFile, gameDataPath);

						if (arcFileLoaded && !addedLegacyArchives.Contains(arcFile))
						{
							addedLegacyArchives.Add(arcFile);
						}

						if (arcFileLoaded)
						{
							int i;
							for (i = 2; i <= checkVerNo; i++)
							{
								AddFolderOrArchive(arcFile + "_" + i, gameDataPath);
							}
						}
					}

					AddFolderOrArchive("material2", gameDataPath);
					AddFolderOrArchive("menu", gameDataPath);
					AddLegacyArchive("menu");
					AddFolderOrArchive("menu2", gameDataPath);
					AddFolderOrArchive("model", gameDataPath);
					AddLegacyArchive("model");
					AddFolderOrArchive("model2", gameDataPath);
					AddFolderOrArchive("texture", gameDataPath);
					AddLegacyArchive("texture");
					AddFolderOrArchive("texture2", gameDataPath);
					AddFolderOrArchive("texture3", gameDataPath);
					AddFolderOrArchive("prioritymaterial", gameDataPath);
					ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading CM3D2 arc files @ {stopwatch.Elapsed}");
				}

				#endregion CM Load

				#region CM Local Load

				//UnityEngine.Debug.Log("■■■■■■■■ Archive Log[2.1 Compatibility] (GameData_20)");
				gameDataPath = "GameData_20";
				GameUty.m_FileSystem.SetBaseDirectory(gamePath);

				if (GameUty.IsEnabledCompatibilityMode)
				{
					GameUty.m_FileSystem.AddPatchDecryptPreferredSearchDirectory(GameMain.Instance.CMSystem.CM3D2Path + "\\GameData");
				}

				GameUty.PathList = GameUty.ReadAutoPathFile("[2.1 Compatibility]", gamePath + gameDataPath + "\\paths.dat");
				if (GameUty.PathList != null && 0 < GameUty.PathList.Count)
				{
					foreach (var arcName in GameUty.PathList)
					{
						const string prefix = "material";
						if (arcName.Equals("denkigai2015wTowelR"))
						{
							AddFolderOrArchive(prefix + "_denkigai2015wTowel", gameDataPath);
						}

						var arcFile = prefix + "_" + arcName;
						var arcWasLoaded = AddFolderOrArchive(arcFile, gameDataPath);

						if (!arcWasLoaded && addedLegacyArchives.Contains(arcFile))
						{
							arcWasLoaded = true;
						}

						if (arcWasLoaded)
						{
							for (var j = 2; j <= checkVerNo; j++)
							{
								AddFolderOrArchive(string.Concat(prefix, "_", arcName, "_", j), gameDataPath);
							}
						}
					}

					LoadAllArcOfPrefix("menu");
					LoadAllArcOfPrefix("model");
					LoadAllArcOfPrefix("texture");
					AddFolderOrArchive("prioritymaterial", gameDataPath);
					var pathList = GameUty.PathList;
					GameUty.PathList = new List<string> { "vp001" };
					LoadAllArcOfPrefix("bg");
					LoadAllArcOfPrefix("motion");
					GameUty.PathList = pathList;
				}
				GameUty.m_FileSystem.ClearPatchDecryptPreferredSearchDirectory();
				ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading legacy archives installed to COM @ {stopwatch.Elapsed}");

				//UnityEngine.Debug.Log("■■■■■■■■ Archive Log[2.1] (GameData)");
				gameDataPath = "GameData";
				GameUty.PathList = GameUty.ReadAutoPathFile("[2.1]", gamePath + gameDataPath + "\\paths.dat");

				if (GameUty.PathList == null)
				{
					GameUty.PathList = new List<string>();
					NDebug.Assert("paths.datを読み込めませんでした");
				}

				GameUty.PathList.Add("jp");

				AddFolderOrArchive("csv", gameDataPath);
				LoadAllArcOfPrefix2("csv");

				AddFolderOrArchive("prioritymaterial", gameDataPath);

				NDebug.Assert(AddFolderOrArchive("motion", gameDataPath), missingArc + "motion");
				LoadAllArcOfPrefix2("motion");

				AddFolderOrArchive("motion2", gameDataPath);

				NDebug.Assert(AddFolderOrArchive("script", gameDataPath), missingArc + "script");
				LoadAllArcOfPrefix2("script");

				AddFolderOrArchive("script_share", gameDataPath);
				LoadAllArcOfPrefix2("script_share");

				AddFolderOrArchive("script_share2", gameDataPath);
				NDebug.Assert(AddFolderOrArchive("sound", gameDataPath), missingArc + "sound");
				LoadAllArcOfPrefix2("sound");

				AddFolderOrArchive("sound2", gameDataPath);

				NDebug.Assert(AddFolderOrArchive("system", gameDataPath), missingArc + "system");
				LoadAllArcOfPrefix2("system");

				AddFolderOrArchive("system2", gameDataPath);

				AddFolderOrArchive("language", gameDataPath);
				LoadAllArcOfPrefix2("language");

				LoadAllArcOfPrefix2("bg");

				if (Product.isEnglish && !Product.isPublic)
				{
					NDebug.Assert(AddFolderOrArchive("bg-en", gameDataPath), missingArc + "bg-en");

					LoadAllArcOfPrefix2("bg-en");

					AddFolderOrArchive("bg-en2", gameDataPath);
				}

				AddFolderOrArchive("voice", gameDataPath);
				for (var num6 = 0; num6 < 25; num6++)
				{
					const string str4 = "voice";
					var arg = str4 + "_" + ((char)(97 + num6));
					AddFolderOrArchive(arg, gameDataPath);
				}

				LoadAllArcOfPrefix2("voice");

				for (var num8 = 2; num8 <= checkVerNo; num8++)
				{
					const string str5 = "voice";
					AddFolderOrArchive(str5 + num8, gameDataPath);
				}

				NDebug.Assert(AddFolderOrArchive("parts", gameDataPath), missingArc + "parts");
				LoadAllArcOfPrefix2("parts");
				AddFolderOrArchive("parts2", gameDataPath);

				ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading arcs @ {stopwatch.Elapsed}");

				#endregion CM Local Load
			}

			//UnityEngine.Debug.Log("■■■■■■■■ Final Processing...");

			GameUty.m_FileSystem.AddAutoPathForAllFolder(true);

			while (!GameUty.m_FileSystem.IsFinishedAddAutoPathJob(true))
			{
				//Thread.Sleep(100);
			}

			GameUty.m_FileSystem.ReleaseAddAutoPathJob();

			if (Product.isPublic && !GameUty.m_FileSystem.IsExistentFile("21C399027026.dat"))
			{
				NDebug.MessageBox("Error", Product.type + " : 21C399027026.dat");
				Application.Quit();
			}
			else
			{
				GameUty.BgFiles = new Dictionary<string, AFileSystemBase>();
				var bgArcs = GameUty.m_FileSystem.GetList("bg", AFileSystemBase.ListType.AllFile);

				if (bgArcs != null && bgArcs.Length != 0)
				{
					foreach (var path in bgArcs)
					{
#if DEBUG
						ShortStartLoader.PluginLogger.LogDebug($"Getting file name of {path}");
#endif
						var fileName = Path.GetFileName(path);
						var flag27 = Path.GetExtension(fileName) == ".asset_bg" && !GameUty.BgFiles.ContainsKey(fileName);
						if (flag27)
						{
							GameUty.BgFiles.Add(fileName, GameUty.m_FileSystem);
						}
					}
				}

				if (Product.supportMultiLanguage)
				{
					bgArcs = GameUty.m_FileSystem.GetList("language", AFileSystemBase.ListType.AllFile);

					if (bgArcs != null && bgArcs.Length != 0)
					{
						foreach (var arcPath in bgArcs)
						{
#if DEBUG
							ShortStartLoader.PluginLogger.LogDebug($"Getting file name of {arcPath}");
#endif
							var file = Path.GetFileName(arcPath);
							if (!Path.GetExtension(file).Equals(".asset_language"))
							{
								continue;
							}

							using (var aFileBase = GameUty.m_FileSystem.FileOpen(file))
							{
								var assetBundle = AssetBundle.LoadFromMemory(aFileBase.ReadAll());
								var languageSource = Object.Instantiate(assetBundle.LoadAllAssets<GameObject>()[0].GetComponent<LanguageSource>());
								var flag30 = GameMain.Instance.transform.Find("Language") == null;
								if (flag30)
								{
									new GameObject("Language").transform.SetParent(GameMain.Instance.transform);
								}
								languageSource.transform.SetParent(GameMain.Instance.transform.Find("Language"));
								assetBundle.Unload(true);
							}
						}
					}
					foreach (var languageSource2 in LocalizationManager.Sources)
					{
						languageSource2.LoadAllLanguages();
					}
				}
			}

			ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Nearly done @ {stopwatch.Elapsed}");

			return false;
		}

		[HarmonyPatch(typeof(GameUty), nameof(GameUty.UpdateFileSystemPathOld))]
		[HarmonyPrefix]
		public static bool UpdateFileSystemPathOld()
		{
			if (!GameUty.IsEnabledCompatibilityMode)
			{
				return false;
			}

			var stopwatch = new Stopwatch();

			const int checkVerNo = 3;
			var fileSystem = GameUty.m_FileSystemOld;

			stopwatch.Start();

			//ShortStartLoader.PluginLogger.LogInfo("■■■■■■■■ Archive Log[Legacy]");

			bool AddFolderOrArchive(string name)
			{
				return fileSystem.AddArchive("GameData\\" + name + ".arc");
				//UnityEngine.Debug.Log("[GameData\\" + name + ".arc]を読み込みました");
			}

			void Action(string prefix)
			{
				foreach (var file in GameUty.PathListOld)
				{
					if (!AddFolderOrArchive(prefix + "_" + file))
					{
						continue;
					}

					if (prefix == "csv")
					{
						GameUty.ExistCsvPathListOld.Add(file);
					}

					for (var m = 2; m <= checkVerNo; m++)
					{
						AddFolderOrArchive(string.Concat(prefix, "_", file, "_", m));
					}
				}
			}

			fileSystem.SetBaseDirectory(GameMain.Instance.CMSystem.CM3D2Path);
			AddFolderOrArchive("csv");
			Action("csv");
			AddFolderOrArchive("motion");
			Action("motion");
			AddFolderOrArchive("motion2");
			AddFolderOrArchive("script");
			Action("script");
			Action("script_share");
			AddFolderOrArchive("script_share2");
			AddFolderOrArchive("sound");
			Action("sound");
			AddFolderOrArchive("sound2");
			AddFolderOrArchive("texture");
			Action("texture");
			AddFolderOrArchive("texture2");
			AddFolderOrArchive("texture3");
			AddFolderOrArchive("system");
			Action("system");
			Action("bg");
			AddFolderOrArchive("voice");
			AddFolderOrArchive("voice_a");
			AddFolderOrArchive("voice_b");
			AddFolderOrArchive("voice_c");

			foreach (var str in GameUty.PathListOld)
			{
				const string str2 = "voice";
				var text = str2 + "_" + str;
				if (AddFolderOrArchive(text))
				{
					for (var i = 2; i <= checkVerNo; i++)
					{
						AddFolderOrArchive(text + "_" + i);
					}
				}
				text = str2 + "_" + str + "a";
				if (AddFolderOrArchive(text))
				{
					for (var j = 2; j <= checkVerNo; j++)
					{
						AddFolderOrArchive(text + "_" + j);
					}
				}
				text = str2 + "_" + str + "b";
				if (AddFolderOrArchive(text))
				{
					for (var k = 2; k <= checkVerNo; k++)
					{
						AddFolderOrArchive(text + "_" + k);
					}
				}
			}

			AddFolderOrArchive("voice2");
			AddFolderOrArchive("voice3");

			fileSystem.AddAutoPathForAllFolder(true);
			while (!fileSystem.IsFinishedAddAutoPathJob(true))
			{
			}
			fileSystem.ReleaseAddAutoPathJob();

			var bgFiles = fileSystem.GetList("bg", AFileSystemBase.ListType.AllFile);

			if (bgFiles != null && 0 < bgFiles.Length)
			{
				while (_normalArcLoaderDone == false)
				{
				}

				foreach (var path in bgFiles)
				{
#if DEBUG
					ShortStartLoader.PluginLogger.LogDebug($"Getting file name of {path}");
#endif
					var fileName = Path.GetFileName(path);
					if (Path.GetExtension(fileName) == ".asset_bg" && !GameUty.BgFiles.ContainsKey(fileName))
					{
						GameUty.BgFiles.Add(fileName, fileSystem);
					}
				}
			}

			ShortStartLoader.PluginLogger.LogInfo($"■■■■■■■■ Done loading legacy files @ {stopwatch.Elapsed}");
			stopwatch.Stop();

			return false;
		}
	}
}