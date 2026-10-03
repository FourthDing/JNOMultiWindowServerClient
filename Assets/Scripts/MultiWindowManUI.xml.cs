namespace Assets.Scripts.Ui.Designer
{
    using ModApi;
    using ModApi.Common;
    using ModApi.Ui;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using UI.Xml;
    using UnityEngine;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement;
    using Debug = UnityEngine.Debug;
    using Random = UnityEngine.Random;

    public class MultiWindowManUI : MonoBehaviour
    {
        public class UIViewModel : XmlLayoutViewModel
        {
            public ObservableList<ExternalWindow> windowDatas { get; set; }
            public string myTestVariable { get; set; }
        }
        public class UIController: XmlLayoutController<UIViewModel>, IXmlLayoutController
        {

            public ExternalWindowsList windows;
            public void ListSource(ExternalWindowsList list)
            {
                Debug.Log("绑定列表……");
                windows = list;
                windows.OnUpdate += UpdateObservableList;
            }
            private void UpdateObservableList()
            {

            }

            public IXmlLayout XmlLayout;
            protected override void PrepopulateViewModelData()
            {
                Debug.Log("Test ViewModel");
                viewModel = new()
                {
                    myTestVariable = "Test"
                };
            }

        }
        public bool isVisible { get; private set; } = true;
        public UIController controller;

        public void OnLayoutRebuilt(XmlLayoutController ctrl)
        {
            this.controller = this.gameObject.AddComponent<UIController>();
            controller.viewModel = new()
            {
                windowDatas = new()
            };

            controller.XmlLayout = gameObject.GetComponent<XmlLayout>();
        }
        public void Hide()
        {
            Debug.Log("Hiding……");
            Debug.Log(controller.ToString());
            if (controller !=null)
            {
                Debug.Log("XmlLayout存在："+(controller.XmlLayout != null));
            }
            this.controller.XmlLayout.Hide();
            isVisible = false;
        }
        public void Show()
        {
            this.controller.XmlLayout.Show();
            isVisible = true;
        }

#pragma warning disable IDE0051
        public void AddNewWindow()
        {
            Debug.Log("添加窗口" + (controller.windows.Count + 1).ToString() + "……目前有" + controller.windows.Count.ToString() + "个窗口。");
            string id = Mod.MultiWindowAppId + "-" + (controller.windows.Count + 1).ToString();

            controller.windows.Add(id);

            //IXmlElement listElement = _controller.XmlLayout.GetElementById("WindowList");
            //var windowTemplate = _controller.XmlLayout.GetElementById("WindowRepresentExample");
            //var newElementObj = Instantiate(windowTemplate.GameObject);
            //IXmlElement newElement = newElementObj.GetComponent<XmlElement>();
            //newElement.SetAttribute("window", id);
            //newElement.AddClass("ExternalWindow");
            //newElement.RemoveAttribute("id");
            //newElement.ApplyAttributes();
            //newElementObj.SetActive(true);
            //listElement.AddChildElement(newElement);

            Debug.Log("往ViewModel里添东西……");
            Debug.Log("ViewModel存在吗？" + (controller.viewModel != null).ToString());
            Debug.Log("windowDatas存在吗？" + (controller.viewModel.windowDatas != null).ToString());
            if (controller.viewModel.windowDatas != null)
            {
                foreach (var item in controller.viewModel.windowDatas)
                {
                    Debug.Log(item.ToString());
                }
            }
            int times = Random.Range(2, 10);
            Debug.Log("加完了，也不知道有没有成功加上");
            foreach (var item in controller.viewModel.windowDatas)
            {
                Debug.Log(item.ToString());
            }

        }
        public void CloseExternalWindow(string id)
        {
            controller.windows.RemoveById(id);
        }
#pragma warning restore IDE0051

        private void OnWindowsUpdate()
        {

        }
        public void Start()
        {
            Debug.Log("MultiWindowUI启动了");
            Debug.Log("LostTrace Start:" + new StackFrame(1).GetMethod().Name + " is being performed.");

        }
        public void OnEnable()
        {

        }
        public void OnDisable()
        {

        }
        public void Update()
        {
            Debug.Log("LostTrace:"+ new StackFrame(1).GetMethod().Name+" is being performed.");
        }
    }
}