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

namespace CharaSelect
{

    class Helpers
    {
        public static String[] possibleRoots =
        {
            "chara/female/",
            "chara/male/",
            "chara/navi/",
            "coordinate/female/",
            "coordinate/male/",
            "cardframe/Back/",
            "cardframe/Front/",
            "bg/"
        };

        public static bool IsFullPath(string path)
        {
            if (path.IndexOf(UserData.Path) >= 0)
            {
                return true;
            }
            var thePath = path.Replace("\\", "/");

            foreach (var p in Helpers.possibleRoots)
            {
                if (thePath.IndexOf(p) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string ConstructPath(string name, byte sex)
        {
            bool male = (sex == 0);
            string path = UserData.Path + (male ? "chara/male/" : "chara/female/") + name;
            path  = Path.ChangeExtension(path, null);
            return path + ".png";
        }

        public static string PathFromRoot(string path)
        {

            path = path.Replace("\\", "/");
            String relpath = path;
            bool found = false;
            foreach (var p in Helpers.possibleRoots)
            {
                int idx = path.IndexOf(p);
                if (idx > 0)
                {
                    found = true;
                    relpath = path.Substring(idx + p.Length, path.Length - idx - p.Length);
                    break;
                }
            }

            if (!found)
            {
                Logger.Log($"Tried to get path from root but couldn't find it : {path}");
                return Path.GetFileNameWithoutExtension(path);
            }
            relpath = Path.ChangeExtension(relpath, null);
            return relpath;
        }
    }

    [HarmonyPatch(typeof(HS2.GroupCharaSelectUI))]
    [HarmonyPatch("ReDrawListView")]
    /* Filter the character list by the current directory of the attached component. */
    class PatchCharaRedraw
    {
        public static List<GameLoadCharaFileSystem.GameCharaFileInfo> Injected(List<GameLoadCharaFileSystem.GameCharaFileInfo> list, HS2.GroupCharaSelectUI instance)
        {
            CharaFolderUI ui = instance.GetComponent<CharaFolderUI>();
            if (ui == null)
            {
                return list;
            }
            return list.FindAll(info => Directory.GetParent(info.FullPath).FullName.Equals(ui.currentDir));
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.ToString().Equals("ldfld System.Collections.Generic.List`1[GameLoadCharaFileSystem.GameCharaFileInfo] charaLists"))
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchCharaRedraw).GetMethod(nameof(Injected), BindingFlags.Public | BindingFlags.Static));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(HS2.GroupCharaSelectUI))]
    [HarmonyPatch("Start")]
    /* Patch the UI to add subfolder support. */
    class PatchCharaSelectUI
    {
        static void Postfix(HS2.GroupCharaSelectUI __instance)
        {
            UnityEngine.Debug.Log($"Postfix constructor patch\n\n\n");
            var panel = __instance.transform.Find("Panel");
            var sep = panel.transform.Find("imgSeparate (1)");
            var sepClone = Object.Instantiate(sep, panel.transform);
            var view = panel.Find("View");
            var scrollView = view.Find("Scroll View");
            var delta = scrollView.GetComponent<RectTransform>().sizeDelta;
            var viewCopy = Object.Instantiate(view, panel.transform);
            var folderScroll = viewCopy.Find("Scroll View");
            var folderContent = folderScroll.Find("Viewport/Content");
            folderContent.Find("Raw").gameObject.SetActive(false);
            var btn = panel.Find("Buttons/btnEntry");

            var menuHeight = CharaSelectPlugin.MenuHeight.Value;
            var padding = 10;

            viewCopy.SetSiblingIndex(2);
            sepClone.SetSiblingIndex(3);
            folderScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(delta.x, menuHeight - padding);
            folderScroll.GetComponent<UnityEngine.UI.ScrollRect>().scrollSensitivity = 15;


            sepClone.transform.Translate(0, delta.y - menuHeight, 0);

            view.transform.Translate(0, -menuHeight - 2*padding, 0);

            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(delta.x, delta.y - menuHeight - 2*padding);

            var plugin = __instance.gameObject.AddComponent<CharaFolderUI>();
            plugin.Initialize(__instance, folderContent.gameObject, btn.gameObject, "chara/female");
            Logger.Log("Finished post constructor patch");
        }
    }

    [HarmonyPatch(typeof(SaveData))]
    [HarmonyPatch("IsRoomListChara")]
    /* Remove the call to GetFileNameWithoutExtension, to integrate with the full path. */
    class PatchSaveDataFind
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileNameWithoutExtension(System.String)"))
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Manager.LobbySceneManager))]
    [HarmonyPatch("LoadChara")]
    /* Replace the naive filename with a query from the root path. */
    class PatchLobbyLoad
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileNameWithoutExtension(System.String)"))
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(Helpers).GetMethod(nameof(Helpers.PathFromRoot), BindingFlags.Public | BindingFlags.Static));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    // FIXME need to convert to patch
    [HarmonyPatch(typeof(FolderAssist))]
    [HarmonyPatch("CreateFolderInfoExToArray")]
    class PatchCharaFinder
    {
        static bool Prefix(ref FolderAssist.FileInfo[] __result, string folder, params string[] searchPattern)
        {
            if (!Directory.Exists(folder))
            {
                __result = null;
                return false;
            }
            // CHANGED THIS -- add alldirs
            string[] source = searchPattern.SelectMany((string pattern) => Directory.GetFiles(folder, pattern, SearchOption.AllDirectories)).ToArray<string>();
            if (!source.Any<string>())
            {
                __result = null;
                return false;
            }
            __result = (from path in source
                            // CHANGED THIS -- pathfrom root
                        select new FolderAssist.FileInfo(path, Helpers.PathFromRoot(path), new DateTime?(File.GetLastWriteTime(path)))).ToArray<FolderAssist.FileInfo>();
            return false;
        }
    }

    // FIXME need to convert to patch
    [HarmonyPatch(typeof(FolderAssist))]
    [HarmonyPatch("CreateFolderInfoToArray")]
    class PatchCharaFinder2
    {
        static bool Prefix(ref FolderAssist.FileInfo[] __result, string folder, string searchPattern, bool getFiles = true)
        {
            if (!Directory.Exists(folder))
            {
                __result = null;
                return false;
            }
            // CHANGED THIS -- add alldirs
            string[] source = getFiles ? Directory.GetFiles(folder, searchPattern, SearchOption.AllDirectories) : Directory.GetDirectories(folder);
            if (!source.Any<string>())
            {
                __result = null;
                return false;
            }
            __result = (from path in source
                            // CHANGED THIS -- pathfrom root
                        select new FolderAssist.FileInfo(path, (!getFiles) ? string.Empty : Helpers.PathFromRoot(path), new DateTime?(File.GetLastWriteTime(path)))).ToArray<FolderAssist.FileInfo>();
            return false;
        }
    }

    [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    [HarmonyPatch("SaveCharaFile")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(byte), typeof(bool) })]
    /* SaveCharaFile sets charaFileName to the file name without the relative path. Inject to retain the relative path. */
    class PatchSaveCharaFile
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.ToString().Equals("call System.String System.IO.Path::GetFileName(System.String)"))
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(Helpers).GetMethod(nameof(Helpers.PathFromRoot), BindingFlags.Public | BindingFlags.Static));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    // FIXME need to convert to patch
    [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    [HarmonyPatch("LoadCharaFile")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(byte), typeof(bool), typeof(bool) })]
    class PatchLoadCharaFile
    {
        static bool Prefix(ref bool __result, AIChara.ChaFileControl __instance, string filename, byte sex = 255, bool noLoadPng = false, bool noLoadStatus = true)
        {
            if (string.IsNullOrEmpty(filename))
            {
                __result = false;
                return false;
            }


            // CHANGED THIS -- use filename for the charaFileName ? does this do anything? 
            /**
             * 	base.charaFileName = Path.GetFileName(filename);
		     *  string path = this.ConvertCharaFilePath(filename, sex, false);
	    	 *  if (!File.Exists(path))
             * */


            /* Attempt to find the path directly. */
            String charapath = filename;
            if (!File.Exists(charapath))
            {
                byte sexByte = (sex == byte.MaxValue) ? __instance.parameter.sex : sex;
                charapath = Helpers.ConstructPath(filename, sexByte);
            }


            String charaName = Helpers.PathFromRoot(charapath);
            __instance.GetType().BaseType.InvokeMember("charaFileName", BindingFlags.SetProperty, null, __instance, new object[] { charaName });


            if (!File.Exists(charapath))
            {
                Logger.Log($"Couldn't find file : {charapath}");
                __result = false;
                return false;
            }
            using (FileStream fileStream = new FileStream(charapath, FileMode.Open, FileAccess.Read))
            {
                __result = __instance.LoadCharaFile(fileStream, noLoadPng, noLoadStatus);
            }
            return false;
        }
    }


    [HarmonyPatch(typeof(AIChara.ChaFileControl))]
    [HarmonyPatch("ConvertCharaFilePath")]
    class PatchConvertCharaFilePath
    {
        static bool Prefix(ref string __result, AIChara.ChaFileControl __instance, string path, byte _sex, bool newFile = false)
        {
            /* We need a new path. */
            if (newFile || __instance.charaFileName == "")
            {
                return true;
            }
            /* Looks like a real path, ensure the extension exists. */
            if (Helpers.IsFullPath(path))
            {
                Logger.Log($"Looks like a real path: {path} ");
                var noExt = Path.ChangeExtension(path, null);
                __result = noExt + ".png";
                return false;
            }

            /* Looks like a relative path, reconstruct it and return. */
            byte sexByte = (byte.MaxValue == _sex) ? __instance.parameter.sex : _sex;
            var absPath = Helpers.ConstructPath(path, sexByte);
            __result = absPath;
            //Logger.Log($"Running convert chara file path on {path} --> {absPath}");
            return false;
        }
    }

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

