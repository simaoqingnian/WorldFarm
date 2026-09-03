using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeGameplayScene : MonoBehaviour
    {
        private const string SaveKey = "WorldFarm.PrototypeGameplay.v1";
        private const int SaveVersion = 1;

        private readonly PrototypeGame game = new PrototypeGame();
        private PrototypeTab currentTab;
        private Vector2 mainScroll;
        private string selectedCountryId = "cn";
        private string selectedBiomeId = "cn_dry_plain";
        private string selectedCropId = "cn_wheat";
        private float nextRefreshTime;

        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle warningStyle;
        private GUIStyle boxStyle;

        private enum PrototypeTab
        {
            Plots,
            Crops,
            Orders,
            Inventory,
            Log
        }

        private enum PrototypePlotStatus
        {
            Empty,
            Growing,
            Mature
        }

        private void Awake()
        {
            game.LoadOrCreate();
            EnsureSelection();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + 0.5f;
            game.RefreshPlotMaturity();
            EnsureSelection();
        }

        private void OnGUI()
        {
            EnsureStyles();
            EnsureSelection();

            GUILayout.BeginArea(new Rect(16f, 14f, Screen.width - 32f, Screen.height - 28f));
            DrawHeader();
            DrawTimeControls();
            DrawTabs();

            mainScroll = GUILayout.BeginScrollView(mainScroll, GUILayout.ExpandHeight(true));
            switch (currentTab)
            {
                case PrototypeTab.Plots:
                    DrawPlotsTab();
                    break;
                case PrototypeTab.Crops:
                    DrawCropsTab();
                    break;
                case PrototypeTab.Orders:
                    DrawOrdersTab();
                    break;
                case PrototypeTab.Inventory:
                    DrawInventoryTab();
                    break;
                case PrototypeTab.Log:
                    DrawLogTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            GUI.skin.label.fontSize = 22;
            GUI.skin.button.fontSize = 22;
            GUI.skin.box.fontSize = 20;
            GUI.skin.toggle.fontSize = 20;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.12f, 0.18f, 0.12f) },
                wordWrap = true
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.15f, 0.25f, 0.16f) },
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = new Color(0.12f, 0.14f, 0.12f) },
                wordWrap = true
            };

            mutedStyle = new GUIStyle(bodyStyle)
            {
                normal = { textColor = new Color(0.38f, 0.42f, 0.36f) }
            };

            warningStyle = new GUIStyle(bodyStyle)
            {
                normal = { textColor = new Color(0.62f, 0.25f, 0.08f) },
                fontStyle = FontStyle.Bold
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 8, 8),
                alignment = TextAnchor.UpperLeft
            };
        }

        private void DrawHeader()
        {
            var country = PrototypeCatalog.GetCountry(selectedCountryId);
            var reputation = game.GetCountryReputation(selectedCountryId);
            var level = game.GetCountryLevel(selectedCountryId);

            GUILayout.Label("WorldFarm M0 玩法验证版 / Gameplay Prototype", titleStyle);
            GUILayout.Label(
                string.Format(
                    "金币 Coins {0} | 探索 Explore {1} | 研究 Research {2} | 经验 XP {3} | 当前国家 {4} Lv.{5} 声望 {6}",
                    game.State.coins,
                    game.State.worldExplorationPoints,
                    game.State.researchPoints,
                    game.State.experience,
                    country != null ? country.DisplayName : selectedCountryId,
                    level,
                    reputation),
                bodyStyle);
            GUILayout.Label("现在时间 Now: " + FormatTime(game.NowSeconds) + " | 测试偏移 Test Offset: " + FormatDuration(game.State.testTimeOffsetSeconds), mutedStyle);
        }

        private void DrawTimeControls()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 分钟 / +1m", GUILayout.Height(42f)))
            {
                game.AdvanceTestTime(60);
            }

            if (GUILayout.Button("+10 分钟 / +10m", GUILayout.Height(42f)))
            {
                game.AdvanceTestTime(600);
            }

            if (GUILayout.Button("全部成熟 / Mature All", GUILayout.Height(42f)))
            {
                game.MatureAllGrowingPlots();
            }

            if (GUILayout.Button("重置原型 / Reset", GUILayout.Height(42f)))
            {
                game.ResetState();
                selectedCountryId = "cn";
                selectedBiomeId = "cn_dry_plain";
                selectedCropId = "cn_wheat";
                mainScroll = Vector2.zero;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            var tabLabels = new[] { "地块", "作物", "订单", "库存", "日志" };
            currentTab = (PrototypeTab)GUILayout.Toolbar((int)currentTab, tabLabels, GUILayout.Height(44f));
        }

        private void DrawPlotsTab()
        {
            DrawCountrySelector();
            DrawBiomeSelector();
            DrawCropSelector(true);

            var biome = PrototypeCatalog.GetBiome(selectedBiomeId);
            if (biome == null)
            {
                GUILayout.Label("没有可用地貌。", warningStyle);
                return;
            }

            var crop = PrototypeCatalog.GetCrop(selectedCropId);
            GUILayout.Label("地块槽 Plot Slots", sectionStyle);
            GUILayout.Label(biome.DisplayName + " | " + biome.Description, mutedStyle);

            var plots = game.GetPlotsForBiome(selectedBiomeId);
            for (var i = 0; i < plots.Count; i++)
            {
                DrawPlotCard(plots[i], biome, crop);
            }
        }

        private void DrawCountrySelector()
        {
            GUILayout.Label("国家 Country", sectionStyle);
            GUILayout.BeginVertical(boxStyle);

            var countries = PrototypeCatalog.Countries;
            for (var i = 0; i < countries.Length; i++)
            {
                var country = countries[i];
                var unlocked = game.IsCountryUnlocked(country.Id);
                var canUnlock = !unlocked && game.State.worldExplorationPoints >= country.RequiredExplorationPoints;
                var text = country.DisplayName + " / " + country.Id;
                if (unlocked)
                {
                    text += string.Format(" | Lv.{0} 声望 {1}", game.GetCountryLevel(country.Id), game.GetCountryReputation(country.Id));
                }
                else
                {
                    text += " | 未解锁 需要探索点 " + country.RequiredExplorationPoints;
                }

                GUI.enabled = unlocked || canUnlock;
                if (GUILayout.Button((country.Id == selectedCountryId ? "已选 " : "") + text, GUILayout.Height(38f)))
                {
                    if (unlocked)
                    {
                        selectedCountryId = country.Id;
                    }
                    else
                    {
                        game.UnlockCountry(country.Id);
                        selectedCountryId = country.Id;
                    }

                    EnsureSelection();
                    mainScroll = Vector2.zero;
                }
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private void DrawBiomeSelector()
        {
            GUILayout.Label("地貌 Biomes", sectionStyle);
            GUILayout.BeginVertical(boxStyle);

            var biomes = PrototypeCatalog.Biomes;
            for (var i = 0; i < biomes.Length; i++)
            {
                var biome = biomes[i];
                if (biome.CountryId != selectedCountryId)
                {
                    continue;
                }

                var unlocked = game.IsBiomeUnlocked(biome.Id);
                var label = biome.DisplayName + " | " + biome.ShortTags + " | 槽位 " + biome.SlotCount;
                if (!unlocked)
                {
                    label += " | 锁定 声望Lv." + biome.RequiredReputationLevel;
                }

                GUI.enabled = unlocked;
                if (GUILayout.Button((biome.Id == selectedBiomeId ? "已选 " : "") + label, GUILayout.Height(38f)))
                {
                    selectedBiomeId = biome.Id;
                    mainScroll = Vector2.zero;
                }
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private void DrawCropSelector(bool compact)
        {
            GUILayout.Label("播种作物 Crop Selection", sectionStyle);
            GUILayout.BeginVertical(boxStyle);

            var currentBiome = PrototypeCatalog.GetBiome(selectedBiomeId);
            var crops = PrototypeCatalog.Crops;
            for (var i = 0; i < crops.Length; i++)
            {
                var crop = crops[i];
                if (crop.CountryId != selectedCountryId)
                {
                    continue;
                }

                var unlocked = game.IsCropUnlocked(crop.Id);
                var balance = currentBiome != null ? PrototypeBalance.Calculate(currentBiome, crop) : PrototypeBalance.Empty;
                var label = string.Format(
                    "{0}{1} | {2} | {3} | {4}-{5}产量 | {6}",
                    crop.Id == selectedCropId ? "已选 " : "",
                    crop.DisplayName,
                    FormatDuration(crop.GrowthSeconds),
                    FormatAdaptation(balance.adaptation),
                    balance.minYield,
                    balance.maxYield,
                    unlocked ? crop.MutationHint : "锁定 声望Lv." + crop.RequiredReputationLevel);

                GUI.enabled = unlocked;
                if (GUILayout.Button(label, GUILayout.Height(compact ? 36f : 44f)))
                {
                    selectedCropId = crop.Id;
                }
            }

            GUI.enabled = true;
            GUILayout.EndVertical();
        }

        private void DrawPlotCard(PrototypePlotStateData plot, PrototypeBiomeDef biome, PrototypeCropDef selectedCrop)
        {
            GUILayout.BeginVertical(boxStyle);

            GUILayout.Label(plot.plotId + " | " + StatusText(plot.status), sectionStyle);
            if (plot.status == PrototypePlotStatus.Empty)
            {
                if (selectedCrop == null)
                {
                    GUILayout.Label("请选择可播种作物。", warningStyle);
                }
                else
                {
                    var balance = PrototypeBalance.Calculate(biome, selectedCrop);
                    GUILayout.Label(
                        string.Format(
                            "准备播种：{0} | 适应 {1} | 成熟 {2} | 预计产量 {3}-{4}",
                            selectedCrop.DisplayName,
                            FormatAdaptation(balance.adaptation),
                            FormatDuration(balance.growthSeconds),
                            balance.minYield,
                            balance.maxYield),
                        bodyStyle);
                    if (GUILayout.Button("播种 Plant " + selectedCrop.DisplayName, GUILayout.Height(40f)))
                    {
                        game.Plant(plot.plotId, selectedCrop.Id);
                    }
                }
            }
            else
            {
                var plantedCrop = PrototypeCatalog.GetCrop(plot.cropId);
                GUILayout.Label(
                    string.Format(
                        "{0} | 适应 {1} | 预计产量 {2}-{3}",
                        plantedCrop != null ? plantedCrop.DisplayName : plot.cropId,
                        FormatAdaptation(plot.adaptation),
                        plot.expectedMinYield,
                        plot.expectedMaxYield),
                    bodyStyle);
                GUILayout.Label("播种: " + FormatTime(plot.plantedAtSeconds) + " | 成熟: " + FormatTime(plot.matureAtSeconds), mutedStyle);

                if (plot.status == PrototypePlotStatus.Mature)
                {
                    if (GUILayout.Button("收获 Harvest", GUILayout.Height(40f)))
                    {
                        game.Harvest(plot.plotId);
                    }
                }
                else
                {
                    GUILayout.Label("剩余 Remaining: " + FormatDuration(Math.Max(0, plot.matureAtSeconds - game.NowSeconds)), warningStyle);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawCropsTab()
        {
            DrawCountrySelector();
            DrawBiomeSelector();

            GUILayout.Label("作物适应表 Crop Balance", sectionStyle);
            var biome = PrototypeCatalog.GetBiome(selectedBiomeId);
            if (biome == null)
            {
                GUILayout.Label("没有选中地貌。", warningStyle);
                return;
            }

            var crops = PrototypeCatalog.Crops;
            for (var i = 0; i < crops.Length; i++)
            {
                var crop = crops[i];
                if (crop.CountryId != selectedCountryId)
                {
                    continue;
                }

                var unlocked = game.IsCropUnlocked(crop.Id);
                var balance = PrototypeBalance.Calculate(biome, crop);

                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(crop.DisplayName + " / " + crop.Id + (unlocked ? "" : " | 锁定"), sectionStyle);
                GUILayout.Label(
                    string.Format(
                        "类别 {0} | 基础售价 {1} | 基础产量 {2} | 成熟 {3}",
                        crop.Category,
                        crop.BasePrice,
                        crop.BaseYield,
                        FormatDuration(crop.GrowthSeconds)),
                    bodyStyle);
                GUILayout.Label(
                    string.Format(
                        "当前地貌 {0}: {1} | 预计成熟 {2} | 预计产量 {3}-{4} | 变异概率 {5:P1}",
                        biome.DisplayName,
                        FormatAdaptation(balance.adaptation),
                        FormatDuration(balance.growthSeconds),
                        balance.minYield,
                        balance.maxYield,
                        balance.mutationChance),
                    bodyStyle);
                GUILayout.Label(unlocked ? crop.MutationHint : "需要国家声望 Lv." + crop.RequiredReputationLevel, unlocked ? mutedStyle : warningStyle);
                GUI.enabled = unlocked;
                if (GUILayout.Button("选择为播种作物 Select", GUILayout.Height(38f)))
                {
                    selectedCropId = crop.Id;
                    currentTab = PrototypeTab.Plots;
                    mainScroll = Vector2.zero;
                }

                GUI.enabled = true;
                GUILayout.EndVertical();
            }
        }

        private void DrawOrdersTab()
        {
            DrawCountrySelector();
            GUILayout.Label("订单 Orders", sectionStyle);

            var orders = PrototypeCatalog.Orders;
            for (var i = 0; i < orders.Length; i++)
            {
                var order = orders[i];
                if (!game.IsOrderVisible(order, selectedCountryId))
                {
                    continue;
                }

                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(order.DisplayName + " | " + order.Channel, sectionStyle);
                GUILayout.Label(order.Description, mutedStyle);
                GUILayout.Label("需求: " + FormatRequirements(order.Requirements), bodyStyle);
                GUILayout.Label(
                    string.Format(
                        "奖励: 金币 {0}, 经验 {1}, 探索 {2}, 研究 {3}, 声望 {4}",
                        order.RewardCoins,
                        order.RewardExperience,
                        order.RewardExplorationPoints,
                        order.RewardResearchPoints,
                        order.RewardCountryReputation),
                    bodyStyle);
                GUILayout.Label("完成次数 Completed: " + game.GetOrderCompletedCount(order.Id), mutedStyle);

                var canSubmit = game.CanSubmitOrder(order);
                GUI.enabled = canSubmit;
                if (GUILayout.Button(canSubmit ? "提交订单 Submit" : "库存不足 Missing Items", GUILayout.Height(40f)))
                {
                    game.SubmitOrder(order, selectedCountryId);
                    EnsureSelection();
                }

                GUI.enabled = true;
                GUILayout.EndVertical();
            }
        }

        private void DrawInventoryTab()
        {
            GUILayout.Label("库存 Inventory", sectionStyle);
            if (game.State.inventory.Count == 0)
            {
                GUILayout.Label("库存为空。先去地块页播种并收获。", mutedStyle);
                return;
            }

            for (var i = 0; i < game.State.inventory.Count; i++)
            {
                var stack = game.State.inventory[i];
                if (stack.count <= 0)
                {
                    continue;
                }

                var crop = PrototypeCatalog.GetCrop(stack.itemId);
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(game.GetItemDisplayName(stack.itemId) + " x" + stack.count, sectionStyle);
                if (crop != null)
                {
                    GUILayout.Label("基础售价 Base Price: " + crop.BasePrice + " | 用于订单、后续加工和育种。", bodyStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("出售 1 / Sell 1", GUILayout.Height(38f)))
                    {
                        game.SellItem(stack.itemId, 1);
                    }

                    if (GUILayout.Button("全部出售 / Sell All", GUILayout.Height(38f)))
                    {
                        game.SellItem(stack.itemId, stack.count);
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("原型材料，后续用于研究、认证或育种。", mutedStyle);
                }

                GUILayout.EndVertical();
            }
        }

        private void DrawLogTab()
        {
            GUILayout.Label("事件日志 Event Log", sectionStyle);
            for (var i = 0; i < game.State.logs.Count; i++)
            {
                var entry = game.State.logs[i];
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(FormatTime(entry.timeSeconds), mutedStyle);
                GUILayout.Label(entry.message, bodyStyle);
                GUILayout.EndVertical();
            }
        }

        private void EnsureSelection()
        {
            if (!game.IsCountryUnlocked(selectedCountryId))
            {
                selectedCountryId = "cn";
            }

            if (!game.IsBiomeUnlocked(selectedBiomeId) || PrototypeCatalog.GetBiome(selectedBiomeId).CountryId != selectedCountryId)
            {
                selectedBiomeId = game.GetFirstUnlockedBiomeId(selectedCountryId);
            }

            var crop = PrototypeCatalog.GetCrop(selectedCropId);
            if (crop == null || crop.CountryId != selectedCountryId || !game.IsCropUnlocked(selectedCropId))
            {
                selectedCropId = game.GetFirstUnlockedCropId(selectedCountryId);
            }
        }

        private string FormatRequirements(PrototypeRequirement[] requirements)
        {
            var result = string.Empty;
            for (var i = 0; i < requirements.Length; i++)
            {
                var requirement = requirements[i];
                if (i > 0)
                {
                    result += ", ";
                }

                result += string.Format(
                    "{0} x{1} (已有 {2})",
                    game.GetItemDisplayName(requirement.ItemId),
                    requirement.Count,
                    game.GetInventoryCount(requirement.ItemId));
            }

            return result;
        }

        private static string StatusText(PrototypePlotStatus status)
        {
            switch (status)
            {
                case PrototypePlotStatus.Empty:
                    return "空地 Empty";
                case PrototypePlotStatus.Growing:
                    return "生长中 Growing";
                case PrototypePlotStatus.Mature:
                    return "可收获 Mature";
                default:
                    return status.ToString();
            }
        }

        private static string FormatAdaptation(float value)
        {
            if (value >= 1.15f)
            {
                return "极佳 " + value.ToString("0.00");
            }

            if (value >= 0.90f)
            {
                return "适宜 " + value.ToString("0.00");
            }

            if (value >= 0.55f)
            {
                return "可试种 " + value.ToString("0.00");
            }

            if (value >= 0.25f)
            {
                return "逆境 " + value.ToString("0.00");
            }

            return "极限 " + value.ToString("0.00");
        }

        private static string FormatDuration(long seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1)
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)span.TotalHours, span.Minutes, span.Seconds);
            }

            return string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
        }

        private static string FormatTime(long epochSeconds)
        {
            if (epochSeconds <= 0)
            {
                return "--";
            }

            return DateTimeOffset.FromUnixTimeSeconds(epochSeconds).LocalDateTime.ToString("MM-dd HH:mm:ss");
        }

        [Serializable]
        private sealed class PrototypeGameState
        {
            public int version = SaveVersion;
            public int coins = 120;
            public int worldExplorationPoints;
            public int researchPoints;
            public int experience;
            public long testTimeOffsetSeconds;
            public List<PrototypeCountryProgressState> countries = new List<PrototypeCountryProgressState>();
            public List<PrototypePlotStateData> plots = new List<PrototypePlotStateData>();
            public List<PrototypeInventoryStack> inventory = new List<PrototypeInventoryStack>();
            public List<PrototypeOrderProgressState> orders = new List<PrototypeOrderProgressState>();
            public List<PrototypeLogEntry> logs = new List<PrototypeLogEntry>();
        }

        [Serializable]
        private sealed class PrototypeCountryProgressState
        {
            public string countryId;
            public bool unlocked;
            public int reputation;
        }

        [Serializable]
        private sealed class PrototypePlotStateData
        {
            public string plotId;
            public string biomeId;
            public PrototypePlotStatus status;
            public string cropId;
            public long plantedAtSeconds;
            public long matureAtSeconds;
            public float adaptation;
            public int expectedMinYield;
            public int expectedMaxYield;
        }

        [Serializable]
        private sealed class PrototypeInventoryStack
        {
            public string itemId;
            public int count;
        }

        [Serializable]
        private sealed class PrototypeOrderProgressState
        {
            public string orderId;
            public int completedCount;
        }

        [Serializable]
        private sealed class PrototypeLogEntry
        {
            public long timeSeconds;
            public string message;
        }

        private sealed class PrototypeGame
        {
            private static readonly int[] ReputationThresholds = { 0, 30, 80, 150, 260, 400, 580, 800, 1060, 1360, 1700 };

            public PrototypeGameState State { get; private set; }

            public long NowSeconds
            {
                get { return DateTimeOffset.Now.ToUnixTimeSeconds() + State.testTimeOffsetSeconds; }
            }

            public void LoadOrCreate()
            {
                var json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        State = JsonUtility.FromJson<PrototypeGameState>(json);
                    }
                    catch (Exception)
                    {
                        State = null;
                    }
                }

                if (State == null || State.version != SaveVersion)
                {
                    ResetState();
                    return;
                }

                NormalizeState();
                AddLog("读取原型存档 / Prototype save loaded.", false);
                Save();
            }

            public void ResetState()
            {
                State = new PrototypeGameState();
                NormalizeState();
                AddLog("原型存档已初始化 / Prototype state initialized.", false);
                Save();
            }

            public void Save()
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
                PlayerPrefs.Save();
            }

            public void AdvanceTestTime(int seconds)
            {
                State.testTimeOffsetSeconds += seconds;
                RefreshPlotMaturity();
                AddLog("测试时间前进 " + FormatDuration(seconds) + " / Advanced test time.");
                Save();
            }

            public void MatureAllGrowingPlots()
            {
                var changed = false;
                for (var i = 0; i < State.plots.Count; i++)
                {
                    var plot = State.plots[i];
                    if (plot.status != PrototypePlotStatus.Growing)
                    {
                        continue;
                    }

                    plot.matureAtSeconds = NowSeconds - 1;
                    plot.status = PrototypePlotStatus.Mature;
                    changed = true;
                }

                if (changed)
                {
                    AddLog("所有生长中地块已设为成熟 / All growing plots matured.");
                    Save();
                }
            }

            public bool RefreshPlotMaturity()
            {
                var now = NowSeconds;
                var changed = false;
                for (var i = 0; i < State.plots.Count; i++)
                {
                    var plot = State.plots[i];
                    if (plot.status == PrototypePlotStatus.Growing && now >= plot.matureAtSeconds)
                    {
                        plot.status = PrototypePlotStatus.Mature;
                        var crop = PrototypeCatalog.GetCrop(plot.cropId);
                        AddLog((crop != null ? crop.DisplayName : plot.cropId) + " 已成熟 / Crop matured.", false);
                        changed = true;
                    }
                }

                if (changed)
                {
                    Save();
                }

                return changed;
            }

            public bool Plant(string plotId, string cropId)
            {
                var plot = GetPlot(plotId);
                var crop = PrototypeCatalog.GetCrop(cropId);
                if (plot == null || crop == null || plot.status != PrototypePlotStatus.Empty || !IsCropUnlocked(cropId))
                {
                    return false;
                }

                var biome = PrototypeCatalog.GetBiome(plot.biomeId);
                if (biome == null || !IsBiomeUnlocked(biome.Id))
                {
                    return false;
                }

                var balance = PrototypeBalance.Calculate(biome, crop);
                var now = NowSeconds;

                plot.cropId = crop.Id;
                plot.status = PrototypePlotStatus.Growing;
                plot.plantedAtSeconds = now;
                plot.matureAtSeconds = now + balance.growthSeconds;
                plot.adaptation = balance.adaptation;
                plot.expectedMinYield = balance.minYield;
                plot.expectedMaxYield = balance.maxYield;

                AddLog(
                    string.Format(
                        "播种 {0} 到 {1}，适应 {2:0.00}，预计产量 {3}-{4} / Planted.",
                        crop.DisplayName,
                        biome.DisplayName,
                        balance.adaptation,
                        balance.minYield,
                        balance.maxYield));
                Save();
                return true;
            }

            public bool Harvest(string plotId)
            {
                var plot = GetPlot(plotId);
                if (plot == null || plot.status != PrototypePlotStatus.Mature)
                {
                    return false;
                }

                var crop = PrototypeCatalog.GetCrop(plot.cropId);
                var yield = UnityEngine.Random.Range(Mathf.Max(1, plot.expectedMinYield), Mathf.Max(1, plot.expectedMaxYield) + 1);
                AddInventory(plot.cropId, yield);

                AddLog(
                    string.Format(
                        "收获 {0} x{1}，适应 {2:0.00} / Harvested.",
                        crop != null ? crop.DisplayName : plot.cropId,
                        yield,
                        plot.adaptation));

                plot.status = PrototypePlotStatus.Empty;
                plot.cropId = string.Empty;
                plot.plantedAtSeconds = 0;
                plot.matureAtSeconds = 0;
                plot.adaptation = 0f;
                plot.expectedMinYield = 0;
                plot.expectedMaxYield = 0;

                Save();
                return true;
            }

            public bool CanSubmitOrder(PrototypeOrderDef order)
            {
                for (var i = 0; i < order.Requirements.Length; i++)
                {
                    var requirement = order.Requirements[i];
                    if (GetInventoryCount(requirement.ItemId) < requirement.Count)
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool SubmitOrder(PrototypeOrderDef order, string fallbackCountryId)
            {
                if (!CanSubmitOrder(order))
                {
                    return false;
                }

                for (var i = 0; i < order.Requirements.Length; i++)
                {
                    var requirement = order.Requirements[i];
                    RemoveInventory(requirement.ItemId, requirement.Count);
                }

                State.coins += order.RewardCoins;
                State.experience += order.RewardExperience;
                State.worldExplorationPoints += order.RewardExplorationPoints;
                State.researchPoints += order.RewardResearchPoints;

                if (order.RewardCountryReputation > 0)
                {
                    var countryId = string.IsNullOrEmpty(order.CountryId) ? fallbackCountryId : order.CountryId;
                    GetCountryProgress(countryId).reputation += order.RewardCountryReputation;
                }

                GetOrderProgress(order.Id).completedCount++;
                AddLog("提交订单：" + order.DisplayName + " / Order submitted.");
                Save();
                return true;
            }

            public bool SellItem(string itemId, int count)
            {
                var crop = PrototypeCatalog.GetCrop(itemId);
                if (crop == null || count <= 0 || GetInventoryCount(itemId) < count)
                {
                    return false;
                }

                RemoveInventory(itemId, count);
                var coins = crop.BasePrice * count;
                State.coins += coins;
                AddLog("出售 " + crop.DisplayName + " x" + count + "，获得金币 " + coins + " / Sold items.");
                Save();
                return true;
            }

            public bool IsCountryUnlocked(string countryId)
            {
                return GetCountryProgress(countryId).unlocked;
            }

            public bool UnlockCountry(string countryId)
            {
                var country = PrototypeCatalog.GetCountry(countryId);
                if (country == null)
                {
                    return false;
                }

                var progress = GetCountryProgress(countryId);
                if (progress.unlocked)
                {
                    return true;
                }

                if (State.worldExplorationPoints < country.RequiredExplorationPoints)
                {
                    return false;
                }

                State.worldExplorationPoints -= country.RequiredExplorationPoints;
                progress.unlocked = true;
                AddLog("解锁国家：" + country.DisplayName + " / Country unlocked.");
                Save();
                return true;
            }

            public bool IsBiomeUnlocked(string biomeId)
            {
                var biome = PrototypeCatalog.GetBiome(biomeId);
                if (biome == null || !IsCountryUnlocked(biome.CountryId))
                {
                    return false;
                }

                return GetCountryLevel(biome.CountryId) >= biome.RequiredReputationLevel;
            }

            public bool IsCropUnlocked(string cropId)
            {
                var crop = PrototypeCatalog.GetCrop(cropId);
                if (crop == null || !IsCountryUnlocked(crop.CountryId))
                {
                    return false;
                }

                return GetCountryLevel(crop.CountryId) >= crop.RequiredReputationLevel;
            }

            public bool IsOrderVisible(PrototypeOrderDef order, string selectedCountryId)
            {
                if (string.IsNullOrEmpty(order.CountryId))
                {
                    return true;
                }

                if (order.CountryId != selectedCountryId || !IsCountryUnlocked(order.CountryId))
                {
                    return false;
                }

                return GetCountryLevel(order.CountryId) >= order.RequiredReputationLevel;
            }

            public int GetCountryReputation(string countryId)
            {
                return GetCountryProgress(countryId).reputation;
            }

            public int GetCountryLevel(string countryId)
            {
                var reputation = GetCountryReputation(countryId);
                var level = 0;
                for (var i = 0; i < ReputationThresholds.Length; i++)
                {
                    if (reputation >= ReputationThresholds[i])
                    {
                        level = i;
                    }
                }

                return level;
            }

            public int GetInventoryCount(string itemId)
            {
                for (var i = 0; i < State.inventory.Count; i++)
                {
                    var stack = State.inventory[i];
                    if (stack.itemId == itemId)
                    {
                        return stack.count;
                    }
                }

                return 0;
            }

            public int GetOrderCompletedCount(string orderId)
            {
                return GetOrderProgress(orderId).completedCount;
            }

            public string GetItemDisplayName(string itemId)
            {
                var crop = PrototypeCatalog.GetCrop(itemId);
                if (crop != null)
                {
                    return crop.DisplayName;
                }

                return itemId;
            }

            public string GetFirstUnlockedBiomeId(string countryId)
            {
                for (var i = 0; i < PrototypeCatalog.Biomes.Length; i++)
                {
                    var biome = PrototypeCatalog.Biomes[i];
                    if (biome.CountryId == countryId && IsBiomeUnlocked(biome.Id))
                    {
                        return biome.Id;
                    }
                }

                return string.Empty;
            }

            public string GetFirstUnlockedCropId(string countryId)
            {
                for (var i = 0; i < PrototypeCatalog.Crops.Length; i++)
                {
                    var crop = PrototypeCatalog.Crops[i];
                    if (crop.CountryId == countryId && IsCropUnlocked(crop.Id))
                    {
                        return crop.Id;
                    }
                }

                return string.Empty;
            }

            public List<PrototypePlotStateData> GetPlotsForBiome(string biomeId)
            {
                var result = new List<PrototypePlotStateData>();
                for (var i = 0; i < State.plots.Count; i++)
                {
                    if (State.plots[i].biomeId == biomeId)
                    {
                        result.Add(State.plots[i]);
                    }
                }

                return result;
            }

            private void NormalizeState()
            {
                if (State.countries == null)
                {
                    State.countries = new List<PrototypeCountryProgressState>();
                }

                if (State.plots == null)
                {
                    State.plots = new List<PrototypePlotStateData>();
                }

                if (State.inventory == null)
                {
                    State.inventory = new List<PrototypeInventoryStack>();
                }

                if (State.orders == null)
                {
                    State.orders = new List<PrototypeOrderProgressState>();
                }

                if (State.logs == null)
                {
                    State.logs = new List<PrototypeLogEntry>();
                }

                for (var i = 0; i < PrototypeCatalog.Countries.Length; i++)
                {
                    var country = PrototypeCatalog.Countries[i];
                    var progress = GetCountryProgress(country.Id);
                    if (country.StartsUnlocked)
                    {
                        progress.unlocked = true;
                    }
                }

                for (var i = 0; i < PrototypeCatalog.Biomes.Length; i++)
                {
                    var biome = PrototypeCatalog.Biomes[i];
                    for (var slotIndex = 1; slotIndex <= biome.SlotCount; slotIndex++)
                    {
                        var plotId = biome.Id + "_" + slotIndex.ToString("00");
                        if (GetPlot(plotId) != null)
                        {
                            continue;
                        }

                        State.plots.Add(new PrototypePlotStateData
                        {
                            plotId = plotId,
                            biomeId = biome.Id,
                            status = PrototypePlotStatus.Empty,
                            cropId = string.Empty
                        });
                    }
                }

                for (var i = State.inventory.Count - 1; i >= 0; i--)
                {
                    if (State.inventory[i].count <= 0)
                    {
                        State.inventory.RemoveAt(i);
                    }
                }

                for (var i = 0; i < PrototypeCatalog.Orders.Length; i++)
                {
                    GetOrderProgress(PrototypeCatalog.Orders[i].Id);
                }
            }

            private PrototypePlotStateData GetPlot(string plotId)
            {
                for (var i = 0; i < State.plots.Count; i++)
                {
                    if (State.plots[i].plotId == plotId)
                    {
                        return State.plots[i];
                    }
                }

                return null;
            }

            private PrototypeCountryProgressState GetCountryProgress(string countryId)
            {
                for (var i = 0; i < State.countries.Count; i++)
                {
                    if (State.countries[i].countryId == countryId)
                    {
                        return State.countries[i];
                    }
                }

                var progress = new PrototypeCountryProgressState
                {
                    countryId = countryId,
                    unlocked = false,
                    reputation = 0
                };
                State.countries.Add(progress);
                return progress;
            }

            private PrototypeOrderProgressState GetOrderProgress(string orderId)
            {
                for (var i = 0; i < State.orders.Count; i++)
                {
                    if (State.orders[i].orderId == orderId)
                    {
                        return State.orders[i];
                    }
                }

                var progress = new PrototypeOrderProgressState
                {
                    orderId = orderId,
                    completedCount = 0
                };
                State.orders.Add(progress);
                return progress;
            }

            private void AddInventory(string itemId, int count)
            {
                if (count <= 0)
                {
                    return;
                }

                for (var i = 0; i < State.inventory.Count; i++)
                {
                    if (State.inventory[i].itemId == itemId)
                    {
                        State.inventory[i].count += count;
                        return;
                    }
                }

                State.inventory.Add(new PrototypeInventoryStack
                {
                    itemId = itemId,
                    count = count
                });
            }

            private void RemoveInventory(string itemId, int count)
            {
                for (var i = 0; i < State.inventory.Count; i++)
                {
                    var stack = State.inventory[i];
                    if (stack.itemId != itemId)
                    {
                        continue;
                    }

                    stack.count -= count;
                    if (stack.count <= 0)
                    {
                        State.inventory.RemoveAt(i);
                    }

                    return;
                }
            }

            private void AddLog(string message, bool save = true)
            {
                State.logs.Insert(0, new PrototypeLogEntry
                {
                    timeSeconds = NowSeconds,
                    message = message
                });

                while (State.logs.Count > 80)
                {
                    State.logs.RemoveAt(State.logs.Count - 1);
                }

                if (save)
                {
                    Save();
                }
            }
        }

        private static class PrototypeCatalog
        {
            public static readonly PrototypeCountryDef[] Countries =
            {
                new PrototypeCountryDef("cn", "中国 China", 0, true),
                new PrototypeCountryDef("jp", "日本 Japan", 100, false),
                new PrototypeCountryDef("fr", "法国 France", 160, false)
            };

            public static readonly PrototypeBiomeDef[] Biomes =
            {
                new PrototypeBiomeDef("cn_dry_plain", "cn", "华北旱田 North Dry Field", "干燥 / 温带 / 平原", "基础粮食地貌，适合小麦、玉米，也适合测试水稻耐旱低产。", 2, 0),
                new PrototypeBiomeDef("cn_paddy", "cn", "江南水田 Jiangnan Paddy", "湿润 / 水田 / 温带", "水稻核心地貌，产量稳定，后续接名产认证。", 2, 0),
                new PrototypeBiomeDef("cn_vegetable_bed", "cn", "城郊菜畦 Vegetable Bed", "肥沃 / 中水分 / 短周期", "高频收获地貌，适合白菜和新手订单。", 2, 0),
                new PrototypeBiomeDef("cn_terrace_hill", "cn", "丘陵梯田 Terrace Hill", "坡地 / 温润 / 排水快", "中期茶叶和坡地变异入口。", 1, 3),
                new PrototypeBiomeDef("jp_snow_paddy", "jp", "雪融水田 Snowmelt Paddy", "温凉 / 水田 / 雪融水", "日本水稻本土化和清香变异入口。", 2, 0),
                new PrototypeBiomeDef("jp_tea_hill", "jp", "山麓茶园 Foothill Tea Garden", "山地 / 林香 / 温润", "日本茶叶和清香加工线入口。", 1, 2),
                new PrototypeBiomeDef("fr_bordeaux_vineyard", "fr", "波尔多葡萄园 Bordeaux Vineyard", "葡萄园 / 砾石土 / 海雾", "法国葡萄名产认证和酒庄订单入口。", 2, 0)
            };

            public static readonly PrototypeCropDef[] Crops =
            {
                new PrototypeCropDef("cn_wheat", "cn", "中国小麦 Wheat", "wheat", "谷物 Grain", 3, 5, 120, 0, "水田低适应，后续可接水麦芽线索。", new[]
                {
                    new PrototypeAffinity("cn_dry_plain", 1.15f),
                    new PrototypeAffinity("cn_paddy", 0.45f),
                    new PrototypeAffinity("cn_vegetable_bed", 0.75f),
                    new PrototypeAffinity("cn_terrace_hill", 0.65f)
                }),
                new PrototypeCropDef("cn_rice", "cn", "中国水稻 Rice", "rice", "谷物 Grain", 4, 8, 300, 0, "旱田逆境可测试耐旱稻方向。", new[]
                {
                    new PrototypeAffinity("cn_dry_plain", 0.35f),
                    new PrototypeAffinity("cn_paddy", 1.20f),
                    new PrototypeAffinity("cn_vegetable_bed", 0.65f),
                    new PrototypeAffinity("cn_terrace_hill", 0.55f)
                }),
                new PrototypeCropDef("cn_cabbage", "cn", "中国白菜 Cabbage", "cabbage", "叶菜 Leaf", 3, 6, 90, 0, "高寒地貌后续可接霜心白菜方向。", new[]
                {
                    new PrototypeAffinity("cn_dry_plain", 0.75f),
                    new PrototypeAffinity("cn_paddy", 0.60f),
                    new PrototypeAffinity("cn_vegetable_bed", 1.15f),
                    new PrototypeAffinity("cn_terrace_hill", 0.70f)
                }),
                new PrototypeCropDef("cn_corn", "cn", "中国玉米 Corn", "corn", "谷物 Grain", 2, 12, 480, 1, "黑土地后续可接黑土甜玉米方向。", new[]
                {
                    new PrototypeAffinity("cn_dry_plain", 1.10f),
                    new PrototypeAffinity("cn_paddy", 0.45f),
                    new PrototypeAffinity("cn_vegetable_bed", 0.65f),
                    new PrototypeAffinity("cn_terrace_hill", 0.80f)
                }),
                new PrototypeCropDef("cn_tea", "cn", "中国茶叶 Tea", "tea", "茶叶 Herb", 2, 20, 1800, 3, "高原冷田后续可接雪芽茶方向。", new[]
                {
                    new PrototypeAffinity("cn_dry_plain", 0.40f),
                    new PrototypeAffinity("cn_paddy", 0.55f),
                    new PrototypeAffinity("cn_vegetable_bed", 0.50f),
                    new PrototypeAffinity("cn_terrace_hill", 1.20f)
                }),
                new PrototypeCropDef("jp_rice", "jp", "日本粳米 Japonica Rice", "rice", "谷物 Grain", 4, 10, 360, 0, "雪融水田适应高，后续可接雪泉稻。", new[]
                {
                    new PrototypeAffinity("jp_snow_paddy", 1.20f),
                    new PrototypeAffinity("jp_tea_hill", 0.50f)
                }),
                new PrototypeCropDef("jp_tea", "jp", "日本茶叶 Matcha Tea", "tea", "茶叶 Herb", 2, 24, 1500, 2, "山麓茶园适应高，后续接抹茶加工。", new[]
                {
                    new PrototypeAffinity("jp_snow_paddy", 0.55f),
                    new PrototypeAffinity("jp_tea_hill", 1.18f)
                }),
                new PrototypeCropDef("fr_grape", "fr", "法国葡萄 Grape", "grape", "水果 Fruit", 3, 18, 900, 0, "波尔多葡萄园触发名产认证和酒香方向。", new[]
                {
                    new PrototypeAffinity("fr_bordeaux_vineyard", 1.22f)
                })
            };

            public static readonly PrototypeOrderDef[] Orders =
            {
                new PrototypeOrderDef("daily_wheat", "日常 Daily", null, 0, "基础小麦补给 Wheat Supply", "消耗短周期粮食，提供稳定金币。", 85, 8, 0, 0, 0, new[]
                {
                    new PrototypeRequirement("cn_wheat", 3)
                }),
                new PrototypeOrderDef("daily_cabbage", "日常 Daily", null, 0, "白菜菜篮 Cabbage Basket", "测试菜畦高频收获。", 90, 8, 0, 0, 0, new[]
                {
                    new PrototypeRequirement("cn_cabbage", 4)
                }),
                new PrototypeOrderDef("cn_rice_cabbage", "中国 China", "cn", 0, "中国饭馆起步订单 Starter Meal", "推动中国声望和世界探索点的基础订单。", 180, 18, 10, 0, 14, new[]
                {
                    new PrototypeRequirement("cn_rice", 2),
                    new PrototypeRequirement("cn_cabbage", 2)
                }),
                new PrototypeOrderDef("cn_grain_supply", "中国 China", "cn", 1, "华北粮食补给 Grain Request", "解锁玉米后测试复合订单。", 250, 26, 18, 0, 20, new[]
                {
                    new PrototypeRequirement("cn_wheat", 4),
                    new PrototypeRequirement("cn_corn", 1)
                }),
                new PrototypeOrderDef("cn_tea_intro", "中国 China", "cn", 3, "丘陵茶饮试单 Tea Trial", "解锁茶叶后测试长周期高价值作物。", 420, 42, 28, 8, 34, new[]
                {
                    new PrototypeRequirement("cn_tea", 2)
                }),
                new PrototypeOrderDef("jp_rice_order", "日本 Japan", "jp", 0, "便当米饭订单 Bento Rice", "测试新国家基础订单。", 210, 22, 12, 0, 18, new[]
                {
                    new PrototypeRequirement("jp_rice", 3)
                }),
                new PrototypeOrderDef("fr_grape_order", "法国 France", "fr", 0, "葡萄采买订单 Grape Order", "测试法国葡萄经济线。", 260, 25, 14, 0, 20, new[]
                {
                    new PrototypeRequirement("fr_grape", 3)
                })
            };

            public static PrototypeCountryDef GetCountry(string id)
            {
                for (var i = 0; i < Countries.Length; i++)
                {
                    if (Countries[i].Id == id)
                    {
                        return Countries[i];
                    }
                }

                return null;
            }

            public static PrototypeBiomeDef GetBiome(string id)
            {
                for (var i = 0; i < Biomes.Length; i++)
                {
                    if (Biomes[i].Id == id)
                    {
                        return Biomes[i];
                    }
                }

                return null;
            }

            public static PrototypeCropDef GetCrop(string id)
            {
                for (var i = 0; i < Crops.Length; i++)
                {
                    if (Crops[i].Id == id)
                    {
                        return Crops[i];
                    }
                }

                return null;
            }
        }

        private static class PrototypeBalance
        {
            public static readonly PrototypeBalanceResult Empty = new PrototypeBalanceResult
            {
                adaptation = 0.05f,
                growthSeconds = 60,
                minYield = 1,
                maxYield = 1,
                mutationChance = 0f
            };

            public static PrototypeBalanceResult Calculate(PrototypeBiomeDef biome, PrototypeCropDef crop)
            {
                if (biome == null || crop == null)
                {
                    return Empty;
                }

                var adaptation = Mathf.Clamp(crop.GetAffinity(biome.Id), 0.05f, 1.25f);
                var stress = Mathf.Max(0f, 1f - adaptation);
                var yieldMultiplier = Mathf.Clamp(adaptation, 0.12f, 1.35f);
                var growthMultiplier = 1f + stress * 0.6f;
                var minYield = Mathf.Max(1, Mathf.RoundToInt(crop.BaseYield * yieldMultiplier * 0.85f));
                var maxYield = Mathf.Max(1, Mathf.RoundToInt(crop.BaseYield * yieldMultiplier * 1.15f));
                var mutationChance = Mathf.Clamp(0.002f + stress * stress * 0.08f, 0f, 0.12f);

                return new PrototypeBalanceResult
                {
                    adaptation = adaptation,
                    growthSeconds = Mathf.Max(5, Mathf.RoundToInt(crop.GrowthSeconds * growthMultiplier)),
                    minYield = minYield,
                    maxYield = maxYield,
                    mutationChance = mutationChance
                };
            }
        }

        private struct PrototypeBalanceResult
        {
            public float adaptation;
            public int growthSeconds;
            public int minYield;
            public int maxYield;
            public float mutationChance;
        }

        private sealed class PrototypeCountryDef
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly int RequiredExplorationPoints;
            public readonly bool StartsUnlocked;

            public PrototypeCountryDef(string id, string displayName, int requiredExplorationPoints, bool startsUnlocked)
            {
                Id = id;
                DisplayName = displayName;
                RequiredExplorationPoints = requiredExplorationPoints;
                StartsUnlocked = startsUnlocked;
            }
        }

        private sealed class PrototypeBiomeDef
        {
            public readonly string Id;
            public readonly string CountryId;
            public readonly string DisplayName;
            public readonly string ShortTags;
            public readonly string Description;
            public readonly int SlotCount;
            public readonly int RequiredReputationLevel;

            public PrototypeBiomeDef(string id, string countryId, string displayName, string shortTags, string description, int slotCount, int requiredReputationLevel)
            {
                Id = id;
                CountryId = countryId;
                DisplayName = displayName;
                ShortTags = shortTags;
                Description = description;
                SlotCount = slotCount;
                RequiredReputationLevel = requiredReputationLevel;
            }
        }

        private sealed class PrototypeCropDef
        {
            public readonly string Id;
            public readonly string CountryId;
            public readonly string DisplayName;
            public readonly string SpeciesId;
            public readonly string Category;
            public readonly int BaseYield;
            public readonly int BasePrice;
            public readonly int GrowthSeconds;
            public readonly int RequiredReputationLevel;
            public readonly string MutationHint;
            private readonly PrototypeAffinity[] affinities;

            public PrototypeCropDef(
                string id,
                string countryId,
                string displayName,
                string speciesId,
                string category,
                int baseYield,
                int basePrice,
                int growthSeconds,
                int requiredReputationLevel,
                string mutationHint,
                PrototypeAffinity[] affinities)
            {
                Id = id;
                CountryId = countryId;
                DisplayName = displayName;
                SpeciesId = speciesId;
                Category = category;
                BaseYield = baseYield;
                BasePrice = basePrice;
                GrowthSeconds = growthSeconds;
                RequiredReputationLevel = requiredReputationLevel;
                MutationHint = mutationHint;
                this.affinities = affinities;
            }

            public float GetAffinity(string biomeId)
            {
                for (var i = 0; i < affinities.Length; i++)
                {
                    if (affinities[i].BiomeId == biomeId)
                    {
                        return affinities[i].Value;
                    }
                }

                return 0.25f;
            }
        }

        private readonly struct PrototypeAffinity
        {
            public readonly string BiomeId;
            public readonly float Value;

            public PrototypeAffinity(string biomeId, float value)
            {
                BiomeId = biomeId;
                Value = value;
            }
        }

        private sealed class PrototypeOrderDef
        {
            public readonly string Id;
            public readonly string Channel;
            public readonly string CountryId;
            public readonly int RequiredReputationLevel;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly int RewardCoins;
            public readonly int RewardExperience;
            public readonly int RewardExplorationPoints;
            public readonly int RewardResearchPoints;
            public readonly int RewardCountryReputation;
            public readonly PrototypeRequirement[] Requirements;

            public PrototypeOrderDef(
                string id,
                string channel,
                string countryId,
                int requiredReputationLevel,
                string displayName,
                string description,
                int rewardCoins,
                int rewardExperience,
                int rewardExplorationPoints,
                int rewardResearchPoints,
                int rewardCountryReputation,
                PrototypeRequirement[] requirements)
            {
                Id = id;
                Channel = channel;
                CountryId = countryId;
                RequiredReputationLevel = requiredReputationLevel;
                DisplayName = displayName;
                Description = description;
                RewardCoins = rewardCoins;
                RewardExperience = rewardExperience;
                RewardExplorationPoints = rewardExplorationPoints;
                RewardResearchPoints = rewardResearchPoints;
                RewardCountryReputation = rewardCountryReputation;
                Requirements = requirements;
            }
        }

        private readonly struct PrototypeRequirement
        {
            public readonly string ItemId;
            public readonly int Count;

            public PrototypeRequirement(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }
        }
    }
}
