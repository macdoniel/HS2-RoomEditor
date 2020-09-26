using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

using System.Reflection;
using Logger = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CharaSelect
{
    class HSceneFolderUI : MonoBehaviour
    {
        private string _rootDir;
        public string currentDir;
        private HSceneSpriteCoordinatesCard coordCard;

        public GameObject btnProto;
        public bool activated;
        public Transform contentPane;

        public float lastUpdatedTimestamp;

        public void Initialize(HSceneSpriteCoordinatesCard coordCard)
        {
            this.activated = false;
            this.coordCard = coordCard;
            this._rootDir = null;
            //_rootDir = Path.GetFullPath(UserData.Path + rootPath);
            //FilterCoords();
            lastUpdatedTimestamp = Time.time;
            Helpers.UI.ModifyHSceneMenu(this,coordCard);
        }

        public void Update()
        {
            if (!activated)
            {
                return;
            }
            if (Time.time - lastUpdatedTimestamp > 7)
            {
                Logger.Log("Initialization chance complete");
                activated = false;
                DeriveRootDir();
                Logger.Log($"Found root dir as {this._rootDir}");
                Callback(this.currentDir);
            }
        }

        public void DeriveRootDir()
        {
            String[] possibleRoots = {
            "chara/female/",
            "chara/male/",
            "coordinate/female/",
            "coordinate/male/",
            };


            var nodes = coordCard.GetComponentsInChildren<HSceneSpriteCoordinatesNode>();
            String foundPath = null;
            foreach (var node in nodes)
            {
                var filename = node.fileName;
                foreach (var root in possibleRoots)
                {
                    Logger.Log(filename);
                    if (filename.IndexOf(root) > 0)
                    {
                        foundPath = root;
                        break;
                    }
                }
                if (foundPath != null)
                {
                    break;
                }
            }
            this._rootDir = UserData.Path + foundPath;
            this.currentDir = this._rootDir;
        }

        public void Callback(String dir)
        {
            this.currentDir = dir;
            foreach (Transform c in this.btnProto.transform.parent)
            {
                if (c.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    c.gameObject.SetActive(false);
                    if (!c.gameObject.Equals(this.btnProto))
                    {
                        Destroy(c.gameObject);
                    }
                }
            }

            var list = Directory.EnumerateDirectories(dir)
                                .Select(d => new { name = Path.GetFileNameWithoutExtension(d), directory = d})
                                .ToList();

            if (!dir.Equals(this._rootDir))
            {
                Logger.Log(dir);
                Logger.Log(this._rootDir);
                list.Insert(0, new { name = "../", directory = Directory.GetParent(dir).ToString() });
            }
            list.Select((s, i) => new { s.name, s.directory, index = i })
                .ToList()
                .ForEach((t) => CreateButton(t.name, t.directory, t.index));

            FilterCoords();
        }

        private void CreateButton(String name, String dir, int index)
        {
            var newBtn = Instantiate(btnProto, btnProto.transform.parent, false);
            var theText = newBtn.transform.Find("Text").GetComponent<UnityEngine.UI.Text>();
            theText.text = name;

            var theBtn = newBtn.GetComponent<UnityEngine.UI.Button>();
            theBtn.onClick.AddListener(delegate() { Callback(dir); });

            newBtn.SetActive(true);
        }

        public void FilterCoords()
        {
            var lstCoordinates = coordCard.GetComponentsInChildren<HSceneSpriteCoordinatesNode>(true);

            if (this._rootDir == null)
            {
                DeriveRootDir();
            }

            Logger.Log($"Current dir is : {this.currentDir}");

            var currentFiles = Directory.GetFiles(this.currentDir);
            foreach (var f in currentFiles)
            {
                Logger.Log($"current file {f}");
            }

            var newList = lstCoordinates.Where((node) =>
            {
                Logger.Log($"coordinate : {node.fileName}");
                return currentFiles.Any((s) => s.Equals(node.fileName));
            }).ToList();

            foreach (var c in lstCoordinates)
            {
                c.gameObject.SetActive(newList.Contains(c));
            }
        }
    }
}
