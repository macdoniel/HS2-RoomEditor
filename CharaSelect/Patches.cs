using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using HarmonyLib;

using UnityEngine;
using Object = UnityEngine.Object;
using Logger = UnityEngine.Debug;

using Instructions = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;

namespace CharaSelect
{
  using CharaCustom;
  class PatchListUI
  {

    [HarmonyPostfix, HarmonyPatch(typeof(CustomClothesWindow), "Start")]
    static void Start(CustomClothesWindow __instance)
    {
      Helpers.UI.ModifyGameMenu(__instance, "coordinate/female", () => 
      {
        Logger.Log("Action running!");
        // TODO last parameter is false on load, true on save
        __instance.UpdateWindow(true, 1, true);
      });
    }

    [HarmonyPostfix, HarmonyPatch(typeof(CustomCharaWindow), "Start")]
    static void StartChara(CustomCharaWindow __instance)
    {
      Helpers.UI.ModifyGameMenu(__instance, "chara/female", () =>
      {
        Logger.Log("Chara Action running!!");
        __instance.UpdateWindow(true, 1, true);
      });
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CustomClothesWindow), "UpdateWindow")]
    static bool Prefix(CustomClothesWindow __instance, bool modeNew, int sex, bool save)
    {
      Type t = __instance.GetType();
      FieldInfo info = t.GetField("lstClothes", BindingFlags.Instance | BindingFlags.NonPublic);
      List<CustomClothesFileInfo> list = new List<CustomClothesFileInfo>();

      CharaFolderUI plugin = __instance.gameObject.GetComponent<CharaFolderUI>();
      string path = plugin.currentDir;
      int num = 0;
      Type atype = typeof(CustomClothesFileInfoAssist);
      MethodInfo minfo = atype.GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);

      Logger.Log("CharaSelect :: TRYING TO INVOKE STATIC METHOD! CLOTHES");
      byte sexb = (byte)sex;
      minfo.Invoke(null, new object[] { list, path, sexb, !save, num });
      info.SetValue(__instance, list);
      __instance.Sort();
      return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaWindow), "UpdateWindow")]
    static bool PrefixChara(CustomCharaWindow __instance, bool modeNew, int sex, bool save)
    {
      Type t = __instance.GetType();
      FieldInfo info = t.GetField("lstChara", BindingFlags.Instance | BindingFlags.NonPublic);
      List<CustomCharaFileInfo> list = new List<CustomCharaFileInfo>();
      CharaFolderUI plugin = __instance.gameObject.GetComponent<CharaFolderUI>();
      string path = plugin.currentDir;

      int num = 0;
      Type atype = typeof(CustomCharaFileInfoAssist);
      MethodInfo minfo = atype.GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);
      Logger.Log("CharaSelect :: TRYING TO INVOKE STATIC METHOD! CHARA");
      byte sexb = (byte)sex;
      minfo.Invoke(null, new object[] { list, path, sexb, !save, true, true, false, num });

      Logger.Log("CharaSelect :: Length of list is " + list.Count);

      info.SetValue(__instance, list);
      __instance.Sort();
      return false;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(AIChara.ChaFileCoordinate), "SaveFile")]
    static Instructions SaveCoordFileToDirectory(Instructions instructions)
    {
      /** Ignore path trimming when saving coordinates */
      string s = "call System.String System.IO.Path::GetFileName(System.String)";
      return Helpers.Replace(instructions, s, (instruction) => {
        return new List<CodeInstruction>{
          new CodeInstruction(OpCodes.Nop),
        };
      });
    }

    [HarmonyPrefix, HarmonyPatch(typeof(AIChara.ChaFileControl), "SaveCharaFile", new Type[] { typeof(string), typeof(byte), typeof(bool) })]
    static bool PrefixFileSave(AIChara.ChaFileControl __instance, ref string filename, byte sex = 255, bool newFile = false)
    {
      Object[] controls = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(CvsO_CharaSave));
      if (controls.Length == 0)
      {
        /** No UI component, proceed as normal */
        return true; 
      }
      else
      {
        /** Found UI component, read the current directory and save there */
        CharaFolderUI ui = ((CvsO_CharaSave)controls[0]).gameObject.GetComponent<CharaFolderUI>();
        filename = ui.currentDir + "\\" + filename;
        return true;
      }
    }
  }

  class PatchHSceneUI
  {

    [HarmonyPostfix, HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), "Start")]
    public static void Start(HSceneSpriteCoordinatesCard __instance)
    {
      var plugin = __instance.gameObject.GetOrAddComponent<HSceneFolderUI>();
      plugin.Initialize(__instance);
    }

    public static List<CustomCharaFileInfo> InjectedChara(bool useMale, bool useFemale, bool useMyData = true, bool useDownload = true, bool usePreset = true, bool _isFindSaveData = true)
    {
      byte sex = useMale ? (byte)0 : (byte)1;
      int num = 0;

      var result = new List<CustomCharaFileInfo>();
      var path = UserData.Path + ( useMale ?  "chara/male" : "chara/female" );
      DirectoryInfo dir = new DirectoryInfo (path);
      var info = dir.EnumerateDirectories (".", SearchOption.AllDirectories);
      var iList = info.ToList();
      iList.Add(dir);
      var addInfo = typeof(CustomCharaFileInfoAssist).GetMethod("AddList", BindingFlags.NonPublic | BindingFlags.Static);

      foreach (var directory in iList)
      {
        string name = directory.FullName;
        string userPath = name.Substring(name.IndexOf("UserData") + "UserData".Length + 1);
        string relativePath = UserData.Path + userPath;

        addInfo.Invoke(null, new object[] {result, relativePath, sex, useMyData, useDownload, usePreset, _isFindSaveData, num});
      }

      return result;
    }

    public static List<CustomClothesFileInfo> Injected(bool useMale, bool useFemale, bool useMyData, bool usePreset)
    {
      byte sex = useMale ? (byte)0 : (byte)1;
      int num = 0;

      var result = new List<CustomClothesFileInfo>();
      var path = UserData.Path + ( useMale ?  "coordinate/male" : "coordinate/female" );

      DirectoryInfo dir = new DirectoryInfo (path);
      var info = dir.EnumerateDirectories (".", SearchOption.AllDirectories);
      var iList = info.ToList();
      iList.Add(dir);

      var addInfo = typeof(CustomClothesFileInfoAssist).GetMethod("AddList", BindingFlags.NonPublic | BindingFlags.Static);

      foreach (var directory in iList)
      {
        string name = directory.FullName;
        string userPath = name.Substring(name.IndexOf("UserData") + "UserData".Length + 1);
        string relativePath = UserData.Path + userPath;

        addInfo.Invoke(null, new object[] {result, relativePath, sex, usePreset, num});
      }
      return result;
    }

    [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), "Init")]
    static Instructions PatchHSceneCoordReplace(Instructions instructions)
    {
      return Helpers.Replace(instructions, "call System.Collections.Generic.List`1[[CharaCustom.CustomClothesFileInfo, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] CharaCustom.CustomClothesFileInfoAssist::CreateClothesFileInfoList(System.Boolean,System.Boolean,System.Boolean,System.Boolean)", (instruction) => {
        return new List<CodeInstruction>() {
          new CodeInstruction(OpCodes.Call, typeof(PatchHSceneUI).GetMethod(nameof(Injected), BindingFlags.Public | BindingFlags.Static))
        };
      });
    }

    [HarmonyTranspiler, HarmonyAfter(nameof(HS2_HCharaSwitcher)), HarmonyPatch(typeof(HS2_HCharaSwitcher.Tools), "PopulateList")]
    static Instructions PatchHSceneCharaReplace(Instructions instructions)
    {
      Logger.Log("CharaSelect :: Patching :: ");
      foreach (var item in instructions)
      {
        Logger.Log(item);
      }
      Logger.Log("CharaSelect :: Patched :: ");

      return Helpers.Replace(instructions, "call System.Collections.Generic.List`1[[CharaCustom.CustomCharaFileInfo, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] CharaCustom.CustomCharaFileInfoAssist::CreateCharaFileInfoList(System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean,System.Boolean)", (instruction) => {
        return new List<CodeInstruction>() {
          new CodeInstruction(OpCodes.Call, typeof(PatchHSceneUI).GetMethod(nameof(InjectedChara), BindingFlags.Public | BindingFlags.Static))
        };
      });
    }

    // [HarmonyTranspiler, HarmonyPatch(typeof(HSceneSprite), "Init")]
    // static Instructions PatchHSceneCharaReplace(Instructions instructions)
    // {

    //   return instructions;
    // }
  }

    //     [HarmonyPostfix, HarmonyPatch(typeof(HSceneSpriteCoordinatesCard), "ChangeTargetSex")]
    //     public static void Postfix(HSceneSpriteCoordinatesCard __instance, int sex)
    //     {
    //         Logger.Log("Changing target sex!!!");
    //         var plugin = __instance.gameObject.GetOrAddComponent<HSceneFolderUI>();
    //         plugin.DeriveRootDir();
    //         plugin.FilterCoords(); 
    //     }


    // [HarmonyPatch(typeof(HS2.GroupCharaSelectUI))]
    // [HarmonyPatch("ReDrawListView")]
    // /* Filter the character list by the current directory of the attached component. */
    // class PatchCharaRedraw
    // {
    //     public static List<GameLoadCharaFileSystem.GameCharaFileInfo> Injected(List<GameLoadCharaFileSystem.GameCharaFileInfo> list, HS2.GroupCharaSelectUI instance)
    //     {
    //         CharaFolderUI ui = instance.GetComponent<CharaFolderUI>();
    //         if (ui == null)
    //         {
    //             return list;
    //         }
    //         return list.FindAll(info => Directory.GetParent(info.FullPath).FullName.Equals(ui.currentDir));
    //     }

    //     static Instructions Transpiler(Instructions instructions)
    //     {
    //         var i = "ldfld System.Collections.Generic.List`1[GameLoadCharaFileSystem.GameCharaFileInfo] charaLists";
    //         return Helpers.findInstruction(instructions, i, (inst) =>
    //         {
    //             return new List<CodeInstruction>
    //             {
    //                 inst,
    //                 new CodeInstruction(OpCodes.Ldarg_0),
    //                 new CodeInstruction(OpCodes.Call, typeof(PatchCharaRedraw).GetMethod(nameof(Injected), BindingFlags.Public | BindingFlags.Static))
    //             };
    //         });
    //     }
    // }

    // class PatchCoordinateListUI
    // {
    //     [HarmonyTranspiler, HarmonyPatch(typeof(HS2.CoordinateListUI), "InitListSelect")]
    //     static Instructions InitListSelect(Instructions instructions)
    //     {
    //         var i = "callvirt System.Void CoordinateFileSystem.CoordinateFileScrollController::Init";
    //         return Helpers.findInstruction(instructions, i, Helpers.Patches.CoordCallback);
    //     }

    //     [HarmonyTranspiler, HarmonyPatch(typeof(HS2.CoordinateListUI), "ListSelectRelease")]
    //     static Instructions ListSelectRelease(Instructions instructions)
    //     {
    //         var i = "callvirt System.Void CoordinateFileSystem.CoordinateFileScrollController::Init";
    //         return Helpers.findInstruction(instructions, i, Helpers.Patches.CoordCallback);
    //     }

    //     [HarmonyTranspiler, HarmonyPatch(typeof(HS2.CoordinateListUI), "ReDrawListView")]
    //     static Instructions ReDrawListView(Instructions instructions)
    //     {
    //         var i = "callvirt System.Void CoordinateFileSystem.CoordinateFileScrollController::Init";
    //         return Helpers.findInstruction(instructions, i, Helpers.Patches.CoordCallback);
    //     }

    //     [HarmonyPostfix, HarmonyPatch(typeof(CharaCustom.CustomClothesWindow), "Start")]
    //     static void Start(CharaCustom.CustomClothesWindow __instance)
    //     {
    //         Helpers.UI.ModifyGameMenu(__instance, "coordinate/female", () => 
    //         {
    //             __instance.ReDrawListView();
    //         });
    //     }
    // }


    // [HarmonyPatch(typeof(HS2.GroupCharaSelectUI))]
    // [HarmonyPatch("Start")]
    // /* Patch the UI to add subfolder support. */
    // class PatchCharaSelectUI
    // {
    //     static void Postfix(HS2.GroupCharaSelectUI __instance)
    //     {
    //         Helpers.UI.ModifyGameMenu(__instance, "chara/female", () =>
    //         {
    //             __instance.ReDrawListView();
    //         });
    //     }
    // }

    // [HarmonyPatch(typeof(SaveData))]
    // [HarmonyPatch("IsRoomListChara")]
    // /* Remove the call to GetFileNameWithoutExtension, to integrate with the full path. */
    // class PatchSaveDataFind
    // {
    //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         foreach (var instruction in instructions)
    //         {
    //             if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileNameWithoutExtension(System.String)"))
    //             {
    //                 yield return new CodeInstruction(OpCodes.Nop);
    //             }
    //             else
    //             {
    //                 yield return instruction;
    //             }
    //         }
    //     }
    // }

    // [HarmonyPatch(typeof(Manager.LobbySceneManager))]
    // [HarmonyPatch("LoadChara")]
    // /* Replace the naive filename with a query from the root path. */
    // class PatchLobbyLoad
    // {
    //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         foreach (var instruction in instructions)
    //         {
    //             if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileNameWithoutExtension(System.String)"))
    //             {
    //                 yield return new CodeInstruction(OpCodes.Call, typeof(Helpers).GetMethod(nameof(Helpers.PathFromRoot), BindingFlags.Public | BindingFlags.Static));
    //             }
    //             else
    //             {
    //                 yield return instruction;
    //             }
    //         }
    //     }
    // }

    // [HarmonyPatch(typeof(FolderAssist))]
    // [HarmonyPatch("CreateFolderInfoExToArray")]
    // class PatchCharaFinder
    // {
    //     static bool Prefix(ref FolderAssist.FileInfo[] __result, string folder, params string[] searchPattern)
    //     {
    //         if (!Directory.Exists(folder))
    //         {
    //             __result = null;
    //             return false;
    //         }
    //         /** Changed to return files in subdirectories **/
    //         string[] source = searchPattern.SelectMany((string pattern) => Directory.GetFiles(folder, pattern, SearchOption.AllDirectories)).ToArray<string>();
    //         if (!source.Any<string>())
    //         {
    //             __result = null;
    //             return false;
    //         }
    //         /** Changed to use relative path **/
    //         __result = (from path in source
    //                     select new FolderAssist.FileInfo(path, Helpers.PathFromRoot(path), new DateTime?(File.GetLastWriteTime(path)))).ToArray<FolderAssist.FileInfo>();
    //         return false;
    //     }
    // }

    // [HarmonyPatch(typeof(FolderAssist))]
    // [HarmonyPatch("CreateFolderInfoToArray")]
    // class PatchCharaFinder2
    // {
    //     static bool Prefix(ref FolderAssist.FileInfo[] __result, string folder, string searchPattern, bool getFiles = true)
    //     {
    //         if (!Directory.Exists(folder))
    //         {
    //             __result = null;
    //             return false;
    //         }
    //         /** Changed to return files in subdirectories **/
    //         string[] source = getFiles ? Directory.GetFiles(folder, searchPattern, SearchOption.AllDirectories) : Directory.GetDirectories(folder);
    //         if (!source.Any<string>())
    //         {
    //             __result = null;
    //             return false;
    //         }
    //         /** Changed to use relative path **/
    //         __result = (from path in source
    //                     select new FolderAssist.FileInfo(path, (!getFiles) ? string.Empty : Helpers.PathFromRoot(path), new DateTime?(File.GetLastWriteTime(path)))).ToArray<FolderAssist.FileInfo>();
    //         return false;
    //     }
    // }

    // [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    // [HarmonyPatch("SaveCharaFile")]
    // [HarmonyPatch(new Type[] { typeof(string), typeof(byte), typeof(bool) })]
    // /* SaveCharaFile sets charaFileName to the file name without the relative path. Inject to retain the relative path. */
    // class PatchSaveCharaFile
    // {
    //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         foreach (var instruction in instructions)
    //         {
    //             if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileName(System.String)"))
    //             {
    //                 yield return new CodeInstruction(OpCodes.Call, typeof(Helpers).GetMethod(nameof(Helpers.PathFromRoot), BindingFlags.Public | BindingFlags.Static));
    //             }
    //             else
    //             {
    //                 yield return instruction;
    //             }
    //         }
    //     }
    // }

    // // FIXME need to convert to patch
    // [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    // [HarmonyPatch("LoadCharaFile")]
    // [HarmonyPatch(new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
    // class PatchLoadCharaFile
    // {
    //     static bool Prefix(ref bool __result, AIChara.ChaFileControl __instance, string filename, byte sex = 255, bool noLoadPng = false, bool noLoadStatus = true)
    //     {
    //         if (string.IsNullOrEmpty(filename))
    //         {
    //             __result = false;
    //             return false;
    //         }


    //         // CHANGED THIS -- use filename for the charaFileName ? does this do anything? 
    //         /**
    //          * 	base.charaFileName = Path.GetFileName(filename);
		  //    *  string path = this.ConvertCharaFilePath(filename, sex, false);
	   //  	 *  if (!File.Exists(path))
    //          * */


    //         /* Attempt to find the path directly. */
    //         String charapath = filename;
    //         if (!File.Exists(charapath))
    //         {
    //             byte sexByte = (sex == byte.MaxValue) ? __instance.parameter.sex : sex;
    //             charapath = Helpers.ConstructPath(filename, sexByte);
    //         }


    //         String charaName = Helpers.PathFromRoot(charapath);
    //         __instance.GetType().BaseType.InvokeMember("charaFileName", BindingFlags.SetProperty, null, __instance, new object[] { charaName });


    //         if (!File.Exists(charapath))
    //         {
    //             Logger.Log($"Couldn't find file : {charapath}");
    //             __result = false;
    //             return false;
    //         }
    //         using (FileStream fileStream = new FileStream(charapath, FileMode.Open, FileAccess.Read))
    //         {
    //             __result = __instance.LoadCharaFile(fileStream, noLoadPng, noLoadStatus);
    //         }
    //         return false;
    //     }
    // }


    // [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    // [HarmonyPatch("ConvertCharaFilePath")]
    // class PatchConvertCharaFilePath
    // {
    //     static bool Prefix(ref string __result, AIChara.ChaFileControl __instance, string path, byte _sex, bool newFile = false)
    //     {
    //         /* We need a new path. */
    //         if (newFile || __instance.charaFileName == "")
    //         {
    //             return true;
    //         }
    //         /* Looks like a real path, ensure the extension exists. */
    //         if (Helpers.IsFullPath(path))
    //         {
    //             Logger.Log($"Looks like a real path: {path} ");
    //             var noExt = Path.ChangeExtension(path, null);
    //             __result = noExt + ".png";
    //             return false;
    //         }

    //         /* Looks like a relative path, reconstruct it and return. */
    //         byte sexByte = (byte.MaxValue == _sex) ? __instance.parameter.sex : _sex;
    //         var absPath = Helpers.ConstructPath(path, sexByte);
    //         __result = absPath;
    //         //Logger.Log($"Running convert chara file path on {path} --> {absPath}");
    //         return false;
    //     }
    // }

    //[HarmonyPatch(typeof(CharaCustom.CustomCharaWindow))]
    //[HarmonyPatch("UpdateWindow")]
    //class PatchCharaEdit
    //{
    //    public static bool Prefix(bool modeNew, int sex, bool save, List<CharaCustom.CustomCharaFileInfo> _lst = null)
    //    {
    //        if (_lst != null)
    //        {
    //            Logger.Log($"Updating window with {_lst.Count} elements");
    //            foreach (var e in _lst)
    //            {
    //                Logger.Log($"element {e.FileName} |||||| {e.FullPath}");
    //            }
    //        }

    //        return true;
    //    }
    //}

    //[HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController))]
    //[HarmonyPatch("FindInfoByFileName")]
    //class PatchLobbyFindChara
    //{
    //    public static bool Prefix(string _fileName)
    //    {
    //        Logger.Log($"Finding info by file name {_fileName}");
    //        return true;
    //    }
    //}

    //[HarmonyPatch(typeof(HS2.LobbySelectUI1))]
    //[HarmonyPatch("LoadSelectCard")]
    //class PatchLoadSelectCard
    //{
    //    public static bool Prefix(LobbyCharaSelectInfoScrollController1.ScrollData _data)
    //    {
    //        Logger.Log($"We are loading select card {_data.info.FullPath}");
    //        return true;
    //    }
    //}

    //[HarmonyPatch(typeof(AIChara.ChaFileControl))]
    //[HarmonyPatch("LoadFileLimited")]
    //class PatchLoadLimited
    //{
    //    public static bool Prefix(bool __result, string filename, byte sex = 255, bool face = true, bool body = true, bool hair = true, bool parameter = true, bool coordinate = true)
    //    {
    //        Logger.Log($"Loading limited!! {filename}");
    //        return true;
    //    }
    //}

    //    [HarmonyPatch(typeof(HS2.GroupListUI))]
    //    [HarmonyPatch("AddList")]
    //    class PatchLoadGroupUI
    //    {
    //        public static string Injected(String s)
    //        {
    ////            UnityEngine.Debug.Log($"We got {s} path for character data!!!");
    //            return s;
    //        }

    //        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //        {
    //            bool flag1 = false;

    //            Logger.Log("We are transpiling some add list code");
    //            foreach (var instruction in instructions)
    //            {
    //                String s = instruction.ToString();
    //                if (flag1 && s.Equals("callvirt System.String System.Object::ToString()"))
    //                {
    //                    Logger.Log("Patching random strinbuilder nonsense");

    //                    yield return instruction;
    //                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchLoadGroupUI).GetMethod(nameof(Injected), BindingFlags.Public | BindingFlags.Static));
    //                    flag1 = false;
    //                }
    //                else if (s.Equals("ldloc.s 10 (System.Text.StringBuilder)"))
    //                {
    //                    flag1 = true;
    //                } else
    //                {
    //                    flag1 = false;
    //                }

    //                yield return instruction;
    //            }
    //        }

    //    }

}

