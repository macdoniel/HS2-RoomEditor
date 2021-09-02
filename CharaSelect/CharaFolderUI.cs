using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using CharaCustom;
using Logger = UnityEngine.Debug;

using System.Reflection;

namespace CharaSelect
{
    class CharaFolderUI : MonoBehaviour
    {
        MonoBehaviour parent;
        GameObject contentPane;
        GameObject prefab;

        float width;
        String rootDir;
        public String currentDir;

        const int PADDING = 5;
        public Action action;

        public CvsC_ClothesSave clothesSave;

        public void Initialize(MonoBehaviour parent, GameObject contentPane, GameObject btnPrefab, String rootPath, Action action)
        {
            this.action = action;
            this.parent = parent;
            this.contentPane = contentPane;
            this.prefab = btnPrefab;
            rootDir = Path.GetFullPath(UserData.Path + rootPath);
            width = contentPane.GetComponent<RectTransform>().rect.width;

            Callback(rootDir);

            CvsC_ClothesSave controller = this.transform.GetComponent<CvsC_ClothesSave>();

            if (controller != null)
            {
                this.clothesSave = controller;
                Logger.Log("CharaSelect :: In save controller, adding button!");
                StartCoroutine(nameof(CreateSaveButton));
            }
        }

        public IEnumerator CreateSaveButton()
        {
            CustomClothesWindow window = Helpers.Field<CustomClothesWindow>(this.clothesSave, "clothesLoadWin");

            Transform saveBtnTransform = this.clothesSave.transform.Find("buttons/btn02");
            UI_ButtonEx saveBtn = saveBtnTransform.GetComponent<UI_ButtonEx>();
            Transform saveBtnCopy = UnityEngine.MonoBehaviour.Instantiate(saveBtn.transform, saveBtn.transform.parent);
            UnityEngine.MonoBehaviour.Destroy(saveBtnCopy.GetComponent<UI_ButtonEx>());

            UnityEngine.UI.Text text = saveBtnCopy.transform.Find("Text").GetComponent<UnityEngine.UI.Text>();
            text.color = Color.black;

            yield return new WaitForSeconds(1);

            UI_ButtonEx newBtn = saveBtnCopy.gameObject.AddComponent<UI_ButtonEx>();
            UnityEngine.UI.Image image = newBtn.transform.Find("imgSelect").GetComponent<UnityEngine.UI.Image>();
            FieldInfo info = newBtn.GetType().GetField("overImage", BindingFlags.Instance | BindingFlags.NonPublic);
            info.SetValue(newBtn, image);

            newBtn.onClick.AddListener(() => {
				Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.ok_s);

                CvsC_ClothesInput input = Helpers.Field<CvsC_ClothesInput>(this.clothesSave, "clothesNameInput");
                CvsC_CreateCoordinateFile createFile = Helpers.Field<CvsC_CreateCoordinateFile>(this.clothesSave, "createCoordinateFile");

                if (null != input)
                {
                    input.SetupInputCoordinateNameWindow("");
                    input.actEntry = delegate(string buf)
                    {
                        string name = "HS2CoordeF_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        string fullPath = this.currentDir + "/" + name + ".png";
                        createFile.CreateCoordinateFile(fullPath, buf, true);
                    };
                }
            });

            saveBtn.gameObject.SetActive(false);

            Debug.Log("CharaSelect :: FINISH SAVE REPLACE");
        }

        private void CreateButton(String name, String dir, int index)
        {
            Debug.Log("Creating button " + name);
            var btnFolder = Instantiate(this.prefab, this.contentPane.transform);
            var text = btnFolder.transform.Find("Text");

            int columns = CharaSelectPlugin.Columns.Value;

            var x = index % columns;
            var y = index / columns;
            var w = this.width / columns - 2*PADDING;

            int buttonHeight = CharaSelectPlugin.ButtonHeight.Value;

            var theText = text.GetComponent<UnityEngine.UI.Text>();

            theText.text = name;
            theText.fontSize = 14;
            theText.color = Color.white;
            btnFolder.transform.localPosition = new Vector2( (this.width/columns)*x, (-(buttonHeight + PADDING) * y) - (buttonHeight + PADDING)/2);
            btnFolder.GetComponent<RectTransform>().sizeDelta = new Vector2(w, buttonHeight);
            theText.color = Color.black;
            //text.GetComponent<RectTransform>().sizeDelta = new Vector2(w, buttonHeight);
            //text.localPosition = new Vector3(0, PADDING/2, 0);
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
                theText.color = Color.black;
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

            action.Invoke();

            var buttonList = list.Select((s, i) => new { s.name, s.directory, index = i })
                .ToList();

            buttonList.ForEach((t) => CreateButton(t.name, t.directory, t.index));

            int columns = CharaSelectPlugin.Columns.Value;
            int buttonHeight = CharaSelectPlugin.ButtonHeight.Value;
            RectTransform rt = contentPane.transform.GetComponent<RectTransform>();
            Vector2 sizeDelta = rt.sizeDelta;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, (buttonHeight + 2*PADDING)*(buttonList.Count / columns)  );
        }
    }
}
