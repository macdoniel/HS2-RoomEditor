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

        public static T Field<T>(object gameObject, string name)
        {
            Type t = gameObject.GetType();
            FieldInfo info = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            object o = info.GetValue(gameObject);
            return (T)o;
        }

        public static Instructions Replace(Instructions instructions, string inst, Func<CodeInstruction,Instructions> callback)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.ToString().StartsWith(inst))
                {
                    var res = callback.Invoke(instruction);
                    foreach (var r in res)
                    {
                        yield return r;
                    }
                }
                else
                {
                    yield return instruction;
                }
            }

        }

        public class UI
        {
            private static void MoveRight(RectTransform t, float delta)
            {
                var offMin = t.offsetMin;
                var offMax = t.offsetMax;
                t.offsetMin = new Vector2(offMin.x + delta, offMin.y);
                t.offsetMax = new Vector2(offMax.x + delta, offMax.y);
            }

            public static void ModifyHSceneMenu(HSceneFolderUI plugin, HSceneSpriteCoordinatesCard menu)
            {
                UnityEngine.Debug.Log($"Postfix constructor patch\n\n\n");
                var bgPanel = menu.transform.Find("CardImageBG");
                var coordPanel = menu.transform.Find("CoodenatePanel");
                var newCoordPanel = menu.transform.Find("CoodenatePanel(Clone)");
                Transform contentPanel = null;
                if (newCoordPanel == null)
                {
                    newCoordPanel = Object.Instantiate(coordPanel, menu.transform, false);
                    var bgT = bgPanel.GetComponent<RectTransform>();
                    var coordT = coordPanel.GetComponent<RectTransform>();
                    var delta = coordT.sizeDelta;
                    MoveRight(bgT, delta.x);
                    MoveRight(coordT, delta.x);

                    /** Disable some elements. **/
                    newCoordPanel.Find("SortDate").gameObject.SetActive(false);
                    newCoordPanel.Find("SortName").gameObject.SetActive(false);
                    newCoordPanel.Find("Sort Up").gameObject.SetActive(false);
                    newCoordPanel.Find("Sort Down").gameObject.SetActive(false);
                    newCoordPanel.Find("DecideCoode").gameObject.SetActive(false);
                    contentPanel = newCoordPanel.Find("Scroll View/Viewport/Content");
                }
                else
                {
                    contentPanel = newCoordPanel.Find("Scroll View/Viewport/Content");
                    foreach (Transform t in contentPanel)
                    {
                        t.gameObject.SetActive(false);
                    }
                }

                var btn = coordPanel.Find("DecideCoode");
                var content = newCoordPanel.Find("Scroll View/Viewport/Content");
                var newBtn = Object.Instantiate(btn, content.transform, false);
                newBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 30f);
                newBtn.gameObject.SetActive(false);
                plugin.btnProto = newBtn.gameObject;
                plugin.contentPane = contentPanel;
                plugin.activated = true;
                Logger.Log("Finished post constructor patch");
            }

            public static void ModifyGameMenu(MonoBehaviour menu, string rootPath, Action action)
            {

                UnityEngine.Debug.Log($"Postfix constructor patch\n\n\n");
                var sep = menu.transform.Find("separate");
                var sepClone = Object.Instantiate(sep, menu.transform);
                var scrollView = menu.transform.Find("Scroll View");
                var delta = scrollView.GetComponent<RectTransform>().sizeDelta;
                var folderScroll = Object.Instantiate(scrollView, menu.transform);
                var folderContent = folderScroll.Find("Viewport/Content");
                //folderContent.Find("Raw").gameObject.SetActive(false);

                var possibleBtns = new string[] { "buttons/btn01", "buttons/btn02", "buttons/btnOverwrite", "buttons/btnSave", "buttons/btn03", "Buttons/btnEntry", "Buttons/btnBatchRelease" };

                Transform btn = null;

                foreach (var pbtn in possibleBtns)
                {
                    var b = menu.transform.Find(pbtn);
                    if (b != null)
                    {
                        btn = b;
                        break;
                    }
                }

                var menuHeight = CharaSelectPlugin.MenuHeight.Value;
                var padding = 10;

                folderScroll.SetSiblingIndex(2);
                sepClone.SetSiblingIndex(3);
                folderScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(delta.x, menuHeight + padding);
                folderScroll.GetComponent<UnityEngine.UI.ScrollRect>().scrollSensitivity = 15;


                //sepClone.transform.Translate(0, - menuHeight, 0);

                scrollView.transform.Translate(0, -menuHeight - 2 * padding, 0);
                scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(delta.x, delta.y - menuHeight - 2 * padding);

                var plugin = menu.gameObject.AddComponent<CharaFolderUI>();
                plugin.Initialize(menu, folderContent.gameObject, btn.gameObject, rootPath, action);
                Logger.Log("Finished post constructor patch");
            }
        }
    }

}