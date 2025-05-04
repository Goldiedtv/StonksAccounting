using MelonLoader;
using HarmonyLib;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader.Utils;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections;
using System.Linq;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;
using Il2CppSystem.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices.ComTypes;
using System.Xml;
using Newtonsoft.Json;
using UnityEngine.Playables;
using Plot = ScottPlot.Plot;
using Il2Cpp;

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
        public const string Version = "0.5";
        public const string DownloadLink = null;
    }
    public class StonksAccountingMod : MelonMod
    {
        public static void SaveData()
        {
            try
            {
                MelonLogger.Msg($"Saving JSON...");
                string json = JsonConvert.SerializeObject(_accountingData, Newtonsoft.Json.Formatting.Indented);
                string dataPath = Il2CppSystem.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "stonksData.json");
                Il2CppSystem.IO.File.WriteAllText(dataPath, json);
                MelonLogger.Msg($"JSON Saved!");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error saving data: {ex.Message}");
            }
        }
        public static AccountingData LoadData()
        {
            try
            {
                MelonLogger.Msg($"Loading JSON...");
                string dataPath = Il2CppSystem.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "stonksData.json");
                string jsonFromFile = Il2CppSystem.IO.File.ReadAllText(dataPath);
                var loadedData = JsonConvert.DeserializeObject<AccountingData>(jsonFromFile);
                MelonLogger.Msg($"JSON Loaded!");
                return loadedData ?? new AccountingData();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading data: {ex.Message}");
                return new AccountingData();
            }
        }
        public static bool LoadDataExists()
        {
            string dataPath = Il2CppSystem.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "stonksData.json");
            return Il2CppSystem.IO.File.Exists(dataPath);
        }

        //MoneyManager.LastCalculatedNetworth Koko netwörtti?
        //MoneyManager.LifetimeEarnings vai tää?

        //MoneyManager.cashBalance käteinen
        //MoneyManager.onlineBalance pankissa

        public static AccountingData _accountingData = new AccountingData();

        public static MoneyManager? _moneyManager;
        public bool _isMoneyInit = false;

        //TimeManager.ElapsedDays This is the current day number
        [HarmonyPatch(typeof(TimeManager), "FastForwardToWakeTime")]
        public static class TimeManager_FastForwardToWakeTime_Patch
        {
            public static void Prefix(TimeManager __instance)
            {
                MelonLogger.Msg($"FastForwardToWakeTime! Today is {__instance.ElapsedDays}"); //THIS IS FINE WAY TO END/START DAY (untill I find where's the proper "endDay" call)

                if (__instance.ElapsedDays > _accountingData.CurrentDayTransaction.dayNumber)
                {
                    _accountingData.CurrentDayTransaction.endCash = _moneyManager.cashInstance.Balance;
                    _accountingData.CurrentDayTransaction.endOnline = _moneyManager.onlineBalance;

                    _accountingData.TransactionHistory[_accountingData.CurrentDayTransaction.dayNumber] = _accountingData.CurrentDayTransaction;
                    AccountingTransactions transactions = new AccountingTransactions
                    {
                        startCash = _moneyManager.cashInstance.Balance,
                        startOnline = _moneyManager.onlineBalance,
                        dayNumber = __instance.ElapsedDays
                    };
                    _accountingData.CurrentDayTransaction = transactions;

                    MelonLogger.Msg($"ElapsedDays > currentDayTransactionDay! Adding old one to history and creating a new one! Our history is {_accountingData.TransactionHistory.Count} long.");
                }
                else
                    MelonLogger.Msg($"ElapsedDays is not greater than currentDayTransactionDay, not adding to history yet!");

                SaveData();
            }
        }

        [HarmonyPatch(typeof(TimeManager), "EndSleep")]
        public static class TimeManager_EndSleep_Patch
        {
            public static void Prefix(TimeManager __instance)
            {
                MelonLogger.Msg($"EndSleep!");
            }
        }

        [HarmonyPatch(typeof(SavePoint), "Save")]
        public static class SavePoint_Save_Patch
        {
            public static void Prefix(SavePoint __instance)
            {
                MelonLogger.Msg($"Saving!");
                SaveData();
            }
        }

        [HarmonyPatch(typeof(Phone), "SetIsOpen")]
        public static class Phone_SetIsOpen_Patch
        {
            public static bool Prefix(Phone __instance, bool o)
            {
                if (o)
                {
                    MelonLogger.Msg($"Phone is open!");
                    updateStonks = true;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SupplierStash), "RemoveCash")]
        public static class SupplierStash_RemoveCash_Patch
        {
            public static bool Prefix(SupplierStash __instance, float amount)
            {
                MelonLogger.Msg($"Supplier remove cash! Cash: {amount}");

                _accountingData.CurrentDayTransaction.cashLoss -= amount;

                return true;
            }
        }

        //TODO There is also ReceiveOnlineTransaction method!
        [HarmonyPatch(typeof(MoneyManager), "CreateOnlineTransaction")]
        public static class MoneyManager_CreateOnlineTransaction_Patch
        {
            public static bool Prefix(MoneyManager __instance, string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
            {
                MelonLogger.Msg($"Online Transaction! Name: {_transaction_Name}, amount: {_unit_Amount}, quantity: {_quantity}, note: {_transaction_Note}");
                if (_unit_Amount > 0)
                {
                    _accountingData.CurrentDayTransaction.onlineGain += _unit_Amount;
                }
                else
                {
                    _accountingData.CurrentDayTransaction.onlineLoss += _unit_Amount;
                }

                return true;
            }
        }
        [HarmonyPatch(typeof(MoneyManager), "ReceiveOnlineTransaction")]
        public static class MoneyManager_ReceiveOnlineTransaction_Patch
        {
            public static bool Prefix(MoneyManager __instance, string _transaction_Name, float _unit_Amount, float _quantity, string _transaction_Note)
            {
                MelonLogger.Msg($"RECEIVE Online Transaction! Name: {_transaction_Name}, amount: {_unit_Amount}, quantity: {_quantity}, note: {_transaction_Note}");
                return true;
            }
        }

        [HarmonyPatch(typeof(MoneyManager), "ChangeCashBalance")]
        public static class oneyManager_ChangeCashBalance_Patch
        {
            public static bool Prefix(MoneyManager __instance, float change, bool visualizeChange, bool playCashSound)
            {
                MelonLogger.Msg($"Cash Balance Change! Change: {change}, visualizeChange: {visualizeChange}, playSound {playCashSound}");

                if (change > 0)
                {
                    _accountingData.CurrentDayTransaction.cashGain += change;
                }
                else
                {
                    _accountingData.CurrentDayTransaction.cashLoss += change;
                }

                return true;
            }
        }

        private bool _isInTargetScene;
        private GameObject? _player;
        private bool _iconModified;
        private bool _appCreated;
        private bool _initializationCoroutineStarted;
        private GameObject? _myCustomAppPanel;

        private GameObject? _customAppContainer;
        private static bool updateStonks = false;

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

            if (updateStonks)
            {
                if (_customAppContainer != null)
                {
                    LoggerInstance.Msg("Rebuilding UI in existing panel '" + _customAppContainer.name + "'...");
                    HideDefaultAppUI(_customAppContainer);
                    BuildCustomAppUI(_customAppContainer);
                }
                updateStonks = false;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                LoggerInstance.Msg($"We have {_accountingData.CurrentDayTransaction.cashGain} cashGain and {_accountingData.CurrentDayTransaction.cashLoss} cashLoss. Totalling: {_accountingData.CurrentDayTransaction.cashGain + _accountingData.CurrentDayTransaction.cashLoss}");
                LoggerInstance.Msg($"We have {_accountingData.CurrentDayTransaction.onlineGain} onlineGain and {_accountingData.CurrentDayTransaction.onlineLoss} onlineLoss. Totalling: {_accountingData.CurrentDayTransaction.onlineGain + _accountingData.CurrentDayTransaction.onlineLoss}");
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
                GameObject? container = containerTransform != null ? containerTransform.gameObject : null;
                if (container != null)
                {
                    _customAppContainer = container;
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

            var moneyManagerObject = GameObject.Find("@Money");
            _moneyManager = moneyManagerObject.GetComponent<MoneyManager>();

            LoggerInstance.Msg($"We have {_moneyManager.cashInstance.Balance} in CASH and {_moneyManager.onlineBalance} in BANK. Total Value {_moneyManager.LastCalculatedNetworth}. LifetimeEarnings: {_moneyManager.LifetimeEarnings}");

            if (LoadDataExists())
            {
                LoggerInstance.Msg("Loading data...");
                _accountingData = LoadData();
                LoggerInstance.Msg($"Loaded data successfully. We have {_accountingData.TransactionHistory.Count} days of Accounting Data");
            }
            else
            {
                LoggerInstance.Msg("No data found, creating new data.");
                var timeManagerObject = GameObject.Find("@Time");
                var timeManager = timeManagerObject.GetComponent<TimeManager>();
                _accountingData.CashBalance = _moneyManager.cashInstance.Balance;
                _accountingData.OnlineBalance = _moneyManager.onlineBalance;
                AccountingTransactions transactions = new AccountingTransactions
                {
                    startCash = _moneyManager.cashInstance.Balance,
                    startOnline = _moneyManager.onlineBalance,
                    dayNumber = timeManager.ElapsedDays
                };
                _accountingData.CurrentDayTransaction = transactions;
                LoggerInstance.Msg("Data Created!");
            }
        }

        private void EnsureAppPanelIsSetup(GameObject appPanel)
        {
            if (!_appCreated)
            {
                GameObject? container = appPanel?.transform.Find("Container")?.gameObject;
                if (container != null && container.transform.childCount < 2)
                {
                    LoggerInstance.Msg("Rebuilding UI in existing panel '" + appPanel?.name + "'...");
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

        private byte[]? GenerateGraph()
        {
            var plt = new Plot();
            var xList = new List<double>();
            var yList = new List<double>();
            // System.Random r = new System.Random();
            // for (int i = 1; i < 8; i++)
            // {
            //     xList.Add(i);
            //     yList.Add(r.Next(1000, 2000));
            // }

            if (_accountingData == null)
                return null;

            if (_accountingData.TransactionHistory.Count == 0)
            {
                xList.Add(_accountingData.CurrentDayTransaction.dayNumber);
                float total = 0;
                if (graphState == 0)
                    total = _accountingData.CurrentDayTransaction.cashGain + _accountingData.CurrentDayTransaction.cashLoss + _accountingData.CurrentDayTransaction.onlineGain + _accountingData.CurrentDayTransaction.onlineLoss;
                else
                    total = _accountingData.CurrentDayTransaction.startCash + _accountingData.CurrentDayTransaction.startOnline + _accountingData.CurrentDayTransaction.cashGain + _accountingData.CurrentDayTransaction.cashLoss + _accountingData.CurrentDayTransaction.onlineGain + _accountingData.CurrentDayTransaction.onlineLoss;

                yList.Add(total);
            }
            else
            {
                Dictionary<int, float> data = new Dictionary<int, float>();
                if (graphState == 0)
                    data = _accountingData.GetSevenDayTotalProfitPerDayForGraph();
                else
                    data = _accountingData.GetSevenDayTotalMoneyPerDayForGraph();
                var orderedTransactions = data
                    .OrderByDescending(x => x.Key)
                    .ToList(); // Convert to a list to preserve the order

                foreach (var d in orderedTransactions)
                {
                    xList.Add(d.Key);
                    yList.Add(d.Value);
                }
            }

            var scatter = plt.Add.Scatter(xList, yList);
            // plt.Grid.XAxis.IsVisible = false;
            // plt.Grid.YAxis.IsVisible = true;
            scatter.Smooth = true;

            plt.XLabel("Day", 30);
            if (graphState == 0)
                plt.YLabel("Profits", 30);
            else
                plt.YLabel("Total Money", 30);
            
            plt.Grid.XAxisStyle.IsVisible = false;
            plt.Grid.YAxisStyle.IsVisible = true;

            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);

            scatter.LineWidth = 5;
            scatter.MarkerSize = 10;
            var bmap = plt.GetImage(600, 400);
            // string savePath = Il2CppSystem.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "plot.png");
            // bmap.Save(savePath);
            return bmap.GetImageBytes();
        }

        bool showGraph = false;
        int graphState = 0;

        private void BuildCustomAppUITexts(GameObject container)
        {
            LoggerInstance.Msg("BuildCustomAppUI: Starting setup.");

                // Background
                GameObject bgGO = new GameObject("AppBackground");
                bgGO.transform.SetParent(container.transform, false);
                RectTransform bgRT = bgGO.AddComponent<RectTransform>();
                Image bg = bgGO.AddComponent<Image>();
                bg.color = Color.white;
                bgRT.anchorMin = new Vector2(0.0f, 0.0f);
                bgRT.anchorMax = new Vector2(1.0f, 1.0f);
                bgRT.anchoredPosition = Vector2.zero;
                bgRT.sizeDelta = new Vector2(100, 100);

                // Title Text
                GameObject textGO = new GameObject("AppTitleText");
                textGO.transform.SetParent(container.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                Text text = textGO.AddComponent<Text>();
                text.text = "S.T.O.N.K.S. - Accounting";
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.black;
                text.fontSize = 40;
                textRT.anchorMin = new Vector2(0.5f, 0.95f);
                textRT.anchorMax = new Vector2(0.5f, 0.95f);
                textRT.anchoredPosition = Vector2.zero;
                textRT.sizeDelta = new Vector2(500, 50);


                float _cashGain = _accountingData.CurrentDayTransaction.cashGain;
                float _cashLoss = _accountingData.CurrentDayTransaction.cashLoss;
                float _onlineGain = _accountingData.CurrentDayTransaction.onlineGain;
                float _onlineLoss = _accountingData.CurrentDayTransaction.onlineLoss;

                string negativeMark = "-";

                // TodayCashGains Text
                GameObject CashGainGO = new GameObject("TodayCashGainText");
                CashGainGO.transform.SetParent(container.transform, false);
                RectTransform CashGainRT = CashGainGO.AddComponent<RectTransform>();
                Text CashGain = CashGainGO.AddComponent<Text>();
                CashGain.text = $"Cash Today:\n+{_cashGain}$\n{((_cashLoss == 0) ? negativeMark : "")}{_cashLoss}$\n= {_cashGain + _cashLoss}$";
                CashGain.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                CashGain.alignment = TextAnchor.MiddleRight;
                CashGain.color = Color.black;
                CashGain.fontSize = 35;
                CashGainRT.anchorMin = new Vector2(0.1f, 0.70f);
                CashGainRT.anchorMax = new Vector2(0.1f, 0.70f);
                CashGainRT.anchoredPosition = Vector2.zero;
                CashGainRT.sizeDelta = new Vector2(300, 300);

                // OnlineGains Text
                GameObject OnlineGainGO = new GameObject("TodayOnlineGainText");
                OnlineGainGO.transform.SetParent(container.transform, false);
                RectTransform OnlineGainRT = OnlineGainGO.AddComponent<RectTransform>();
                Text OnlineGain = OnlineGainGO.AddComponent<Text>();
                OnlineGain.text = $"Online Today:\n+{_onlineGain}$\n{((_onlineLoss == 0) ? negativeMark : "")}{_onlineLoss}$\n= {_onlineGain + _onlineLoss}$";
                OnlineGain.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                OnlineGain.alignment = TextAnchor.MiddleRight;
                OnlineGain.color = Color.black;
                OnlineGain.fontSize = 35;
                OnlineGainRT.anchorMin = new Vector2(0.45f, 0.70f);
                OnlineGainRT.anchorMax = new Vector2(0.45f, 0.70f);
                OnlineGainRT.anchoredPosition = Vector2.zero;
                OnlineGainRT.sizeDelta = new Vector2(300, 300);

                // TotalGains Textd
                GameObject TotalGainGO = new GameObject("TodayTotalGainText");
                TotalGainGO.transform.SetParent(container.transform, false);
                RectTransform TotalGainRT = TotalGainGO.AddComponent<RectTransform>();
                Text TotalGain = TotalGainGO.AddComponent<Text>();
                TotalGain.text = $"Total Today:\n+{_onlineGain + _cashGain}$\n{((_onlineLoss + _cashLoss == 0) ? negativeMark : "")}{_onlineLoss + _cashLoss}$\n= {(_onlineGain + _cashGain) + (_onlineLoss + _cashLoss)}$";
                TotalGain.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                TotalGain.alignment = TextAnchor.MiddleRight;
                TotalGain.color = Color.black;
                TotalGain.fontSize = 35;
                TotalGainRT.anchorMin = new Vector2(0.8f, 0.70f);
                TotalGainRT.anchorMax = new Vector2(0.8f, 0.70f);
                TotalGainRT.anchoredPosition = Vector2.zero;
                TotalGainRT.sizeDelta = new Vector2(300, 300);

                // SevenDayTotal Text
                float _cashGain7 = _accountingData.GetSevenDayCash(true);
                float _cashLoss7 = _accountingData.GetSevenDayCash(false);
                float _onlineGain7 = _accountingData.GetSevenDayOnline(true);
                float _onlineLoss7 = _accountingData.GetSevenDayOnline(false);
                string disclaimer = (_accountingData.TransactionHistory.Count < 6) ? $"({_accountingData.TransactionHistory.Count + 1} in history)" : "";

                GameObject SevenDayTotalGO = new GameObject("SevenDayTotalText");
                SevenDayTotalGO.transform.SetParent(container.transform, false);
                RectTransform SevenDayTotalRT = SevenDayTotalGO.AddComponent<RectTransform>();
                Text SevenDayTotal = SevenDayTotalGO.AddComponent<Text>();
                SevenDayTotal.text = $"7 day Total{disclaimer}:\n+{_cashGain7 + _onlineGain7}$\n{((_cashLoss7 + _onlineLoss7 == 0) ? negativeMark : "")}{_onlineLoss7 + _cashLoss7}$\n= {(_onlineGain7 + _cashGain7) + (_onlineLoss7 + _cashLoss7)}$";
                SevenDayTotal.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                SevenDayTotal.alignment = TextAnchor.MiddleRight;
                SevenDayTotal.color = Color.black;
                SevenDayTotal.fontSize = 35;
                SevenDayTotalRT.anchorMin = new Vector2(0.3f, 0.30f);
                SevenDayTotalRT.anchorMax = new Vector2(0.3f, 0.30f);
                SevenDayTotalRT.anchoredPosition = Vector2.zero;
                SevenDayTotalRT.sizeDelta = new Vector2(300, 300);

                GameObject GrandTotalGO = new GameObject("GrandTotalText");
                GrandTotalGO.transform.SetParent(container.transform, false);
                RectTransform GrandTotalRT = GrandTotalGO.AddComponent<RectTransform>();
                Text GrandTotal = GrandTotalGO.AddComponent<Text>();
                GrandTotal.text = $"Grand Total:\nWIP";
                GrandTotal.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                GrandTotal.alignment = TextAnchor.MiddleRight;
                GrandTotal.color = Color.black;
                GrandTotal.fontSize = 35;
                GrandTotalRT.anchorMin = new Vector2(0.7f, 0.30f);
                GrandTotalRT.anchorMax = new Vector2(0.7f, 0.30f);
                GrandTotalRT.anchoredPosition = Vector2.zero;
                GrandTotalRT.sizeDelta = new Vector2(300, 300);

                // Button
                GameObject buttonGO = new GameObject("ShowGraphButton");
                buttonGO.transform.SetParent(container.transform, false);
                RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = Color.gray;
                Button button = buttonGO.AddComponent<Button>();

                // Button text
                GameObject buttonTextGO = new GameObject("ShowGraphButtonText");
                buttonTextGO.transform.SetParent(buttonGO.transform, false);
                RectTransform buttonTextRT = buttonTextGO.AddComponent<RectTransform>();
                Text buttonText = buttonTextGO.AddComponent<Text>();
                buttonText.text = "Graph";
                buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.black;
                buttonText.fontSize = 18;
                buttonTextRT.anchorMin = Vector2.zero;
                buttonTextRT.anchorMax = Vector2.one;
                buttonTextRT.offsetMin = Vector2.zero;
                buttonTextRT.offsetMax = Vector2.zero;

                // Button position
                buttonRT.anchorMin = new Vector2(0.5f, 0.1f);
                buttonRT.anchorMax = new Vector2(0.5f, 0.1f);
                buttonRT.anchoredPosition = Vector2.zero;
                buttonRT.sizeDelta = new Vector2(160, 50);

                void FuncThatCallsFunc() => ClickToGraph();
                button.onClick.AddListener((UnityAction)FuncThatCallsFunc);
        }
        private void BuildCustomAppUIGraph(GameObject container)
        {
                // Background
                GameObject bgGO = new GameObject("AppBackground");
                bgGO.transform.SetParent(container.transform, false);
                RectTransform bgRT = bgGO.AddComponent<RectTransform>();
                Image bg = bgGO.AddComponent<Image>();
                bg.color = Color.white;
                bgRT.anchorMin = new Vector2(0.0f, 0.0f);
                bgRT.anchorMax = new Vector2(1.0f, 1.0f);
                bgRT.anchoredPosition = Vector2.zero;
                bgRT.sizeDelta = new Vector2(100, 100);

                // Graph
                GameObject graphGO = new GameObject("GraphCustom");
                graphGO.transform.SetParent(container.transform, false);
                RectTransform graphRT = graphGO.AddComponent<RectTransform>();
                Image graph = graphGO.AddComponent<Image>();
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(GenerateGraph());
                graph.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                graphRT.anchorMin = new Vector2(0.30f, 0.4f);
                graphRT.anchorMax = new Vector2(0.75f, 0.7f);
                graphRT.anchoredPosition = Vector2.zero;
                graphRT.sizeDelta = new Vector2(600, 400);

                // Button
                GameObject buttonGO = new GameObject("ShowTextshButton");
                buttonGO.transform.SetParent(container.transform, false);
                RectTransform buttonRT = buttonGO.AddComponent<RectTransform>();
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = Color.gray;
                Button button = buttonGO.AddComponent<Button>();

                // Button text
                GameObject buttonTextGO = new GameObject("ShowTextsButtonText");
                buttonTextGO.transform.SetParent(buttonGO.transform, false);
                RectTransform buttonTextRT = buttonTextGO.AddComponent<RectTransform>();
                Text buttonText = buttonTextGO.AddComponent<Text>();
                buttonText.text = "Texts";
                buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = Color.black;
                buttonText.fontSize = 18;
                buttonTextRT.anchorMin = Vector2.zero;
                buttonTextRT.anchorMax = Vector2.one;
                buttonTextRT.offsetMin = Vector2.zero;
                buttonTextRT.offsetMax = Vector2.zero;

                // Button position
                buttonRT.anchorMin = new Vector2(0.5f, 0.1f);
                buttonRT.anchorMax = new Vector2(0.5f, 0.1f);
                buttonRT.anchoredPosition = Vector2.zero;
                buttonRT.sizeDelta = new Vector2(160, 50);

                                // Button 2
                GameObject buttonGO2 = new GameObject("ShowGraph2Button");
                buttonGO2.transform.SetParent(container.transform, false);
                RectTransform buttonRT2 = buttonGO2.AddComponent<RectTransform>();
                Image buttonImage2 = buttonGO2.AddComponent<Image>();
                buttonImage2.color = Color.gray;
                Button button2 = buttonGO2.AddComponent<Button>();

                // Button text 2
                GameObject buttonTextGO2 = new GameObject("ShowGraphButtonText");
                buttonTextGO2.transform.SetParent(buttonGO2.transform, false);
                RectTransform buttonTextRT2 = buttonTextGO2.AddComponent<RectTransform>();
                Text buttonText2 = buttonTextGO2.AddComponent<Text>();
                buttonText2.text = "Next Graph";
                buttonText2.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                buttonText2.alignment = TextAnchor.MiddleCenter;
                buttonText2.color = Color.black;
                buttonText2.fontSize = 18;
                buttonTextRT2.anchorMin = Vector2.zero;
                buttonTextRT2.anchorMax = Vector2.one;
                buttonTextRT2.offsetMin = Vector2.zero;
                buttonTextRT2.offsetMax = Vector2.zero;

                // Button position 2
                buttonRT2.anchorMin = new Vector2(0.7f, 0.1f);
                buttonRT2.anchorMax = new Vector2(0.7f, 0.1f);
                buttonRT2.anchoredPosition = Vector2.zero;
                buttonRT2.sizeDelta = new Vector2(160, 50);

                void FuncThatCallsFunc() => ClickToGraphCycle();
                button2.onClick.AddListener((UnityAction)FuncThatCallsFunc);

                void FuncThatCallsFunc2() => ClickToText();
                button.onClick.AddListener((UnityAction)FuncThatCallsFunc2);
        }

        private void BuildCustomAppUI(GameObject container)
        {
            try
            {
                if (showGraph)
                    BuildCustomAppUIGraph(container);
                else
                    BuildCustomAppUITexts(container);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("BuildCustomAppUI: Exception occurred: " + ex);
            }
        }

        public void ClickToGraphCycle()
        {
            MelonLogger.Msg("ButtonClickHandler: TO GRAPH!");
            graphState++;

            if (graphState>1)
                graphState = 0;

            if (_customAppContainer != null)
            {
                LoggerInstance.Msg("Rebuilding UI in existing panel '" + _customAppContainer.name + "'...");
                HideDefaultAppUI(_customAppContainer);
                BuildCustomAppUI(_customAppContainer);
            }
        }

        public void ClickToGraph()
        {
            MelonLogger.Msg("ButtonClickHandler: TO GRAPH!");
            showGraph = !showGraph;

            if (_customAppContainer != null)
            {
                LoggerInstance.Msg("Rebuilding UI in existing panel '" + _customAppContainer.name + "'...");
                HideDefaultAppUI(_customAppContainer);
                BuildCustomAppUI(_customAppContainer);
            }
            
        }
        public void ClickToText()
        {
            MelonLogger.Msg("ButtonClickHandler: TO TEXT!");
            showGraph = !showGraph;
            if (_customAppContainer != null)
            {
                LoggerInstance.Msg("Rebuilding UI in existing panel '" + _customAppContainer.name + "'...");
                HideDefaultAppUI(_customAppContainer);
                BuildCustomAppUI(_customAppContainer);
            }
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

            Transform? lastIconTransform = icons.LastOrDefault();
            if (lastIconTransform == null)
            {
                logger?.Error("Could not get the last icon transform.");
                return false;
            }

            GameObject lastIcon = lastIconTransform.gameObject;
            Transform labelTransform = lastIcon.transform.Find("Label");
            Text? labelText = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
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

        private bool ChangeAppIconImage(GameObject appIconGameObject, MelonLogger.Instance? logger)
        {
            if (appIconGameObject == null) return false;
            Transform imageTransform = appIconGameObject.transform.Find("Mask/Image");
            Image? image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            if (image == null)
            {
                if (logger != null)
                {
                    logger.Warning("Could not find 'Mask/Image' component on '" + appIconGameObject.name + "'.");
                }
                return false;
            }
            Texture2D? customTexture = LoadCustomTexture(logger);
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

        private Texture2D? LoadCustomTexture(MelonLogger.Instance? logger)
        {
            string iconPath = Il2CppSystem.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "stonks4.png");
            if (!Il2CppSystem.IO.File.Exists(iconPath))
            {
                if (logger != null)
                {
                    logger.Error("File not found: '" + iconPath + "'");
                }
                return null;
            }
            try
            {
                byte[] imageData = Il2CppSystem.IO.File.ReadAllBytes(iconPath);
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
