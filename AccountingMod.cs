using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader.Utils;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;
using System.Collections;
using System.Linq;

[assembly: MelonInfo(typeof(StonksAccounting.StonksAccountingMod), StonksAccounting.BuildInfo.Name, StonksAccounting.BuildInfo.Version, StonksAccounting.BuildInfo.Author, StonksAccounting.BuildInfo.DownloadLink)]
[assembly: MelonColor(255, 255, 165, 0)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace StonksAccounting
{
    public static class BuildInfo
    {
        public const string Name = "S.T.O.N.K.S Accounting";
        public const string Description = "Simple accounting mod";
        public const string Author = "Goldie";
        public const string Company = null;
        public const string Version = "0.1";
        public const string DownloadLink = null;
    }
    public class StonksAccountingMod : MelonMod
    {
        [HarmonyPatch(typeof(Player), "ConsumeProduct")]
        public static class Player_ConsumeProduct_Patch
        {
            public static bool Prefix(Player __instance, ProductItemInstance product)
            {
                MelonLogger.Msg("Product is being consumed");
                return true;
            }
        }

        private bool _isInTargetScene;
        private GameObject _player;
        private bool _iconModified;
        private bool _appCreated;
        private bool _initializationCoroutineStarted;
        private GameObject _myCustomAppPanel;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg(Info.Name + " v" + Info.Version + " Initialized.");
        }

        public override void OnDeinitializeMelon()
        {
            LoggerInstance.Msg(Info.Name + " Deinitialized.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            bool wasInTargetScene = _isInTargetScene;
            _isInTargetScene = sceneName != null && sceneName.Contains("Main");
            if (_isInTargetScene && !wasInTargetScene)
            {
                LoggerInstance.Msg("Entered target scene: '" + sceneName + "'. Resetting state.");
                ResetSceneState();
            }
            else if (!_isInTargetScene && wasInTargetScene)
            {
                LoggerInstance.Msg("Left target scene, entering '" + sceneName + "'. Resetting state.");
                ResetSceneState();
            }
        }

        private void ResetSceneState()
        {
            _iconModified = false;
            _appCreated = false;
            _initializationCoroutineStarted = false;
            _player = null;
            _myCustomAppPanel = null;
        }

        public override void OnUpdate()
        {
            if (!_isInTargetScene) return;
            if (_player == null)
            {
                TryFindPlayerAndStartInit();
            }
        }

        private void TryFindPlayerAndStartInit()
        {
            GameObject playerObj = GameObject.Find("Player_Local");
            if (playerObj != null)
            {
                _player = playerObj;
                LoggerInstance.Msg("Found Player_Local object.");
                if (!_initializationCoroutineStarted)
                {
                    _initializationCoroutineStarted = true;
                    LoggerInstance.Msg("Starting 5s delay before App/Icon setup...");
                    MelonCoroutines.Start(DelayedInitialization());
                }
            }
        }

        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(5f);
            CreateOrEnsureAppAndIcon();
        }

        private void CreateOrEnsureAppAndIcon()
        {
            if (_appCreated && _iconModified) return;

            if (_player == null)
            {
                LoggerInstance.Error("Cannot create app/icon: Player_Local object is null.");
                return;
            }

            GameObject appsCanvas = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/AppsCanvas");
            if (appsCanvas == null)
            {
                LoggerInstance.Error("Could not find 'AppsCanvas'.");
                return;
            }

            Transform existingApp = appsCanvas.transform.Find("MyCustomApp");
            if (existingApp != null)
            {
                _myCustomAppPanel = existingApp.gameObject;
                EnsureAppPanelIsSetup(_myCustomAppPanel);
            }
            else
            {
                Transform templateApp = appsCanvas.transform.Find("ProductManagerApp");
                if (templateApp == null)
                {
                    LoggerInstance.Error("Cannot create app: 'ProductManagerApp' template not found.");
                    return;
                }
                _myCustomAppPanel = Object.Instantiate(templateApp.gameObject, appsCanvas.transform);
                _myCustomAppPanel.name = "MyCustomApp";
                Transform containerTransform = _myCustomAppPanel.transform.Find("Container");
                GameObject container = containerTransform != null ? containerTransform.gameObject : null;
                if (container != null)
                {
                    HideDefaultAppUI(container);
                    BuildCustomAppUI(container);
                }
                else
                {
                    LoggerInstance.Warning("Could not find 'Container' in new 'MyCustomApp'.");
                }
                _appCreated = true;
            }

            if (_myCustomAppPanel != null)
            {
                _myCustomAppPanel.SetActive(false);
            }

            if (!_iconModified)
            {
                _iconModified = ModifyAppIcon("MyCustomIcon", "My App", LoggerInstance);
                if (_iconModified)
                {
                    LoggerInstance.Msg("App icon modified successfully.");
                }
                else
                {
                    LoggerInstance.Error("Failed to modify app icon.");
                }
            }
        }

        private void EnsureAppPanelIsSetup(GameObject appPanel)
        {
            if (!_appCreated)
            {
                GameObject container = appPanel?.transform.Find("Container")?.gameObject;
                if (container != null && container.transform.childCount < 2)
                {
                    LoggerInstance.Msg("Rebuilding UI in existing panel '" + appPanel.name + "'...");
                    HideDefaultAppUI(container);
                    BuildCustomAppUI(container);
                }
                else if (container == null && appPanel != null)
                {
                    LoggerInstance.Error("EnsureAppPanelIsSetup: Container missing!");
                }
                _appCreated = true;
            }
        }

        private void HideDefaultAppUI(GameObject container)
        {
            if (container == null) return;
            for (int i = container.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = container.transform.GetChild(i);
                if (child != null)
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private void BuildCustomAppUI(GameObject container)
        {
            try
            {
                LoggerInstance.Msg("BuildCustomAppUI: Starting setup.");

                // Title Text
                GameObject textGO = new GameObject("AppTitleText");
                textGO.transform.SetParent(container.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                Text text = textGO.AddComponent<Text>();
                text.text = "My Custom App";
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.fontSize = 24;
                textRT.anchorMin = new Vector2(0.5f, 0.9f);
                textRT.anchorMax = new Vector2(0.5f, 0.9f);
                textRT.anchoredPosition = Vector2.zero;
                textRT.sizeDelta = new Vector2(200, 50);

                // Button
                GameObject buttonGO = new GameObject("ClickMeButton");
                buttonGO.transform.SetParent(container.transform, false);
                RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = Color.gray;
                Button button = buttonGO.AddComponent<Button>();

                // Button text
                GameObject buttonTextGO = new GameObject("ButtonText");
                buttonTextGO.transform.SetParent(buttonGO.transform, false);
                RectTransform buttonTextRT = buttonTextGO.AddComponent<RectTransform>();
                Text buttonText = buttonTextGO.AddComponent<Text>();
                buttonText.text = "Click Me";
                buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.black;
                buttonText.fontSize = 18;
                buttonTextRT.anchorMin = Vector2.zero;
                buttonTextRT.anchorMax = Vector2.one;
                buttonTextRT.offsetMin = Vector2.zero;
                buttonTextRT.offsetMax = Vector2.zero;

                // Button position
                buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRT.anchoredPosition = Vector2.zero;
                buttonRT.sizeDelta = new Vector2(160, 50);

                void FuncThatCallsFunc() => Click();
                button.onClick.AddListener((UnityAction)FuncThatCallsFunc);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("BuildCustomAppUI: Exception occurred: " + ex);
            }
        }

        public void Click()
        {
            MelonLogger.Msg("ButtonClickHandler: Clicked!");
        }




        private bool ModifyAppIcon(string targetIconGameObjectName, string targetLabelText, MelonLogger.Instance logger)
        {
            GameObject appIconsParent = GameObject.Find("Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/AppIcons/");
            if (appIconsParent == null)
            {
                logger?.Error("Could not find 'AppIcons' parent.");
                return false;
            }

            List<Transform> icons = new List<Transform>();
            Transform parentTransform = appIconsParent.transform;

            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                if (child != null)
                {
                    icons.Add(child);
                }
            }

            if (icons.Count == 0)
            {
                logger?.Error("Found no GameObjects under AppIcons.");
                return false;
            }

            Transform lastIconTransform = icons.LastOrDefault();
            if (lastIconTransform == null)
            {
                logger?.Error("Could not get the last icon transform.");
                return false;
            }

            GameObject lastIcon = lastIconTransform.gameObject;
            Transform labelTransform = lastIcon.transform.Find("Label");
            Text labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
            if (labelText != null)
            {
                labelText.text = targetLabelText;
            }
            else
            {
                logger?.Warning("Could not find Label component on target icon '" + lastIcon.name + "'.");
            }

            lastIcon.name = targetIconGameObjectName;
            return ChangeAppIconImage(lastIcon, logger);
        }

        private bool ChangeAppIconImage(GameObject appIconGameObject, MelonLogger.Instance logger)
        {
            if (appIconGameObject == null) return false;
            Transform imageTransform = appIconGameObject.transform.Find("Mask/Image");
            Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            if (image == null)
            {
                if (logger != null)
                {
                    logger.Warning("Could not find 'Mask/Image' component on '" + appIconGameObject.name + "'.");
                }
                return false;
            }
            Texture2D customTexture = LoadCustomTexture(logger);
            if (customTexture != null)
            {
                Sprite customSprite = Sprite.Create(customTexture, new Rect(0f, 0f, customTexture.width, customTexture.height), new Vector2(0.5f, 0.5f));
                if (customSprite != null)
                {
                    image.sprite = customSprite;
                    return true;
                }
                else
                {
                    Object.Destroy(customTexture);
                }
            }
            return true; // Proceed with default image if custom fails
        }

        private Texture2D LoadCustomTexture(MelonLogger.Instance logger)
        {
            string iconPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SilkroadIcon.png");
            if (!File.Exists(iconPath))
            {
                if (logger != null)
                {
                    logger.Error("File not found: '" + iconPath + "'");
                }
                return null;
            }
            try
            {
                byte[] imageData = File.ReadAllBytes(iconPath);
                Texture2D texture = new Texture2D(2, 2);
                if (ImageConversion.LoadImage(texture, imageData, false))
                {
                    return texture;
                }
                else
                {
                    if (logger != null)
                    {
                        logger.Error("ImageConversion.LoadImage returned false for '" + iconPath + "'.");
                    }
                    Object.Destroy(texture);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("Exception loading '" + iconPath + "': " + ex.Message);
                }
                return null;
            }
        }

    }
}
