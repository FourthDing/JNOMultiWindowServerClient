namespace Assets.Scripts
{
    using Assets.Scripts.Ui;
    using Assets.Scripts.Ui.Designer;
    using ModApi;
    using ModApi.Common;
    using ModApi.Common.Extensions;
    using ModApi.Mods;
    using ModApi.Ui;
    using ModApi.Ui.Events;
    using ModApi.Ui.Inspector;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using TMPro;
    using UI.Xml;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using WindowServer.IO;
    using static ModApi.Ui.InputDialogScript;
    using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        public static string MultiWindowAppId = "JunoNewOrigins";
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {

        }

        protected override void OnModInitialized()
        {
            Debug.Log($"Mod Initialized: {this.ModInfo.Name} - {this.ModInfo.Author} - {this.ModInfo.Version} - {this.ModInfo.LastUpdated}");

            SceneManager.sceneLoaded += SceneSpecific;
            SceneManager.sceneUnloaded += UnSceneSpecific;

            var ui = Game.Instance.UserInterface;
            //ui.UserInterfaceLoading += this.SceneSpecific;
            //ui.UserInterfaceLoaded += this.OnUserInterfaceLoaded;

            base.OnModInitialized();

            ui.AddBuildInspectorPanelAction(
                ModApi.Ui.Inspector.InspectorIds.FlightView,
                OnBuildFlightViewInspectorPanel);
        }

        private void SceneSpecific(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("进场景：");
            Debug.Log(scene.name);
            switch (scene.name)
            {
                case "Flight":
                    Debug.Log("新建windows列表……");
                    windows = new();
                    break;
                default:
                    break;
            }
        }
        private void UnSceneSpecific(Scene scene)
        {
            switch (scene.name)
            {
                case "Flight":
                    Debug.Log("清掉windows列表……");
                    windows = null;
                    break;
                default:
                    break;
            }

        }

        MultiWindowManUI mwmUI;
        ExternalWindowsList windows;


        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            GameObject[] SR2CameraObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(obj => obj.name == "CameraPosition")
                .Select(obj => obj.transform.parent.gameObject)
                .ToArray();
            var ui = Game.Instance.UserInterface;
            //Debug.Log("加载预制体……");
            //GameObject mwmUIObject = Resources.Load<GameObject>("Prefab UI/MWMUI.prefab");
            //Debug.Log("设置爹……");
            //mwmUIObject.transform.parent = ui.Transform;
            //GameObject.Instantiate(mwmUIObject);
            //不行，就是得重写那个方法，好事是只写一次

            //GameObject mwmUIObj = UiUtilities.CreateUiGameObject(typeof(MultiWindowManUI).Name, ui.Transform);
            //mwmUI = mwmUIObj.AddComponent<MultiWindowManUI>();
            //BuildUserInterfaceXmlRequest buildUserInterfaceXmlRequest = BuildUserInterfaceXmlRequest.CreateFromResource("Multi Window Client/MultiWindowManUI", null);
            //XmlLayout mwmUILayout = mwmUIObj.AddComponent<XmlLayout>();

            //在这里运行就死：
            //MultiWindowManUI.UIController controller = mwmUIObj.AddComponent<MultiWindowManUI.UIController>();

            //mwmUI.controller = controller;
            //mwmUI.controller.ListSource(windows);
            //Debug.Log("设置了viewmodel窗口信息源");
            //controller.EventTarget = mwmUI;
            //controller.OnLayoutRebuilt = (controller) => { return; };
            //controller.XmlLayout = mwmUILayout;
            //controller.viewModel = new();
            //ui.BuildUserInterfaceFromRequest(buildUserInterfaceXmlRequest, mwmUILayout);
            //Debug.Log("创建了mwmUI");
            //在这就又行了？
            //MultiWindowManUI.UIController controller = mwmUIObj.AddComponent<MultiWindowManUI.UIController>();
            //mwmUI.Hide();



            //mwmUI = ui.BuildUserInterfaceFromResourceWithCustomController<MultiWindowManUI, MultiWindowManUI.UIController>("Multi Window Client/MultiWindowManUI",(script,controller) =>  script.OnLayoutRebuilt(controller));
            InspectorModel mwmModel = new("MultiWindowManInspector", "External Windows");
            IInspectorPanel mwmInspector = Game.Instance.UserInterface.CreateInspectorPanel(mwmModel);
            mwmInspector.CloseButtonClicked += delegate (IInspectorPanel p)
            {
                mwmInspector.Visible = false;
            };
            mwmInspector.Visible = false;

            void UpdateModelFromList()
            {
                Debug.Log("实施更新……目前有" + windows.Count.ToString() + "个窗口：");
                foreach (var item in mwmModel.Groups)
                {
                    Debug.Log("    " + item.Name);
                }
                mwmInspector.RebuildModelElements();
                Debug.Log("更新完成。");
            }


            //mwmUI.controller = mwmUI.gameObject.AddComponent<UIController>();
            //Debug.Log("创建了mwmUI的UIcontroller");
            //MonoBehaviour.DestroyImmediate(mwmUI.gameObject.GetComponent<XmlLayoutController>());
            //Debug.Log("干掉了随BuildUserInterfaceFromResource创建的XmlLayoutController");
            //mwmUI.controller.XmlLayout = mwmUI.gameObject.GetComponent<XmlLayout>();
            //Debug.Log("controller一致：" + (mwmUI.controller.XmlLayout.XmlLayoutController == mwmUI.controller).ToString());//False
            //Debug.Log("controller与原结果一致：" + (mwmUI.controller.XmlLayout.XmlLayoutController == mwmUI.gameObject.GetComponent<XmlLayoutController>()).ToString());//False
            //Debug.Log("隐藏窗口。");
            GroupModel windowCtrlGroup = new("External Windows");
            request.Model.AddGroup(windowCtrlGroup);
            windowCtrlGroup.Add(new TextModel("Warning:Mod is under development."));
            TextButtonModel enterManagementButton = new("Show/Hide Management UI",
                b =>
                {
                    mwmInspector.Visible = !mwmInspector.Visible;
                });
            TextButtonModel addWindowButton = new("Add New Window",
                b =>
                {
                    Debug.Log("触发了添加窗口！");
                    ModApi.Ui.InputDialogScript inputDialog = Game.Instance.UserInterface.CreateInputDialog();
                    inputDialog.MessageText = "Name the new window.";
                    inputDialog.InputPlaceholderText = "Window " + (windows.Count + 1).ToString();
                    inputDialog.OkayButtonText = "Create";
                    inputDialog.OkayClicked += iDialog =>
                    {
                        iDialog.Close();
                        //移入controller，添加对应的UpdateXXX(ExternalWindow window)
                        bool result = windows.Add(iDialog.InputText);
                        if (result)
                        {
                            ExternalWindow window = windows[iDialog.InputText];
                            GroupModel newGroup = new(window.windowId);

                            TextModel logNameText = new("LogName:", () => window.windowId);
                            newGroup.Add(logNameText);

                            TextButtonModel nameButton = new("Rename", b =>
                            {
                                ModApi.Ui.InputDialogScript renameDialog = Game.Instance.UserInterface.CreateInputDialog();
                                renameDialog.MessageText = "Enter the new name of the window.";
                                renameDialog.InputPlaceholderText = window.windowId;
                                renameDialog.OkayButtonText = "Rename";
                                renameDialog.OkayClicked += rDialog =>
                                {
                                    rDialog.Close();
                                    windows.RenameItem(window.windowId, rDialog.InputText);
                                };
                            });
                            newGroup.Add(nameButton);
                            
                            TextModel geometryText = new("Geometry:", () => $"{window.geometry.Width}*{window.geometry.Height}+{window.geometry.X}*{window.geometry.Y}");
                            newGroup.Add(geometryText);

                            TextButtonModel geometryButton = new("Resize", b =>
                            {
                                //先写成挨个设置吧
                                resizeX(window);

                                void resizeX(ExternalWindow window)
                                {
                                    ModApi.Ui.InputDialogScript xDialog = Game.Instance.UserInterface.CreateInputDialog();
                                    TMP_InputField field = xDialog.transform.gameObject.GetComponentInChildren<TMP_InputField>();
                                    field.contentType = TMP_InputField.ContentType.IntegerNumber;
                                    xDialog.MessageText = "Enter the new size of the window, start from X:";
                                    xDialog.InputPlaceholderText = window.geometry.X.ToString();
                                    xDialog.OkayClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        window.geometry.X = rDialog.InputText.ToInt();
                                        resizeY(window);
                                    };
                                    xDialog.CancelClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        resizeY(window);
                                    };
                                }
                                void resizeY(ExternalWindow window)
                                {
                                    ModApi.Ui.InputDialogScript yDialog = Game.Instance.UserInterface.CreateInputDialog();
                                    TMP_InputField field = yDialog.transform.gameObject.GetComponentInChildren<TMP_InputField>();
                                    field.contentType = TMP_InputField.ContentType.IntegerNumber;
                                    yDialog.MessageText = "Y:";
                                    yDialog.InputPlaceholderText = window.geometry.Y.ToString();
                                    yDialog.OkayClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        window.geometry.Y = rDialog.InputText.ToInt();
                                        resizeW(window);
                                    };
                                    yDialog.CancelClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        resizeW(window);
                                    };
                                }
                                void resizeW(ExternalWindow window)
                                {
                                    ModApi.Ui.InputDialogScript wDialog = Game.Instance.UserInterface.CreateInputDialog();
                                    TMP_InputField field = wDialog.transform.gameObject.GetComponentInChildren<TMP_InputField>();
                                    field.contentType = TMP_InputField.ContentType.IntegerNumber;
                                    wDialog.MessageText = "Width:";
                                    wDialog.InputPlaceholderText = window.geometry.Width.ToString();
                                    wDialog.OkayClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        window.geometry.Width = rDialog.InputText.ToInt();
                                        resizeH(window);
                                    };
                                    wDialog.CancelClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        resizeH(window);
                                    };
                                }
                                void resizeH(ExternalWindow window)
                                {
                                    ModApi.Ui.InputDialogScript hDialog = Game.Instance.UserInterface.CreateInputDialog();
                                    TMP_InputField field = hDialog.transform.gameObject.GetComponentInChildren<TMP_InputField>();
                                    field.contentType = TMP_InputField.ContentType.IntegerNumber;
                                    hDialog.MessageText = "Height:";
                                    hDialog.InputPlaceholderText = window.geometry.Height.ToString();
                                    hDialog.OkayClicked += rDialog =>
                                    {
                                        rDialog.Close();
                                        window.geometry.Height = rDialog.InputText.ToInt();

                                    };
                                    hDialog.CancelClicked += rDialog =>
                                    {
                                        rDialog.Close();

                                    };
                                }
                            });
                            newGroup.Add(geometryButton);
                            
                            string _dropdownModelSelection = SR2CameraObjects[0].name;                           // class field
                            string[] _dropdownModelOptions = { "M-HCI3-P0:E_L", "M-HCI3-D1:E_R" }; // class field
                            var cameraDropdown = new DropdownModel(
                                "Camera",
                                () => _dropdownModelSelection,
                                value => {
                                    _dropdownModelSelection = value;
                                    Debug.Log(_dropdownModelSelection + ":" + SR2CameraObjects.Where(obj => obj.name == _dropdownModelSelection).First().transform.position.ToString());
                                    window.PipedCamera.transform.SetParent(SR2CameraObjects.Where(obj => obj.name == _dropdownModelSelection).First().transform.Find("CameraPosition"), false);
                                    //window.PipedSubCamera.transform.SetParent(SR2CameraObjects.Where(obj => obj.name == _dropdownModelSelection).First().transform.Find("CameraPosition"), false);
                                },
                                SR2CameraObjects.Select((cam) => cam.name));
                            newGroup.Add(cameraDropdown);

                            LabelButtonModel closeButton = new LabelButtonModel("Close Window",
                                b =>
                                {
                                    mwmModel.RemoveGroup(newGroup);
                                    windows.Remove(window);
                                    UpdateModelFromList();
                                }
                                )
                            {
                                ButtonLabel = "Close"
                            };
                            newGroup.Add(closeButton);

                            mwmModel.AddGroup(newGroup);
                            Debug.Log("试图重建……");
                            UpdateModelFromList();
                        }
                        else { Debug.Log("疑似窗口名重复，没有再进一步操作。"); }
                        ;

                    };


                });
            windowCtrlGroup.Add(enterManagementButton);
            windowCtrlGroup.Add(addWindowButton);
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();


    }
}