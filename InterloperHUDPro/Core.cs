using Il2CppTLD.SaveState;
using InterloperHudPro;

[assembly: MelonInfo(typeof(InterloperHudProMain), "InterloperHudPro", "1.1.0", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace InterloperHudPro
{
    public class InterloperHudProMain : MelonMod
    {
        internal static InterloperHudProMain? Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Settings.OnLoad();

            Log("Interloper HUD PRO Starting", false);
        }

        internal static void Log(string message, bool onlyWhenDebugEnabled = true)
        {
            if (onlyWhenDebugEnabled && !Settings.options.IsLogging)
                return;

            Instance?.LoggerInstance.Msg(message);
        }
    }

    // -------------------------------------------------------------------------
    //                           HUD DATA MODELS
    // -------------------------------------------------------------------------
    internal readonly struct TemperatureHudData
    {
        internal readonly float AirTemperature;
        internal readonly float WindChill;
        internal readonly float FeelsLikeTemperature;
        internal readonly bool UseDangerColor;

        internal TemperatureHudData(
            float airTemperature,
            float windChill,
            float feelsLikeTemperature,
            bool useDangerColor)
        {
            AirTemperature = airTemperature;
            WindChill = windChill;
            FeelsLikeTemperature = feelsLikeTemperature;
            UseDangerColor = useDangerColor;
        }
    }

    internal readonly struct WeightHudData
    {
        internal readonly float WeightKg;
        internal readonly bool IsEncumbered;

        internal WeightHudData(float weightKg, bool isEncumbered)
        {
            WeightKg = weightKg;
            IsEncumbered = isEncumbered;
        }
    }

    internal readonly struct ActiveItemHudData
    {
        internal readonly float ConditionPercent;

        internal ActiveItemHudData(float conditionPercent)
        {
            ConditionPercent = conditionPercent;
        }
    }

    internal readonly struct DayNightHudData
    {
        internal readonly int Day;
        internal readonly int Hour;
        internal readonly int Minute;

        internal DayNightHudData(int day, int hour, int minute)
        {
            Day = day;
            Hour = hour;
            Minute = minute;
        }
    }

    internal readonly struct WeakIceHudData
    {
        internal readonly float RemainingSeconds;

        internal WeakIceHudData(float remainingSeconds)
        {
            RemainingSeconds = remainingSeconds;
        }
    }

    // -------------------------------------------------------------------------
    //                              HUD LOGIC
    // -------------------------------------------------------------------------
    internal static class HudLogic
    {
        private const float PoorCirculationPenalty = -5f;
        private const float WeightUnitsToKilograms = 1e9f;
        private const float BaseWeakIceTimeSeconds = 5f;

        private static IceCrackingManager? _cachedIceCrackingManager;
        private static bool _wasInsideWeakIceTrigger;
        private static float _weakIceEnteredAtRealtime = -1f;

        internal static void ResetWeakIceTracking()
        {
            _cachedIceCrackingManager = null;
            _wasInsideWeakIceTrigger = false;
            _weakIceEnteredAtRealtime = -1f;
        }

        internal static bool HasTemperatureHudContent()
        {
            return Settings.options.ShowAirTemperature
                || Settings.options.ShowWindChill
                || Settings.options.ShowFeelsLikeTemperature;
        }

        internal static bool HasMainHudContent()
        {
            return HasTemperatureHudContent()
                || Settings.options.ShowWeight
                || Settings.options.ShowDayNight;
        }

        internal static bool TryGetTemperatureHudData(out TemperatureHudData data)
        {
            data = default;

            var weather = GameManager.GetWeatherComponent();
            var player = GameManager.GetPlayerManagerComponent();
            var condition = GameManager.GetConditionComponent();
            var freezing = GameManager.GetFreezingComponent();

            if (weather == null || player == null || condition == null || freezing == null)
                return false;

            float airTemperature = weather.GetCurrentTemperature();
            float windChill = weather.GetCurrentWindchill();
            float clothingWarmthBonus = player.m_WarmthBonusFromClothing;
            float clothingWindproofBonus = player.m_WindproofBonusFromClothing;

            float netWindChill = Mathf.Min(windChill + clothingWindproofBonus, 0f);

            float poorCirculationModifier = condition.HasSpecificAffliction(AfflictionType.PoorCirculation)
                ? PoorCirculationPenalty
                : 0f;

            float feelsLikeTemperature = airTemperature + clothingWarmthBonus + netWindChill + poorCirculationModifier;
            bool useDangerColor = Math.Round(freezing.CalculateBodyTemperature()) < 0;

            data = new TemperatureHudData(
                airTemperature,
                windChill,
                feelsLikeTemperature,
                useDangerColor);

            return true;
        }

        internal static bool TryGetWeightHudData(out WeightHudData data)
        {
            data = default;

            var inventory = GameManager.GetInventoryComponent();
            var encumber = GameManager.GetEncumberComponent();
            if (inventory == null || encumber == null)
                return false;

            float weightKg = inventory.GetTotalWeightKG().m_Units / WeightUnitsToKilograms;
            bool isEncumbered = encumber.IsEncumbered();

            data = new WeightHudData(weightKg, isEncumbered);
            return true;
        }

        internal static bool TryGetActiveItemHudData(out ActiveItemHudData data)
        {
            data = default;

            var player = GameManager.GetPlayerManagerComponent();
            if (player == null || player.m_ItemInHands == null)
                return false;

            data = new ActiveItemHudData(player.m_ItemInHands.m_CurrentHP);
            return true;
        }

        internal static bool TryGetDayNightHudData(out DayNightHudData data)
        {
            data = default;

            var timeOfDay = GameManager.GetTimeOfDayComponent();
            if (timeOfDay == null)
                return false;

            int day = timeOfDay.GetDayNumber();
            int hour = Mathf.FloorToInt(timeOfDay.GetHour());
            int minute = Mathf.FloorToInt(timeOfDay.GetMinutes());

            data = new DayNightHudData(day, hour, minute);
            return true;
        }

        internal static bool TryGetWeakIceHudData(Panel_HUD hud, out WeakIceHudData data)
        {
            data = default;

            if (hud == null || hud.m_ThinIceWidget == null)
            {
                ResetWeakIceTracking();
                return false;
            }

            IceCrackingManager? iceManager = GetIceCrackingManager();
            if (iceManager == null)
            {
                ResetWeakIceTracking();
                return false;
            }

            bool insideWeakIce = iceManager.IsInsideTrigger();

            if (insideWeakIce && !_wasInsideWeakIceTrigger)
                _weakIceEnteredAtRealtime = Time.realtimeSinceStartup;
            else if (!insideWeakIce)
                _weakIceEnteredAtRealtime = -1f;

            _wasInsideWeakIceTrigger = insideWeakIce;

            bool weakIceWarningVisible =
                iceManager.m_ShowingWeakIceLabel &&
                hud.m_ThinIceWidget.gameObject.activeInHierarchy;

            if (!insideWeakIce || !weakIceWarningVisible)
                return false;

            if (_weakIceEnteredAtRealtime < 0f)
                _weakIceEnteredAtRealtime = Time.realtimeSinceStartup;

            PlayerManager? player = GameManager.GetPlayerManagerComponent();
            float modifier = player != null ? player.m_ClimbingBuffWeakIceTimeSecondsModifier : 0f;
            float allowedTime = BaseWeakIceTimeSeconds + modifier;
            float elapsed = Time.realtimeSinceStartup - _weakIceEnteredAtRealtime;
            float remaining = Mathf.Max(0f, allowedTime - elapsed);

            data = new WeakIceHudData(remaining);
            return true;
        }

        internal static string FormatAirTemperatureText(TemperatureHudData data)
        {
            return $"{data.AirTemperature:F0}°";
        }

        internal static string FormatWindChillText(TemperatureHudData data)
        {
            return $"{data.WindChill:F0}°";
        }

        internal static string FormatFeelsLikeText(TemperatureHudData data)
        {
            return $"{data.FeelsLikeTemperature:F0}°";
        }

        internal static string FormatWeightHudText(WeightHudData data)
        {
            SettingsState settings = SettingsState.Instance;
            if (settings != null && settings.m_Units == MeasurementUnits.Imperial)
            {
                float weightLbs = data.WeightKg * 2.20462f;
                return $"{weightLbs:F2} LBS";
            }

            return $"{data.WeightKg:F2} KG";
        }

        internal static string FormatActiveItemHudText(ActiveItemHudData data)
        {
            return $"{data.ConditionPercent:F0}%";
        }

        internal static string FormatDayNightHudText(DayNightHudData data)
        {
            return Localization.Language switch
            {
                "English" => $"Day {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "German" => $"Tag {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Russian" => $"День {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "French (France)" => $"Jour {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Japanese" => $"{data.Day}日  {data.Hour:D2}:{data.Minute:D2}",
                "Korean" => $"{data.Day}일  {data.Hour:D2}:{data.Minute:D2}",
                "Simplified Chinese" => $"第{data.Day}天  {data.Hour:D2}:{data.Minute:D2}",
                "Swedish" => $"Dag {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Traditional Chinese" => $"第{data.Day}天  {data.Hour:D2}:{data.Minute:D2}",
                "Turkish" => $"Gün {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Norwegian" => $"Dag {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Spanish (Spain)" => $"Día {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Portuguese (Portugal)" => $"Dia {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Portuguese (Brazil)" => $"Dia {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Dutch" => $"Dag {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Finnish" => $"Päivä {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Italian" => $"Giorno {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Polish" => $"Dzień {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "Ukrainian" => $"День {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                "NOTES" => $"Day {data.Day}  {data.Hour:D2}:{data.Minute:D2}",
                _ => $"Day {data.Day}  {data.Hour:D2}:{data.Minute:D2}"
            };
        }

        internal static string FormatBreakIceTimeText(float gameMinutes)
        {
            if (gameMinutes < 1f)
                return Localization.Get("GAMEPLAY_lessthanoneminute");

            return $"{gameMinutes:F0} {Localization.Get("GAMEPLAY_minutes")}";
        }

        internal static string FormatWeakIceTimeText(WeakIceHudData data)
        {
            return $"{data.RemainingSeconds:F1}s";
        }

        private static IceCrackingManager? GetIceCrackingManager()
        {
            if (_cachedIceCrackingManager != null)
                return _cachedIceCrackingManager;

            _cachedIceCrackingManager = UnityEngine.Object.FindObjectOfType<IceCrackingManager>();
            return _cachedIceCrackingManager;
        }
    }

    // -------------------------------------------------------------------------
    //                          HUD RENDERER
    // -------------------------------------------------------------------------
    internal static class HudRenderer
    {
        private const string MainRootName = "InterloperHudPro_MainRoot";
        private const string AirTemperatureLabelName = "InterloperHudPro_AirTemperatureLabel";
        private const string WindChillLabelName = "InterloperHudPro_WindChillLabel";
        private const string FeelsLikeLabelName = "InterloperHudPro_FeelsLikeLabel";
        private const string WeightLabelName = "InterloperHudPro_WeightLabel";
        private const string DayNightLabelName = "InterloperHudPro_DayNightLabel";
        private const string ActiveItemLabelName = "InterloperHudPro_ActiveItemConditionLabel";
        private const string WeakIceTimerLabelName = "InterloperHudPro_WeakIceTimerLabel";

        private const int MainFontSize = 32;
        private const int SmallFontSize = 20;
        private static int WeakIceFontSize => Settings.options.ThinIceTimerFontSize;

        private static Vector3 AirTemperaturePosition => new(Settings.options.AirTemperatureX, 0f, 0f);
        private static Vector3 WindChillPosition => new(Settings.options.WindChillX, 0f, 0f);
        private static Vector3 FeelsLikePosition => new(Settings.options.FeelsLikeX, 0f, 0f);
        private static Vector3 WeightPosition => new(Settings.options.WeightX, 0f, 0f);

        private static readonly Color DefaultTextColor = new(0.9f, 0.95f, 1f, 1f);
        private static readonly Color DangerTextColor = new(0.8f, 0.2f, 0.23f, 1f);
        private static readonly Color OutlineColor = new(0.125f, 0.094f, 0.094f, 0.6f);

        private static GameObject? _mainRoot;

        private static UILabel? _airTemperatureLabel;
        private static UILabel? _windChillLabel;
        private static UILabel? _feelsLikeLabel;
        private static UILabel? _weightLabel;

        private static UILabel? _dayNightLabel;
        private static UILabel? _activeItemLabel;
        private static UILabel? _weakIceTimerLabel;

        private static int _mainLineHeight = 42;

        internal static void Reset()
        {
            if (_mainRoot != null)
            {
                UnityEngine.Object.Destroy(_mainRoot);
                _mainRoot = null;
            }

            if (_dayNightLabel != null)
            {
                UnityEngine.Object.Destroy(_dayNightLabel.gameObject);
                _dayNightLabel = null;
            }

            if (_activeItemLabel != null)
            {
                UnityEngine.Object.Destroy(_activeItemLabel.gameObject);
                _activeItemLabel = null;
            }

            if (_weakIceTimerLabel != null)
            {
                UnityEngine.Object.Destroy(_weakIceTimerLabel.gameObject);
                _weakIceTimerLabel = null;
            }

            _airTemperatureLabel = null;
            _windChillLabel = null;
            _feelsLikeLabel = null;
            _weightLabel = null;

            InterloperHudProMain.Log("HUD renderer reset.");
        }

        internal static void HideMainBlock()
        {
            _mainRoot?.SetActive(false);
        }

        internal static void HideDayNightBlock()
        {
            _dayNightLabel?.gameObject.SetActive(false);
        }

        internal static void HideActiveItemBlock()
        {
            _activeItemLabel?.gameObject.SetActive(false);
        }

        internal static void HideWeakIceTimerBlock()
        {
            _weakIceTimerLabel?.gameObject.SetActive(false);
        }

        internal static void EnsureMainAnchor(StatusBar statusBar)
        {
            if (_mainRoot != null)
                return;

            if (statusBar.m_OuterBoxSprite == null)
                return;

            var outerBoxSprite = statusBar.m_OuterBoxSprite.GetComponent<UISprite>();
            if (outerBoxSprite == null)
                return;

            GameObject parentObject = outerBoxSprite.transform.parent.gameObject;

            _mainRoot = new GameObject(MainRootName);
            _mainRoot.transform.SetParent(parentObject.transform, false);
            _mainRoot.transform.localScale = outerBoxSprite.transform.localScale;

            UILabel anchorLabel = _mainRoot.AddComponent<UILabel>();
            ConfigureStandardLabel(anchorLabel, MainFontSize);
            anchorLabel.text = "0°C";

            _mainLineHeight = anchorLabel.height;

            int xOffset = -anchorLabel.width / 2;
            int yOffset = 20 + anchorLabel.height;
            _mainRoot.transform.localPosition = new Vector3(xOffset, yOffset, 0f);

            UnityEngine.Object.Destroy(anchorLabel);

            _airTemperatureLabel = CreateMainChildLabel(AirTemperatureLabelName, AirTemperaturePosition);
            _windChillLabel = CreateMainChildLabel(WindChillLabelName, WindChillPosition);
            _feelsLikeLabel = CreateMainChildLabel(FeelsLikeLabelName, FeelsLikePosition);
            _weightLabel = CreateMainChildLabel(WeightLabelName, WeightPosition);

            RefreshMainLabelPositions();

            InterloperHudProMain.Log("Main HUD anchor created.");
        }

        internal static void RenderMainBlock(bool hasTemperatureData, TemperatureHudData temperatureData, bool hasWeightData, WeightHudData weightData)
        {
            RefreshMainLabelPositions();

            if (_mainRoot == null || _airTemperatureLabel == null || _windChillLabel == null || _feelsLikeLabel == null || _weightLabel == null)
                return;

            Color temperatureColor = hasTemperatureData && temperatureData.UseDangerColor
                ? DangerTextColor
                : DefaultTextColor;

            Color weightColor = hasWeightData && weightData.IsEncumbered
                ? DangerTextColor
                : DefaultTextColor;

            SetLabelState(
                _airTemperatureLabel,
                Settings.options.ShowAirTemperature && hasTemperatureData,
                hasTemperatureData ? HudLogic.FormatAirTemperatureText(temperatureData) : string.Empty,
                temperatureColor);

            SetLabelState(
                _windChillLabel,
                Settings.options.ShowWindChill && hasTemperatureData,
                hasTemperatureData ? HudLogic.FormatWindChillText(temperatureData) : string.Empty,
                temperatureColor);

            SetLabelState(
                _feelsLikeLabel,
                Settings.options.ShowFeelsLikeTemperature && hasTemperatureData,
                hasTemperatureData ? HudLogic.FormatFeelsLikeText(temperatureData) : string.Empty,
                temperatureColor);

            SetLabelState(
                _weightLabel,
                Settings.options.ShowWeight && hasWeightData,
                hasWeightData ? HudLogic.FormatWeightHudText(weightData) : string.Empty,
                weightColor);

            bool anyVisible =
                (Settings.options.ShowAirTemperature && hasTemperatureData) ||
                (Settings.options.ShowWindChill && hasTemperatureData) ||
                (Settings.options.ShowFeelsLikeTemperature && hasTemperatureData) ||
                (Settings.options.ShowWeight && hasWeightData);

            _mainRoot.SetActive(anyVisible);
        }

        internal static void RenderDayNightBlock(string text)
        {
            if (_mainRoot == null)
                return;

            if (_dayNightLabel == null)
            {
                GameObject parentObject = _mainRoot.transform.parent.gameObject;

                _dayNightLabel = NGUITools.AddWidget<UILabel>(parentObject);
                _dayNightLabel.name = DayNightLabelName;
                ConfigureStandardLabel(_dayNightLabel, MainFontSize);

                InterloperHudProMain.Log("Day/night label created.");
            }

            int yOffset = _mainRoot.activeSelf ? _mainLineHeight + 10 : 0;

            _dayNightLabel.transform.localPosition =
                _mainRoot.transform.localPosition + new Vector3(0f, yOffset, 0f);

            _dayNightLabel.gameObject.SetActive(true);
            _dayNightLabel.text = text;
        }

        internal static void RenderActiveItemBlock(Panel_HUD hud, string text)
        {
            var filledBar = FindActiveItemConditionBar(hud);
            if (filledBar == null)
            {
                HideActiveItemBlock();
                return;
            }

            UILabel label = GetOrCreateActiveItemLabel(filledBar);
            label.transform.localPosition = new Vector3(Settings.options.ActiveItemConditionX, -26f, 0f);
            label.gameObject.SetActive(true);
            label.text = text;
        }

        internal static void RenderWeakIceTimerBlock(Panel_HUD hud, string text)
        {
            if (hud == null || hud.m_ThinIceWidget == null)
            {
                HideWeakIceTimerBlock();
                return;
            }

            UIWidget widget = hud.m_ThinIceWidget;
            UILabel label = GetOrCreateWeakIceTimerLabel(widget);

            label.transform.localScale = Vector3.one;
            label.fontSize = Settings.options.ThinIceTimerFontSize;
            label.effectDistance = new Vector2(1.0f, 1.0f);
            label.alignment = NGUIText.Alignment.Center;
            label.pivot = UIWidget.Pivot.Center;

            float yOffset = -(widget.height * 0.5f) - Settings.options.ThinIceTimerYOffset;
            label.transform.localPosition = widget.transform.localPosition + new Vector3(0f, yOffset, 0f);
            label.text = text;
            label.gameObject.SetActive(true);
        }

        private static UILabel CreateMainChildLabel(string name, Vector3 localPosition)
        {
            GameObject go = new(name);
            go.transform.SetParent(_mainRoot!.transform, false);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = localPosition;

            UILabel label = go.AddComponent<UILabel>();
            ConfigureStandardLabel(label, MainFontSize);
            label.text = string.Empty;
            label.gameObject.SetActive(false);

            return label;
        }

        private static void SetLabelState(UILabel label, bool visible, string text, Color color)
        {
            label.gameObject.SetActive(visible);

            if (!visible)
                return;

            label.text = text;
            label.color = color;
        }

        private static UISprite? FindActiveItemConditionBar(Panel_HUD hud)
        {
            if (hud == null || hud.m_EquipItemPopup == null)
                return null;

            UISprite[] sprites = hud.m_EquipItemPopup.GetComponentsInChildren<UISprite>();
            foreach (UISprite sprite in sprites)
            {
                if (sprite != null && sprite.type == UIBasicSprite.Type.Filled && sprite.depth >= 3)
                    return sprite;
            }

            return null;
        }

        private static UILabel GetOrCreateActiveItemLabel(UISprite barSprite)
        {
            if (_activeItemLabel != null)
            {
                Transform? currentParent = _activeItemLabel.transform.parent;
                Transform? targetParent = barSprite.transform.parent;

                if (currentParent == targetParent)
                    return _activeItemLabel;

                UnityEngine.Object.Destroy(_activeItemLabel.gameObject);
                _activeItemLabel = null;
            }

            Transform parent = barSprite.transform.parent;
            GameObject? existingObject = parent.Find(ActiveItemLabelName)?.gameObject;

            if (existingObject != null)
            {
                UILabel existingLabel = existingObject.GetComponent<UILabel>();
                if (existingLabel != null)
                {
                    _activeItemLabel = existingLabel;
                    return _activeItemLabel;
                }
            }

            GameObject labelObject = new(ActiveItemLabelName);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localScale = barSprite.transform.localScale;
            labelObject.transform.localPosition = new Vector3(Settings.options.ActiveItemConditionX, -26f, 0f);

            _activeItemLabel = labelObject.AddComponent<UILabel>();
            ConfigureStandardLabel(_activeItemLabel, SmallFontSize);

            InterloperHudProMain.Log("Active item condition label created.");
            return _activeItemLabel;
        }

        private static UILabel GetOrCreateWeakIceTimerLabel(UIWidget widget)
        {
            Transform parent = widget.transform.parent;

            if (_weakIceTimerLabel != null)
            {
                if (_weakIceTimerLabel.transform.parent == parent)
                    return _weakIceTimerLabel;

                UnityEngine.Object.Destroy(_weakIceTimerLabel.gameObject);
                _weakIceTimerLabel = null;
            }

            GameObject? existingObject = parent.Find(WeakIceTimerLabelName)?.gameObject;
            if (existingObject != null)
            {
                UILabel existingLabel = existingObject.GetComponent<UILabel>();
                if (existingLabel != null)
                {
                    _weakIceTimerLabel = existingLabel;
                    return _weakIceTimerLabel;
                }
            }

            GameObject labelObject = new(WeakIceTimerLabelName);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localScale = Vector3.one;

            _weakIceTimerLabel = labelObject.AddComponent<UILabel>();
            ConfigureCenteredLabel(_weakIceTimerLabel, WeakIceFontSize);
            _weakIceTimerLabel.effectDistance = new Vector2(1.0f, 1.0f);
            _weakIceTimerLabel.text = string.Empty;
            _weakIceTimerLabel.gameObject.SetActive(false);

            InterloperHudProMain.Log("Weak ice timer label created.");
            return _weakIceTimerLabel;
        }

        private static void ConfigureStandardLabel(UILabel label, int fontSize)
        {
            label.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
            label.fontStyle = FontStyle.Normal;
            label.color = DefaultTextColor;
            label.fontSize = fontSize;
            label.effectStyle = UILabel.Effect.Outline;
            label.effectColor = OutlineColor;
            label.effectDistance = fontSize >= MainFontSize ? new Vector2(1.7f, 1.7f) : new Vector2(1.5f, 1.5f);
            label.overflowMethod = UILabel.Overflow.ResizeFreely;
            label.alignment = NGUIText.Alignment.Left;
            label.pivot = UIWidget.Pivot.Left;
        }

        private static void ConfigureCenteredLabel(UILabel label, int fontSize)
        {
            ConfigureStandardLabel(label, fontSize);
            label.alignment = NGUIText.Alignment.Center;
            label.pivot = UIWidget.Pivot.Center;
        }

        private static void RefreshMainLabelPositions()
        {
            if (_airTemperatureLabel != null)
                _airTemperatureLabel.transform.localPosition = AirTemperaturePosition;

            if (_windChillLabel != null)
                _windChillLabel.transform.localPosition = WindChillPosition;

            if (_feelsLikeLabel != null)
                _feelsLikeLabel.transform.localPosition = FeelsLikePosition;

            if (_weightLabel != null)
                _weightLabel.transform.localPosition = WeightPosition;
        }
    }

    // -------------------------------------------------------------------------
    //                               PATCHES
    // -------------------------------------------------------------------------
    internal static class Patches
    {
        private const double TemperatureUpdateIntervalMinutes = 0.1d;
        private const double GeneralHudUpdateIntervalMinutes = 0.01d;

        private static double GetElapsedMinutes()
        {
            var timer = GameManager.GetHighResolutionTimerManager();
            return timer != null ? timer.GetElapsedMinutes() : 0d;
        }

        [HarmonyPatch(typeof(GameManager), "Start")]
        private static class ResetHudRefsOnGameStart
        {
            private static void Postfix()
            {
                HudRenderer.Reset();
                HudLogic.ResetWeakIceTracking();
                MainHudPatch.LastUpdateMinutes = 0d;
                DayNightHudPatch.LastUpdateMinutes = 0d;
                ActiveItemHudPatch.LastUpdateMinutes = 0d;
                WeakIceHudPatch.LastUpdateMinutes = 0d;
            }
        }

        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class MainHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(StatusBar __instance)
            {
                if (!HudLogic.HasMainHudContent())
                {
                    HudRenderer.HideMainBlock();
                    return;
                }

                if (!__instance.m_IsOnHUD)
                    return;

                if (__instance.m_StatusBarType != StatusBar.StatusBarType.Cold)
                    return;

                HudRenderer.EnsureMainAnchor(__instance);

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < TemperatureUpdateIntervalMinutes)
                    return;

                bool needTemperatureData = HudLogic.HasTemperatureHudContent();
                bool needWeightData = Settings.options.ShowWeight;

                TemperatureHudData temperatureData = default;
                WeightHudData weightData = default;

                if (needTemperatureData && !HudLogic.TryGetTemperatureHudData(out temperatureData))
                {
                    HudRenderer.HideMainBlock();
                    return;
                }

                if (needWeightData && !HudLogic.TryGetWeightHudData(out weightData))
                {
                    HudRenderer.HideMainBlock();
                    return;
                }

                HudRenderer.RenderMainBlock(
                    needTemperatureData,
                    temperatureData,
                    needWeightData,
                    weightData);

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), "Update")]
        private static class DayNightHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix()
            {
                if (!Settings.options.ShowDayNight || !HudLogic.HasMainHudContent())
                {
                    HudRenderer.HideDayNightBlock();
                    return;
                }

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < GeneralHudUpdateIntervalMinutes)
                    return;

                if (!HudLogic.TryGetDayNightHudData(out DayNightHudData data))
                {
                    HudRenderer.HideDayNightBlock();
                    LastUpdateMinutes = now;
                    return;
                }

                string text = HudLogic.FormatDayNightHudText(data);
                HudRenderer.RenderDayNightBlock(text);

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), "Update")]
        private static class ActiveItemHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                if (!Settings.options.ShowActiveItemCondition)
                {
                    HudRenderer.HideActiveItemBlock();
                    return;
                }

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < GeneralHudUpdateIntervalMinutes)
                    return;

                if (!HudLogic.TryGetActiveItemHudData(out ActiveItemHudData data))
                {
                    HudRenderer.HideActiveItemBlock();
                    LastUpdateMinutes = now;
                    return;
                }

                string text = HudLogic.FormatActiveItemHudText(data);
                HudRenderer.RenderActiveItemBlock(__instance, text);

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), "Update")]
        private static class WeakIceHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                if (!Settings.options.ShowThinIceTime)
                {
                    HudRenderer.HideWeakIceTimerBlock();
                    return;
                }

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < GeneralHudUpdateIntervalMinutes)
                    return;

                if (!HudLogic.TryGetWeakIceHudData(__instance, out WeakIceHudData data))
                {
                    HudRenderer.HideWeakIceTimerBlock();
                    LastUpdateMinutes = now;
                    return;
                }

                string text = HudLogic.FormatWeakIceTimeText(data);
                HudRenderer.RenderWeakIceTimerBlock(__instance, text);

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), "Enable")]
        private static class IceFishingHoleClearEnablePatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance)
            {
                UpdateBreakIceToolLabel(__instance);
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), "Update")]
        private static class IceFishingHoleClearUpdatePatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance)
            {
                UpdateBreakIceToolLabel(__instance);
            }
        }

        private static void UpdateBreakIceToolLabel(Panel_IceFishingHoleClear panel)
        {
            if (!Settings.options.ShowBreakIceTime)
                return;

            if (panel == null || panel.m_ToolNameLabel == null || panel.m_ScrollList == null)
                return;

            if (panel.m_AvailableTools == null || panel.m_IceFishingHole == null)
                return;

            int selectedIndex = panel.m_ScrollList.m_SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= panel.m_AvailableTools.Count)
                return;

            GearItem selectedTool = panel.m_AvailableTools[selectedIndex];
            if (selectedTool == null || selectedTool.m_IceFishingHoleClearItem == null)
                return;

            float estimatedMinutes =
                panel.m_IceFishingHole.NormalizedFrozen *
                selectedTool.m_IceFishingHoleClearItem.m_NumGameMinutesToClear;

            string toolName = selectedTool.DisplayName;
            string timeText = HudLogic.FormatBreakIceTimeText(estimatedMinutes);

            panel.m_ToolNameLabel.text = $"{toolName} ({timeText})";
        }
    }
}