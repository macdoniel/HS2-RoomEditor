using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CharaSelect
{
    class CharaFolderUI : MonoBehaviour
    {
        HS2.GroupCharaSelectUI parent;
        GameObject contentPane;
        GameObject prefab;

        float width;
        String rootDir;
        public String currentDir;

        const int PADDING = 5;

        public void Initialize(HS2.GroupCharaSelectUI parent, GameObject contentPane, GameObject btnPrefab, String rootPath)
        {
            this.parent = parent;
            this.contentPane = contentPane;
            this.prefab = btnPrefab;
            rootDir = Path.GetFullPath(UserData.Path + rootPath);
            width = contentPane.GetComponent<RectTransform>().rect.width;

            Callback(rootDir);
        }

        private void CreateButton(String name, String dir, int index)
        {
            var btnFolder = Instantiate(this.prefab, this.contentPane.transform);
            var text = btnFolder.transform.Find("Text");
            var collider = btnFolder.transform.Find("collision");

            int columns = CharaSelectPlugin.Columns.Value;

            var x = index % columns;
            var y = index / columns;
            var w = this.width / columns - 2*PADDING;

            int buttonHeight = CharaSelectPlugin.ButtonHeight.Value;

            var theText = text.GetComponent<UnityEngine.UI.Text>();

            theText.text = name;
            theText.color = Color.white;
            btnFolder.transform.localPosition = new Vector2( (this.width/(2*columns))*(2*(x+1) -1), (-(buttonHeight + PADDING) * y) - (buttonHeight + PADDING)/2);
            btnFolder.GetComponent<RectTransform>().sizeDelta = new Vector2(w, buttonHeight);
            collider.GetComponent<RectTransform>().sizeDelta = new Vector2(w, buttonHeight);
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(w, buttonHeight);
            text.localPosition = new Vector3(0, PADDING/2, 0);
            var theBtn = btnFolder.GetComponent<UnityEngine.UI.Button>();
            theBtn.onClick.RemoveAllListeners();

            btnFolder.SetActive(true);
            theBtn.interactable = true;
            theBtn.onClick.AddListener(delegate() { Callback(dir); });
            EventTrigger.Entry evtEnter = new EventTrigger.Entry();
            evtEnter.eventID = EventTriggerType.PointerEnter;
            evtEnter.callback.AddListener((eventData) =>
            {
                theText.color = Color.black;
            });

            EventTrigger.Entry evtExit = new EventTrigger.Entry();
            evtExit.eventID = EventTriggerType.PointerExit;
            evtExit.callback.AddListener((eventData) =>
            {
                theText.color = Color.white;
            });

            EventTrigger.Entry evtScroll = new EventTrigger.Entry();
            evtScroll.eventID = EventTriggerType.Scroll;
            evtScroll.callback.AddListener((eventData) =>
            {
                btnFolder.GetComponentInParent<UnityEngine.UI.ScrollRect>().SendMessage("OnScroll", eventData);
            });

            btnFolder.AddComponent<EventTrigger>();
            btnFolder.GetComponent<EventTrigger>().triggers.Add(evtEnter);
            btnFolder.GetComponent<EventTrigger>().triggers.Add(evtExit);
            btnFolder.GetComponent<EventTrigger>().triggers.Add(evtScroll);
        }

        public void Callback(String dir)
        {
            this.currentDir = dir;
            foreach (Transform c in this.contentPane.transform)
            {
                if (c.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    UnityEngine.Debug.Log("deleting");
                    c.gameObject.SetActive(false);
                    Destroy(c.gameObject);
                }
            }

            var list = Directory.EnumerateDirectories(dir)
                                .Select(d => new { name = Path.GetFileNameWithoutExtension(d), directory = d})
                                .ToList();

            if (!dir.Equals(rootDir))
            {
                list.Insert(0, new { name = "../", directory = Directory.GetParent(dir).FullName });
            }

            parent.ReDrawListView();

            list.Select((s, i) => new { s.name, s.directory, index = i })
                .ToList()
                .ForEach((t) => CreateButton(t.name, t.directory, t.index));

        }
    }
}
