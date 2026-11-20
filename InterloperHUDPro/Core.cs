using Il2CppTLD.SaveState;
using InterloperHudPro;
using UnityEngine.SceneManagement;
using static InterloperHudPro.ModSettings;

[assembly: MelonInfo(typeof(InterloperHudProMain), "InterloperHudPro", "1.5.0", "EtherSystem", null)]
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

        public override void OnUpdate()
        {
            HudEditor.OnUpdate();
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

    internal readonly struct BodyHeatHudData
    {
        internal readonly float Celsius;

        internal BodyHeatHudData(float celsius)
        {
            Celsius = celsius;
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

    internal readonly struct MovementSpeedHudData
    {
        internal readonly float SpeedMetersPerSecond;

        internal MovementSpeedHudData(float speedMetersPerSecond)
        {
            SpeedMetersPerSecond = speedMetersPerSecond;
        }
    }

    internal readonly struct PlayerCoordinatesHudData
    {
        internal readonly Vector3 Position;

        internal PlayerCoordinatesHudData(Vector3 position)
        {
            Position = position;
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

    internal enum WindHudSeverity
    {
        Safe,
        Warning,
        Danger
    }

    internal readonly struct WindDirectionHudData
    {
        internal readonly float RelativeAngle;
        internal readonly bool IsOutdoors;
        internal readonly float SpeedMPH;
        internal readonly WindHudSeverity Severity;

        internal WindDirectionHudData(float relativeAngle, bool isOutdoors, float speedMph, WindHudSeverity severity)
        {
            RelativeAngle = relativeAngle;
            IsOutdoors = isOutdoors;
            SpeedMPH = speedMph;
            Severity = severity;
        }
    }

    internal static class MajorMiseriesIntegration
    {
        private const string AssemblyName = "MajorMiseries";
        private const string CoreTypeName = "MajorMiseries.Core";
        private const string SettingsTypeName = "MajorMiseries.Settings";

        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool _resolutionAttempted;
        private static bool _failureLogged;
        private static PropertyInfo? _isGameplayEnabledProperty;
        private static FieldInfo? _stateField;
        private static FieldInfo? _internalBodyTempField;
        private static FieldInfo? _settingsOptionsField;
        private static FieldInfo? _enableBodyHeatField;

        internal static bool IsAvailable()
        {
            return ResolveMembers();
        }

        internal static void Reset()
        {
            _resolutionAttempted = false;
            _failureLogged = false;
            _isGameplayEnabledProperty = null;
            _stateField = null;
            _internalBodyTempField = null;
            _settingsOptionsField = null;
            _enableBodyHeatField = null;
        }

        internal static bool TryGetBodyHeat(out float bodyHeatCelsius)
        {
            bodyHeatCelsius = 0f;
            if (!ResolveMembers()) return false;

            try
            {
                if (_isGameplayEnabledProperty?.GetValue(null) is not bool gameplayEnabled || !gameplayEnabled)
                    return false;

                object? settings = _settingsOptionsField?.GetValue(null);
                if (settings == null || _enableBodyHeatField?.GetValue(settings) is not bool bodyHeatEnabled || !bodyHeatEnabled)
                    return false;

                object? state = _stateField?.GetValue(null);
                if (state == null || _internalBodyTempField?.GetValue(state) is not float bodyHeat)
                    return false;

                if (float.IsNaN(bodyHeat) || float.IsInfinity(bodyHeat))
                    return false;

                bodyHeatCelsius = Mathf.Clamp(bodyHeat, 34f, 43f);
                return true;
            }
            catch (Exception ex)
            {
                LogFailureOnce($"MajorMiseries Body Heat integration failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static bool ResolveMembers()
        {
            if (_resolutionAttempted)
            {
                return _isGameplayEnabledProperty != null
                    && _stateField != null
                    && _internalBodyTempField != null
                    && _settingsOptionsField != null
                    && _enableBodyHeatField != null;
            }

            _resolutionAttempted = true;

            try
            {
                Assembly? assembly = null;
                foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(loadedAssembly.GetName().Name, AssemblyName, StringComparison.Ordinal)) continue;

                    assembly = loadedAssembly;
                    break;
                }

                if (assembly == null) return false;

                Type? coreType = assembly.GetType(CoreTypeName, false);
                Type? settingsType = assembly.GetType(SettingsTypeName, false);
                if (coreType == null || settingsType == null) return false;

                _isGameplayEnabledProperty = coreType.GetProperty("IsGameplayEnabled", StaticFlags);
                _stateField = coreType.GetField("State", StaticFlags);
                _settingsOptionsField = settingsType.GetField("options", StaticFlags);

                if (_stateField == null || _settingsOptionsField == null) return false;

                _internalBodyTempField = _stateField.FieldType.GetField("InternalBodyTemp", InstanceFlags);
                _enableBodyHeatField = _settingsOptionsField.FieldType.GetField("EnableBodyHeat", InstanceFlags);

                return _isGameplayEnabledProperty != null
                    && _internalBodyTempField != null
                    && _enableBodyHeatField != null;
            }
            catch (Exception ex)
            {
                LogFailureOnce($"MajorMiseries Body Heat integration could not initialize: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static void LogFailureOnce(string message)
        {
            if (_failureLogged) return;

            _failureLogged = true;
            InterloperHudProMain.Log(message, false);
        }
    }

    internal static class WeatherOverhaulIntegration
    {
        private const string AssemblyName = "WeatherOverhaul";
        private const string ApiTypeName = "WeatherOverhaul.WeatherOverhaulApi";
        private const string ApiMethodName = "TryGetCurrentStageDisplayName";

        private delegate bool TryGetCurrentStageDisplayNameDelegate(out string displayName);

        private static bool _resolutionAttempted;
        private static bool _failureLogged;
        private static TryGetCurrentStageDisplayNameDelegate? _tryGetCurrentStageDisplayName;

        internal static void Reset()
        {
            _resolutionAttempted = false;
            _failureLogged = false;
            _tryGetCurrentStageDisplayName = null;
        }

        internal static bool TryGetCurrentStageDisplayName(out string displayName)
        {
            displayName = string.Empty;
            if (!ResolveApi() || _tryGetCurrentStageDisplayName == null) return false;

            try
            {
                return _tryGetCurrentStageDisplayName(out displayName) && !string.IsNullOrWhiteSpace(displayName);
            }
            catch (Exception ex)
            {
                LogFailureOnce($"WeatherOverhaul integration failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static bool ResolveApi()
        {
            if (_resolutionAttempted) return _tryGetCurrentStageDisplayName != null;
            _resolutionAttempted = true;

            try
            {
                Assembly? assembly = null;
                foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!string.Equals(loadedAssembly.GetName().Name, AssemblyName, StringComparison.Ordinal)) continue;
                    assembly = loadedAssembly;
                    break;
                }

                if (assembly == null) return false;

                Type? apiType = assembly.GetType(ApiTypeName, false);
                MethodInfo? apiMethod = apiType?.GetMethod(ApiMethodName, BindingFlags.Static | BindingFlags.Public);
                if (apiMethod == null) return false;

                _tryGetCurrentStageDisplayName = (TryGetCurrentStageDisplayNameDelegate)apiMethod.CreateDelegate(typeof(TryGetCurrentStageDisplayNameDelegate));
                return true;
            }
            catch (Exception ex)
            {
                LogFailureOnce($"WeatherOverhaul integration could not initialize: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static void LogFailureOnce(string message)
        {
            if (_failureLogged) return;
            _failureLogged = true;
            InterloperHudProMain.Log(message, false);
        }
    }

    // -------------------------------------------------------------------------
    //                              HUD LOGIC
    // -------------------------------------------------------------------------
    internal static class HudLogic
    {
        private const float WeightUnitsToKilograms = 1e9f;
        private const float BaseWeakIceTimeSeconds = 5f;

        private const float WindWarningThresholdKmh = 49f;
        private const float WindDangerThresholdKmh = 65f;
        private const float MphToKmhFactor = 1.60934f;

        private static IceCrackingManager? _cachedIceCrackingManager;
        private static bool _wasInsideWeakIceTrigger;
        private static float _weakIceEnteredAtRealtime = -1f;

        internal static bool TryGetSceneHudText(out string text)
        {
            text = string.Empty;

            Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.name))
                return false;

            text = activeScene.name;
            return true;
        }

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

        internal static bool HasBodyHeatHudContent()
        {
            return Settings.options.ShowBodyHeat && MajorMiseriesIntegration.IsAvailable();
        }

        internal static bool HasWindArrowContent()
        {
            return Settings.options.WindDisplayMode == WindHudDisplayMode.ArrowOnly
                || Settings.options.WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;
        }

        internal static bool HasWindSpeedContent()
        {
            return Settings.options.WindDisplayMode == WindHudDisplayMode.SpeedOnly
                || Settings.options.WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;
        }

        internal static bool HasWindHudContent()
        {
            return HasWindArrowContent() || HasWindSpeedContent();
        }

        internal static bool HasMainHudContent()
        {
            return HasTemperatureHudContent()
                || HasBodyHeatHudContent()
                || HasWindHudContent()
                || Settings.options.ShowWeight
                || Settings.options.ShowMovementSpeed
                || Settings.options.ShowPlayerCoordinates
                || Settings.options.ShowDay
                || Settings.options.ShowTime
                || Settings.options.ShowSceneName
                || Settings.options.ShowWeatherOverhaulStage;
        }

        internal static bool TryGetMovementSpeedHudData(out MovementSpeedHudData data)
        {
            data = default;

            var player = GameManager.GetPlayerManagerComponent();
            if (player == null)
                return false;

            PlayerMovement? playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
                return false;

            Vector3 velocity = playerMovement.GetVelocity();
            velocity.y = 0f;

            data = new MovementSpeedHudData(velocity.magnitude);
            return true;
        }

        internal static string FormatMovementSpeedHudText(MovementSpeedHudData data)
        {
            SettingsState settings = SettingsState.Instance;
            if (settings != null && settings.m_Units == MeasurementUnits.Imperial)
            {
                float speedMph = data.SpeedMetersPerSecond * 2.23694f;
                return $"{speedMph:F1} MPH";
            }

            float speedKmh = data.SpeedMetersPerSecond * 3.6f;
            return $"{speedKmh:F1} KM/H";
        }

        internal static bool TryGetPlayerCoordinatesHudData(out PlayerCoordinatesHudData data)
        {
            data = default;

            Transform? playerTransform = GameManager.GetPlayerTransform();
            if (playerTransform == null) return false;

            Vector3 position = playerTransform.position;
            if (float.IsNaN(position.x) || float.IsInfinity(position.x)
                || float.IsNaN(position.y) || float.IsInfinity(position.y)
                || float.IsNaN(position.z) || float.IsInfinity(position.z)) return false;

            data = new PlayerCoordinatesHudData(position);
            return true;
        }

        internal static string FormatPlayerCoordinatesHudText(PlayerCoordinatesHudData data)
        {
            return Settings.options.ShowPlayerCoordinateDecimals
                ? $"{data.Position.x:F2} / {data.Position.y:F2} / {data.Position.z:F2}"
                : $"{data.Position.x:F0} / {data.Position.y:F0} / {data.Position.z:F0}";
        }

        internal static bool TryGetWindDirectionHudData(out WindDirectionHudData data)
        {
            data = default;

            Wind? wind = GameManager.GetWindComponent();
            Weather? weather = GameManager.GetWeatherComponent();

            if (weather == null || wind == null)
                return false;

            bool isOutdoors = !weather.IsIndoorEnvironment() && !weather.IsIndoorScene();
            float relativeAngle = wind.GetWindAngleRelativeToPlayer();
            float speedMph = wind.GetSpeedMPH();
            WindHudSeverity severity = GetWindHudSeverity(isOutdoors, speedMph);

            data = new WindDirectionHudData(relativeAngle, isOutdoors, speedMph, severity);
            return true;
        }

        internal static string FormatWindSpeedText(WindDirectionHudData data)
        {
            SettingsState settings = SettingsState.Instance;
            if (settings != null && settings.m_Units == MeasurementUnits.Imperial)
            {
                return $"{Mathf.CeilToInt(data.SpeedMPH)} MPH";
            }

            float speedKmh = data.SpeedMPH * MphToKmhFactor;
            return $"{Mathf.CeilToInt(speedKmh)} KM/H";
        }

        private static WindHudSeverity GetWindHudSeverity(bool isOutdoors, float speedMph)
        {
            Wind? wind = GameManager.GetWindComponent();
            if (wind == null)
                return WindHudSeverity.Safe;

            if (!isOutdoors)
                return WindHudSeverity.Safe;

            if (wind.PlayerShelteredFromWind())
                return WindHudSeverity.Safe;

            float speedKmh = speedMph * MphToKmhFactor;

            if (speedKmh >= WindDangerThresholdKmh)
                return WindHudSeverity.Danger;

            if (speedKmh >= WindWarningThresholdKmh)
                return WindHudSeverity.Warning;

            return WindHudSeverity.Safe;
        }

        internal static bool TryGetTemperatureHudData(out TemperatureHudData data)
        {
            data = default;

            var weather = GameManager.GetWeatherComponent();
            var freezing = GameManager.GetFreezingComponent();

            if (weather == null || freezing == null)
                return false;

            float airTemperature = weather.GetCurrentTemperature();
            float windChill = weather.GetCurrentWindchill();
            float feelsLikeTemperature = freezing.CalculateBodyTemperature();
            bool useDangerColor = Math.Round(feelsLikeTemperature) < 0;

            data = new TemperatureHudData(
                airTemperature,
                windChill,
                feelsLikeTemperature,
                useDangerColor);

            return true;
        }

        internal static bool TryGetBodyHeatHudData(out BodyHeatHudData data)
        {
            data = default;
            if (!MajorMiseriesIntegration.TryGetBodyHeat(out float bodyHeatCelsius)) return false;

            data = new BodyHeatHudData(bodyHeatCelsius);
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
            float temperature = IsUsingImperialUnits()
                ? CelsiusToFahrenheit(data.AirTemperature)
                : data.AirTemperature;

            return $"{temperature:F0}°";
        }

        internal static string FormatWindChillText(TemperatureHudData data)
        {
            float windChill = IsUsingImperialUnits()
                ? CelsiusDeltaToFahrenheitDelta(data.WindChill)
                : data.WindChill;

            return $"{windChill:F0}°";
        }

        internal static string FormatFeelsLikeText(TemperatureHudData data)
        {
            float temperature = IsUsingImperialUnits()
                ? CelsiusToFahrenheit(data.FeelsLikeTemperature)
                : data.FeelsLikeTemperature;

            return $"{temperature:F0}°";
        }

        internal static string FormatBodyHeatHudText(BodyHeatHudData data)
        {
            if (IsUsingImperialUnits())
                return $"{CelsiusToFahrenheit(data.Celsius):F1}°F";

            return $"{data.Celsius:F1}°C";
        }

        private static bool IsUsingImperialUnits()
        {
            SettingsState settings = SettingsState.Instance;
            return settings != null && settings.m_Units == MeasurementUnits.Imperial;
        }

        private static float CelsiusToFahrenheit(float celsius)
        {
            return celsius * 1.8f + 32f;
        }

        private static float CelsiusDeltaToFahrenheitDelta(float celsiusDelta)
        {
            return celsiusDelta * 1.8f;
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

        internal static string FormatDayHudText(DayNightHudData data)
        {
            return Localization.Language switch
            {
                "English" => $"Day {data.Day}",
                "German" => $"Tag {data.Day}",
                "Russian" => $"День {data.Day}",
                "French (France)" => $"Jour {data.Day}",
                "Japanese" => $"{data.Day}日",
                "Korean" => $"{data.Day}일",
                "Simplified Chinese" => $"第{data.Day}天",
                "Swedish" => $"Dag {data.Day}",
                "Traditional Chinese" => $"第{data.Day}天",
                "Turkish" => $"Gün {data.Day}",
                "Norwegian" => $"Dag {data.Day}",
                "Spanish (Spain)" => $"Día {data.Day}",
                "Portuguese (Portugal)" => $"Dia {data.Day}",
                "Portuguese (Brazil)" => $"Dia {data.Day}",
                "Dutch" => $"Dag {data.Day}",
                "Finnish" => $"Päivä {data.Day}",
                "Italian" => $"Giorno {data.Day}",
                "Polish" => $"Dzień {data.Day}",
                "Ukrainian" => $"День {data.Day}",
                "NOTES" => $"Day {data.Day}",
                _ => $"Day {data.Day}"
            };
        }

        internal static string FormatTimeHudText(DayNightHudData data)
        {
            return $"{data.Hour:D2}:{data.Minute:D2}";
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
        private const string BodyHeatLabelName = "InterloperHudPro_BodyHeatLabel";
        private const string WeightLabelName = "InterloperHudPro_WeightLabel";
        private const string DayLabelName = "InterloperHudPro_DayLabel";
        private const string TimeLabelName = "InterloperHudPro_TimeLabel";
        private const string ActiveItemLabelName = "InterloperHudPro_ActiveItemConditionLabel";
        private const string WeakIceTimerLabelName = "InterloperHudPro_WeakIceTimerLabel";
        private const string WindRootName = "InterloperHudPro_WindRoot";
        private const string WindArrowLabelName = "InterloperHudPro_WindArrowLabel";
        private const string WindSpeedLabelName = "InterloperHudPro_WindSpeedLabel";
        private const string SceneLabelName = "InterloperHudPro_SceneLabel";
        private const string WeatherOverhaulStageLabelName = "InterloperHudPro_WeatherOverhaulStageLabel";
        private const string MovementSpeedLabelName = "InterloperHudPro_MovementSpeedLabel";
        private const string PlayerCoordinatesLabelName = "InterloperHudPro_PlayerCoordinatesLabel";
        private const string WindDirectionGlyph = "↑";

        private static float _indoorWindSpinAngle = 0f;

        private static Vector3 WindRootPosition => new(Settings.options.WindDirectionX, Settings.options.WindDirectionY, 0f);
        private static Vector3 WindArrowLocalPosition => new(0f, 0f, 0f);
        private static Vector3 WindSpeedLocalPosition => new(0f, -Settings.options.WindSpeedYOffset, 0f);
        private static Vector3 MovementSpeedPosition => new(Settings.options.MovementSpeedX, Settings.options.MovementSpeedY, 0f);
        private static Vector3 PlayerCoordinatesPosition => new(Settings.options.PlayerCoordinatesX, Settings.options.PlayerCoordinatesY, 0f);

        private static GameObject? _windRoot;
        private static UILabel? _windArrowLabel;
        private static UILabel? _windSpeedLabel;
        private static UILabel? _movementSpeedLabel;
        private static UILabel? _playerCoordinatesLabel;

        private const int MainFontSize = 32;
        private const int SmallFontSize = 20;
        private static int WeakIceFontSize => Settings.options.ThinIceTimerFontSize;

        private static Vector3 AirTemperaturePosition => new(Settings.options.AirTemperatureX, Settings.options.AirTemperatureY, 0f);
        private static Vector3 WindChillPosition => new(Settings.options.WindChillX, Settings.options.WindChillY, 0f);
        private static Vector3 FeelsLikePosition => new(Settings.options.FeelsLikeX, Settings.options.FeelsLikeY, 0f);
        private static Vector3 BodyHeatPosition => new(Settings.options.BodyHeatX, Settings.options.BodyHeatY, 0f);
        private static Vector3 WeightPosition => new(Settings.options.WeightX, Settings.options.WeightY, 0f);
        private static Vector3 DayHudPosition => SharedToParentSpace(Settings.options.DayHudX, Settings.options.DayHudY);
        private static Vector3 TimeHudPosition => SharedToParentSpace(Settings.options.TimeHudX, Settings.options.TimeHudY);
        private static Vector3 SceneHudPosition => SharedToParentSpace(Settings.options.SceneHudX, Settings.options.SceneHudY);
        private static Vector3 WeatherOverhaulStageHudPosition => SharedToParentSpace(Settings.options.WeatherOverhaulStageX, Settings.options.WeatherOverhaulStageY);

        private static readonly Color DefaultTextColor = new(0.9f, 0.95f, 1f, 1f);
        private static readonly Color WarningTextColor = new(0.95f, 0.55f, 0.15f, 1f);
        private static readonly Color DangerTextColor = new(0.8f, 0.2f, 0.23f, 1f);
        private static readonly Color OutlineColor = new(0.125f, 0.094f, 0.094f, 0.6f);

        private static GameObject? _mainRoot;

        private static UILabel? _airTemperatureLabel;
        private static UILabel? _windChillLabel;
        private static UILabel? _feelsLikeLabel;
        private static UILabel? _bodyHeatLabel;
        private static UILabel? _weightLabel;

        private static UILabel? _sceneLabel;
        private static UILabel? _weatherOverhaulStageLabel;
        private static UILabel? _dayLabel;
        private static UILabel? _timeLabel;
        private static UILabel? _activeItemLabel;
        private static UILabel? _weakIceTimerLabel;

        private static int _mainLineHeight = 42;

        // ---------------------------------------------------------------------
        //                    LAYOUT EDITOR ACCESS / PREVIEW
        // ---------------------------------------------------------------------
        internal static bool HasMainRoot => _mainRoot != null;
        internal static Transform? MainRootParent => _mainRoot != null ? _mainRoot.transform.parent : null;
        internal static Transform? WindRootTransform => _windRoot != null ? _windRoot.transform : null;
        internal static Color NormalTextColor => DefaultTextColor;

        internal static UILabel? AirTemperatureLabel => _airTemperatureLabel;
        internal static UILabel? WindChillLabel => _windChillLabel;
        internal static UILabel? FeelsLikeLabel => _feelsLikeLabel;
        internal static UILabel? BodyHeatLabel => _bodyHeatLabel;
        internal static UILabel? WeightLabel => _weightLabel;
        internal static UILabel? MovementSpeedLabel => _movementSpeedLabel;
        internal static UILabel? PlayerCoordinatesLabel => _playerCoordinatesLabel;
        internal static UILabel? WindArrowLabel => _windArrowLabel;
        internal static UILabel? WindSpeedLabel => _windSpeedLabel;
        internal static UILabel? DayLabel => _dayLabel;
        internal static UILabel? TimeLabel => _timeLabel;
        internal static UILabel? SceneLabel => _sceneLabel;
        internal static UILabel? WeatherOverhaulStageLabel => _weatherOverhaulStageLabel;
        internal static UILabel? ActiveItemLabel => _activeItemLabel;
        internal static UILabel? WeakIceTimerLabel => _weakIceTimerLabel;

        internal static void RenderEditorPreview(Panel_HUD hud)
        {
            if (_mainRoot == null)
                return;

            _mainRoot.SetActive(true);
            RefreshMainLabelPositions();

            TemperatureHudData temperature = HudLogic.TryGetTemperatureHudData(out TemperatureHudData realTemperature)
                ? realTemperature
                : new TemperatureHudData(-10f, -5f, -15f, false);
            BodyHeatHudData bodyHeat = HudLogic.TryGetBodyHeatHudData(out BodyHeatHudData realBodyHeat)
                ? realBodyHeat
                : new BodyHeatHudData(37f);
            WeightHudData weight = HudLogic.TryGetWeightHudData(out WeightHudData realWeight)
                ? realWeight
                : new WeightHudData(12.5f, false);
            MovementSpeedHudData movementSpeed = HudLogic.TryGetMovementSpeedHudData(out MovementSpeedHudData realSpeed)
                ? realSpeed
                : new MovementSpeedHudData(0f);
            PlayerCoordinatesHudData coordinates = HudLogic.TryGetPlayerCoordinatesHudData(out PlayerCoordinatesHudData realCoordinates)
                ? realCoordinates
                : new PlayerCoordinatesHudData(Vector3.zero);

            ApplyFontSize(_bodyHeatLabel, Settings.options.BodyHeatFontSize);
            ApplyFontSize(_movementSpeedLabel, Settings.options.MovementSpeedFontSize);
            ApplyFontSize(_playerCoordinatesLabel, Settings.options.PlayerCoordinatesFontSize);

            ShowPreviewLabel(_airTemperatureLabel, HudLogic.FormatAirTemperatureText(temperature));
            ShowPreviewLabel(_windChillLabel, HudLogic.FormatWindChillText(temperature));
            ShowPreviewLabel(_feelsLikeLabel, HudLogic.FormatFeelsLikeText(temperature));
            ShowPreviewLabel(_bodyHeatLabel, HudLogic.FormatBodyHeatHudText(bodyHeat));
            ShowPreviewLabel(_weightLabel, HudLogic.FormatWeightHudText(weight));
            ShowPreviewLabel(_movementSpeedLabel, HudLogic.FormatMovementSpeedHudText(movementSpeed));
            ShowPreviewLabel(_playerCoordinatesLabel, HudLogic.FormatPlayerCoordinatesHudText(coordinates));

            if (_windRoot != null && _windArrowLabel != null && _windSpeedLabel != null)
            {
                WindDirectionHudData wind = HudLogic.TryGetWindDirectionHudData(out WindDirectionHudData realWind)
                    ? realWind
                    : new WindDirectionHudData(0f, true, 10f, WindHudSeverity.Safe);

                _windRoot.SetActive(true);
                _windRoot.transform.localPosition = WindRootPosition;
                _windArrowLabel.transform.localPosition = WindArrowLocalPosition;
                _windSpeedLabel.transform.localPosition = WindSpeedLocalPosition;
                _windArrowLabel.fontSize = Settings.options.WindArrowFontSize;
                _windSpeedLabel.fontSize = Settings.options.WindSpeedFontSize;

                _windArrowLabel.transform.localRotation = Quaternion.identity;
                _windSpeedLabel.transform.localRotation = Quaternion.identity;

                ShowPreviewLabel(_windArrowLabel, WindDirectionGlyph);
                ShowPreviewLabel(_windSpeedLabel, HudLogic.FormatWindSpeedText(wind));
            }

            DayNightHudData dayNight = HudLogic.TryGetDayNightHudData(out DayNightHudData realDayNight)
                ? realDayNight
                : new DayNightHudData(1, 12, 0);

            RenderDayBlock(HudLogic.FormatDayHudText(dayNight));
            RenderTimeBlock(HudLogic.FormatTimeHudText(dayNight));
            RenderSceneBlock(HudLogic.TryGetSceneHudText(out string sceneText) ? sceneText : "Scene name");
            RenderWeatherOverhaulStageBlock(
                WeatherOverhaulIntegration.TryGetCurrentStageDisplayName(out string stageText) ? stageText : "Weather stage");

            if (hud != null && hud.m_EquipItemPopup != null && hud.m_EquipItemPopup.gameObject.activeInHierarchy)
            {
                string itemText = HudLogic.TryGetActiveItemHudData(out ActiveItemHudData item)
                    ? HudLogic.FormatActiveItemHudText(item)
                    : "100%";
                RenderActiveItemBlock(hud, itemText);
            }
            else
            {
                HideActiveItemBlock();
            }

            if (hud != null)
                RenderWeakIceTimerBlock(hud, HudLogic.FormatWeakIceTimeText(new WeakIceHudData(5f)));
        }

        internal static void EndEditorPreview()
        {
            UILabel?[] labels =
            {
                _airTemperatureLabel, _windChillLabel, _feelsLikeLabel, _bodyHeatLabel, _weightLabel,
                _movementSpeedLabel, _playerCoordinatesLabel, _windArrowLabel, _windSpeedLabel,
                _dayLabel, _timeLabel, _sceneLabel, _weatherOverhaulStageLabel, _activeItemLabel, _weakIceTimerLabel
            };

            foreach (UILabel? label in labels)
            {
                if (label != null)
                    label.color = DefaultTextColor;
            }

            if (_windArrowLabel != null)
                _windArrowLabel.transform.localRotation = Quaternion.identity;
        }

        internal static bool TryGetThinIceTimerBaseY(Panel_HUD hud, out float baseY)
        {
            baseY = 0f;
            if (hud == null || hud.m_ThinIceWidget == null)
                return false;

            UIWidget widget = hud.m_ThinIceWidget;
            baseY = widget.transform.localPosition.y - (widget.height * 0.5f);
            return true;
        }

        internal static Vector3 SharedToParentSpace(float x, float y)
        {
            if (_mainRoot == null)
                return new Vector3(x, y, 0f);

            Transform root = _mainRoot.transform;
            Vector3 scale = root.localScale;
            Vector3 origin = root.localPosition;
            return new Vector3(origin.x + x * scale.x, origin.y + y * scale.y, 0f);
        }

        internal static Vector2 ParentToSharedSpace(Vector2 parentLocal)
        {
            if (_mainRoot == null)
                return parentLocal;

            Transform root = _mainRoot.transform;
            Vector3 scale = root.localScale;
            Vector3 origin = root.localPosition;
            return new Vector2(
                (parentLocal.x - origin.x) / SafeScale(scale.x),
                (parentLocal.y - origin.y) / SafeScale(scale.y));
        }

        internal static int LegacyParentXToShared(int x)
        {
            return Mathf.RoundToInt(ParentToSharedSpace(new Vector2(x, 0f)).x);
        }

        internal static int LegacyParentYToShared(int y)
        {
            return Mathf.RoundToInt(ParentToSharedSpace(new Vector2(0f, y)).y);
        }

        private static float SafeScale(float value)
        {
            return Mathf.Abs(value) < 0.0001f ? 1f : value;
        }

        private static void MigrateToSharedPositionSpace()
        {
            ModSettings options = Settings.options;
            if (options.UsesSharedPositionSpace || _mainRoot == null)
                return;

            options.DayHudX = Settings.ClampSharedX(LegacyParentXToShared(options.DayHudX));
            options.DayHudY = Settings.ClampSharedY(LegacyParentYToShared(options.DayHudY));
            options.TimeHudX = Settings.ClampSharedX(LegacyParentXToShared(options.TimeHudX));
            options.TimeHudY = Settings.ClampSharedY(LegacyParentYToShared(options.TimeHudY));
            options.SceneHudX = Settings.ClampSharedX(LegacyParentXToShared(options.SceneHudX));
            options.SceneHudY = Settings.ClampSharedY(LegacyParentYToShared(options.SceneHudY));
            options.WeatherOverhaulStageX = Settings.ClampSharedX(LegacyParentXToShared(options.WeatherOverhaulStageX));
            options.WeatherOverhaulStageY = Settings.ClampSharedY(LegacyParentYToShared(options.WeatherOverhaulStageY));
            options.UsesSharedPositionSpace = true;
            options.SaveFromEditor();

            InterloperHudProMain.Log("Day, time, scene and weather stage positions converted to the shared position space.", false);
        }

        internal static void ConfigureEditorOverlayLabel(UILabel label, int fontSize)
        {
            ConfigureCenteredLabel(label, fontSize);
        }

        private static void ShowPreviewLabel(UILabel? label, string text)
        {
            if (label == null)
                return;

            label.text = text;
            label.gameObject.SetActive(true);
        }

        private static void ApplyFontSize(UILabel? label, int fontSize)
        {
            if (label == null)
                return;

            label.fontSize = fontSize;
            label.effectDistance = fontSize >= MainFontSize
                ? new Vector2(1.7f, 1.7f)
                : new Vector2(1.5f, 1.5f);
        }

        internal static void Reset()
        {
            if (_mainRoot != null)
            {
                UnityEngine.Object.Destroy(_mainRoot);
                _mainRoot = null;
            }

            if (_dayLabel != null)
            {
                UnityEngine.Object.Destroy(_dayLabel.gameObject);
                _dayLabel = null;
            }

            if (_timeLabel != null)
            {
                UnityEngine.Object.Destroy(_timeLabel.gameObject);
                _timeLabel = null;
            }

            if (_sceneLabel != null)
            {
                UnityEngine.Object.Destroy(_sceneLabel.gameObject);
                _sceneLabel = null;
            }

            if (_weatherOverhaulStageLabel != null)
            {
                UnityEngine.Object.Destroy(_weatherOverhaulStageLabel.gameObject);
                _weatherOverhaulStageLabel = null;
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

            if (_windRoot != null)
            {
                UnityEngine.Object.Destroy(_windRoot);
                _windRoot = null;
            }

            _windArrowLabel = null;
            _windSpeedLabel = null;
            _indoorWindSpinAngle = 0f;
            _airTemperatureLabel = null;
            _windChillLabel = null;
            _feelsLikeLabel = null;
            _bodyHeatLabel = null;
            _weightLabel = null;
            _movementSpeedLabel = null;
            _playerCoordinatesLabel = null;

            InterloperHudProMain.Log("HUD renderer reset.");
        }

        internal static void HideWindDirectionBlock()
        {
            _windRoot?.SetActive(false);

            RefreshMainRootVisibility();
        }

        internal static void HideMainBlock()
        {
            _mainRoot?.SetActive(false);
        }

        internal static void HideDayBlock()
        {
            _dayLabel?.gameObject.SetActive(false);
        }

        internal static void HideTimeBlock()
        {
            _timeLabel?.gameObject.SetActive(false);
        }

        internal static void HideSceneBlock()
        {
            _sceneLabel?.gameObject.SetActive(false);
        }

        internal static void HideWeatherOverhaulStageBlock()
        {
            _weatherOverhaulStageLabel?.gameObject.SetActive(false);
        }

        internal static void HideDayTimeBlocks()
        {
            HideDayBlock();
            HideTimeBlock();
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

            MigrateToSharedPositionSpace();

            _airTemperatureLabel = CreateMainChildLabel(AirTemperatureLabelName, AirTemperaturePosition);
            _windChillLabel = CreateMainChildLabel(WindChillLabelName, WindChillPosition);
            _feelsLikeLabel = CreateMainChildLabel(FeelsLikeLabelName, FeelsLikePosition);
            _bodyHeatLabel = CreateMainChildLabel(BodyHeatLabelName, BodyHeatPosition);
            _weightLabel = CreateMainChildLabel(WeightLabelName, WeightPosition);
            _movementSpeedLabel = CreateMainChildLabel(MovementSpeedLabelName, MovementSpeedPosition);
            _playerCoordinatesLabel = CreateMainChildLabel(PlayerCoordinatesLabelName, PlayerCoordinatesPosition);

            _windRoot = new GameObject(WindRootName);
            _windRoot.transform.SetParent(_mainRoot.transform, false);
            _windRoot.transform.localScale = Vector3.one;
            _windRoot.transform.localPosition = WindRootPosition;

            _windArrowLabel = CreateCenteredChildLabel(
                _windRoot.transform,
                WindArrowLabelName,
                WindArrowLocalPosition,
                Settings.options.WindArrowFontSize);

            _windSpeedLabel = CreateCenteredChildLabel(
                _windRoot.transform,
                WindSpeedLabelName,
                WindSpeedLocalPosition,
                Settings.options.WindSpeedFontSize);

            RefreshMainLabelPositions();

            InterloperHudProMain.Log("Main HUD anchor created.");
        }

        internal static void RenderMainBlock(
            bool hasTemperatureData,
            TemperatureHudData temperatureData,
            bool hasBodyHeatData,
            BodyHeatHudData bodyHeatData,
            bool hasWeightData,
            WeightHudData weightData,
            bool hasMovementSpeedData,
            MovementSpeedHudData movementSpeedData,
            bool hasPlayerCoordinatesData,
            PlayerCoordinatesHudData playerCoordinatesData)
        {
            RefreshMainLabelPositions();

            if (_mainRoot == null || _airTemperatureLabel == null || _windChillLabel == null || _feelsLikeLabel == null || _bodyHeatLabel == null || _weightLabel == null || _movementSpeedLabel == null || _playerCoordinatesLabel == null)
                return;

            _bodyHeatLabel.fontSize = Settings.options.BodyHeatFontSize;
            _bodyHeatLabel.effectDistance = Settings.options.BodyHeatFontSize >= MainFontSize
                ? new Vector2(1.7f, 1.7f)
                : new Vector2(1.5f, 1.5f);

            _movementSpeedLabel.fontSize = Settings.options.MovementSpeedFontSize;
            _movementSpeedLabel.effectDistance = Settings.options.MovementSpeedFontSize >= MainFontSize
                ? new Vector2(1.7f, 1.7f)
                : new Vector2(1.5f, 1.5f);

            _playerCoordinatesLabel.fontSize = Settings.options.PlayerCoordinatesFontSize;
            _playerCoordinatesLabel.effectDistance = Settings.options.PlayerCoordinatesFontSize >= MainFontSize
                ? new Vector2(1.7f, 1.7f)
                : new Vector2(1.5f, 1.5f);

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
                _bodyHeatLabel,
                Settings.options.ShowBodyHeat && hasBodyHeatData,
                hasBodyHeatData ? HudLogic.FormatBodyHeatHudText(bodyHeatData) : string.Empty,
                DefaultTextColor);

            SetLabelState(
                _weightLabel,
                Settings.options.ShowWeight && hasWeightData,
                hasWeightData ? HudLogic.FormatWeightHudText(weightData) : string.Empty,
                weightColor);

            SetLabelState(
                _movementSpeedLabel,
                Settings.options.ShowMovementSpeed && hasMovementSpeedData,
                hasMovementSpeedData ? HudLogic.FormatMovementSpeedHudText(movementSpeedData) : string.Empty,
                DefaultTextColor);

            SetLabelState(
                _playerCoordinatesLabel,
                Settings.options.ShowPlayerCoordinates && hasPlayerCoordinatesData,
                hasPlayerCoordinatesData ? HudLogic.FormatPlayerCoordinatesHudText(playerCoordinatesData) : string.Empty,
                DefaultTextColor);

            RefreshMainRootVisibility();
        }

        internal static void RenderWindDirectionBlock(WindDirectionHudData data)
        {
            if (_mainRoot == null || _windRoot == null || _windArrowLabel == null || _windSpeedLabel == null)
                return;

            _windRoot.transform.localPosition = WindRootPosition;
            _windArrowLabel.transform.localPosition = WindArrowLocalPosition;
            _windSpeedLabel.transform.localPosition = WindSpeedLocalPosition;

            _windArrowLabel.fontSize = Settings.options.WindArrowFontSize;
            _windSpeedLabel.fontSize = Settings.options.WindSpeedFontSize;

            bool showIndoors = Settings.options.ShowWindHudIndoors;
            if (!data.IsOutdoors && !showIndoors)
            {
                _windRoot.SetActive(false);
                RefreshMainRootVisibility();
                return;
            }

            bool showArrow = HudLogic.HasWindArrowContent()
                && (data.IsOutdoors || Settings.options.ShowWindDirectionIndoors);
            bool showSpeed = HudLogic.HasWindSpeedContent();

            _windRoot.SetActive(showArrow || showSpeed);

            if (!showArrow && !showSpeed)
            {
                RefreshMainRootVisibility();
                return;
            }

            Color windColor = data.IsOutdoors ? GetWindHudColor(data.Severity) : DefaultTextColor;

            _windArrowLabel.gameObject.SetActive(showArrow);
            if (showArrow)
            {
                _windArrowLabel.text = WindDirectionGlyph;
                _windArrowLabel.color = windColor;

                if (data.IsOutdoors)
                {
                    _windArrowLabel.transform.localRotation =
                        Quaternion.Euler(0f, 0f, -data.RelativeAngle);
                }
                else
                {
                    float directionMultiplier =
                        Settings.options.IndoorWindSpinDirection == WindSpinDirection.Clockwise ? -1f : 1f;

                    _indoorWindSpinAngle +=
                        Time.unscaledDeltaTime * Settings.options.IndoorWindSpinSpeed * directionMultiplier;

                    if (_indoorWindSpinAngle >= 360f || _indoorWindSpinAngle <= -360f)
                        _indoorWindSpinAngle = 0f;

                    _windArrowLabel.transform.localRotation =
                        Quaternion.Euler(0f, 0f, _indoorWindSpinAngle);
                }
            }

            _windSpeedLabel.gameObject.SetActive(showSpeed);
            if (showSpeed)
            {
                _windSpeedLabel.text = HudLogic.FormatWindSpeedText(data);
                _windSpeedLabel.color = windColor;
                _windSpeedLabel.transform.localRotation = Quaternion.identity;
            }
            else
            {
                _windSpeedLabel.text = string.Empty;
            }

            RefreshMainRootVisibility();
        }

        internal static void RenderDayBlock(string text)
        {
            UILabel label = GetOrCreateDayLabel();
            if (label == null)
            {
                HideDayBlock();
                return;
            }

            label.transform.localPosition = DayHudPosition;
            label.text = text;
            label.gameObject.SetActive(true);
        }

        internal static void RenderTimeBlock(string text)
        {
            UILabel label = GetOrCreateTimeLabel();
            if (label == null)
            {
                HideTimeBlock();
                return;
            }

            label.transform.localPosition = TimeHudPosition;
            label.text = text;
            label.gameObject.SetActive(true);
        }

        internal static void RenderSceneBlock(string text)
        {
            UILabel label = GetOrCreateSceneLabel();
            if (label == null)
            {
                HideSceneBlock();
                return;
            }

            label.transform.localPosition = SceneHudPosition;
            label.fontSize = Settings.options.SceneHudFontSize;
            label.text = text;
            label.gameObject.SetActive(true);
        }

        internal static void RenderWeatherOverhaulStageBlock(string text)
        {
            UILabel label = GetOrCreateWeatherOverhaulStageLabel();
            if (label == null)
            {
                HideWeatherOverhaulStageBlock();
                return;
            }

            label.transform.localPosition = WeatherOverhaulStageHudPosition;
            label.fontSize = Settings.options.WeatherOverhaulStageFontSize;
            label.text = text;
            label.gameObject.SetActive(true);
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

        private static UILabel CreateCenteredChildLabel(Transform parent, string name, Vector3 localPosition, int fontSize)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = localPosition;

            UILabel label = go.AddComponent<UILabel>();
            ConfigureCenteredLabel(label, fontSize);
            label.text = string.Empty;
            label.gameObject.SetActive(false);

            return label;
        }

        private static UILabel GetOrCreateDayLabel()
        {
            if (_dayLabel != null)
                return _dayLabel;

            if (_mainRoot == null)
                return null!;

            GameObject labelObject = new(DayLabelName);
            labelObject.transform.SetParent(_mainRoot.transform.parent, false);
            labelObject.transform.localScale = Vector3.one;

            _dayLabel = labelObject.AddComponent<UILabel>();
            ConfigureStandardLabel(_dayLabel, MainFontSize);

            InterloperHudProMain.Log("Day label created.");
            return _dayLabel;
        }

        private static UILabel GetOrCreateTimeLabel()
        {
            if (_timeLabel != null)
                return _timeLabel;

            if (_mainRoot == null)
                return null!;

            GameObject labelObject = new(TimeLabelName);
            labelObject.transform.SetParent(_mainRoot.transform.parent, false);
            labelObject.transform.localScale = Vector3.one;

            _timeLabel = labelObject.AddComponent<UILabel>();
            ConfigureStandardLabel(_timeLabel, MainFontSize);

            InterloperHudProMain.Log("Time label created.");
            return _timeLabel;
        }

        private static UILabel GetOrCreateSceneLabel()
        {
            if (_sceneLabel != null)
                return _sceneLabel;

            if (_mainRoot == null)
                return null!;

            GameObject labelObject = new(SceneLabelName);
            labelObject.transform.SetParent(_mainRoot.transform.parent, false);
            labelObject.transform.localScale = Vector3.one;

            _sceneLabel = labelObject.AddComponent<UILabel>();
            ConfigureStandardLabel(_sceneLabel, SmallFontSize);

            InterloperHudProMain.Log("Scene label created.");
            return _sceneLabel;
        }

        private static UILabel GetOrCreateWeatherOverhaulStageLabel()
        {
            if (_weatherOverhaulStageLabel != null)
                return _weatherOverhaulStageLabel;

            if (_mainRoot == null)
                return null!;

            GameObject labelObject = new(WeatherOverhaulStageLabelName);
            labelObject.transform.SetParent(_mainRoot.transform.parent, false);
            labelObject.transform.localScale = Vector3.one;

            _weatherOverhaulStageLabel = labelObject.AddComponent<UILabel>();
            ConfigureStandardLabel(_weatherOverhaulStageLabel, SmallFontSize);

            InterloperHudProMain.Log("WeatherOverhaul stage label created.");
            return _weatherOverhaulStageLabel;
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

        private static Color GetWindHudColor(WindHudSeverity severity)
        {
            return severity switch
            {
                WindHudSeverity.Warning => WarningTextColor,
                WindHudSeverity.Danger => DangerTextColor,
                _ => DefaultTextColor
            };
        }

        private static void RefreshMainLabelPositions()
        {
            if (_airTemperatureLabel != null)
                _airTemperatureLabel.transform.localPosition = AirTemperaturePosition;

            if (_windChillLabel != null)
                _windChillLabel.transform.localPosition = WindChillPosition;

            if (_feelsLikeLabel != null)
                _feelsLikeLabel.transform.localPosition = FeelsLikePosition;

            if (_bodyHeatLabel != null)
                _bodyHeatLabel.transform.localPosition = BodyHeatPosition;

            if (_weightLabel != null)
                _weightLabel.transform.localPosition = WeightPosition;

            if (_windRoot != null)
                _windRoot.transform.localPosition = WindRootPosition;

            if (_windSpeedLabel != null)
                _windSpeedLabel.transform.localPosition = WindSpeedLocalPosition;

            if (_movementSpeedLabel != null)
                _movementSpeedLabel.transform.localPosition = MovementSpeedPosition;

            if (_playerCoordinatesLabel != null)
                _playerCoordinatesLabel.transform.localPosition = PlayerCoordinatesPosition;
        }

        private static void RefreshMainRootVisibility()
        {
            if (_mainRoot == null)
                return;

            bool anyVisible =
                (_airTemperatureLabel != null && _airTemperatureLabel.gameObject.activeSelf) ||
                (_windChillLabel != null && _windChillLabel.gameObject.activeSelf) ||
                (_feelsLikeLabel != null && _feelsLikeLabel.gameObject.activeSelf) ||
                (_bodyHeatLabel != null && _bodyHeatLabel.gameObject.activeSelf) ||
                (_weightLabel != null && _weightLabel.gameObject.activeSelf) ||
                (_movementSpeedLabel != null && _movementSpeedLabel.gameObject.activeSelf) ||
                (_playerCoordinatesLabel != null && _playerCoordinatesLabel.gameObject.activeSelf) ||
                (_windRoot != null && _windRoot.activeSelf);

            _mainRoot.SetActive(anyVisible);
        }

    }

    // -------------------------------------------------------------------------
    //                               PATCHES
    // -------------------------------------------------------------------------
    internal static class FirstAidVitaminC
    {
        private const string WidgetName = "InterloperHudPro_VitaminC";
        private static Vector3 WidgetOffset => new(Settings.options.FirstAidVitaminCX * 0.01f, Settings.options.FirstAidVitaminCY * 0.01f, 0f);
        private static float _nextRefreshTime;

        internal static Transform? EditorWidget { get; private set; }
        internal static Transform? EditorCalories { get; private set; }
        internal static UILabel? EditorNameLabel => GetChildLabel(EditorWidget, 1);
        internal static UILabel? EditorValueLabel => GetChildLabel(EditorWidget, 2);

        private static UILabel? GetChildLabel(Transform? parent, int index)
        {
            if (parent == null || parent.childCount <= index)
                return null;

            return parent.GetChild(index).GetComponent<UILabel>();
        }

        private static bool TryGetCalories(Panel_FirstAid panel, out Transform statusBars, out Transform calories)
        {
            statusBars = null!;
            calories = null!;

            if (panel == null || panel.gameObject == null)
                return false;

            Transform panelRoot = panel.transform;
            if (panelRoot.childCount <= 2)
                return false;

            statusBars = panelRoot.GetChild(2);
            if (statusBars.childCount <= 12)
                return false;

            calories = statusBars.GetChild(12);
            return calories.childCount > 3;
        }

        private static bool TryGetVitaminC(out float amount, out float maximum)
        {
            amount = 0f;
            maximum = 0f;

            var scurvy = GameManager.GetScurvyComponent();
            var nutrition = Il2CppTLD.Player.Nutrition.Instance;

            if (scurvy == null || nutrition == null || !nutrition.IsInitialized)
                return false;

            if (!scurvy.TryGetVitaminCDefinition(out Il2CppTLD.Gameplay.NutrientDefinition nutrient) || nutrient == null)
                return false;

            amount = nutrition.GetValue(nutrient);
            maximum = nutrition.GetMaxValue(nutrient);

            return !float.IsNaN(amount) && !float.IsInfinity(amount)
                && !float.IsNaN(maximum) && !float.IsInfinity(maximum) && maximum > 0f;
        }

        private static GameObject CreateWidget(Transform statusBars, Transform calories)
        {
            GameObject widget = UnityEngine.Object.Instantiate(calories.gameObject, calories.parent);
            widget.name = WidgetName;
            widget.transform.GetChild(0).gameObject.SetActive(false);

            UILabel label = widget.transform.GetChild(1).GetComponent<UILabel>();
            UILocalize? localize = label.GetComponent<UILocalize>();
            if (localize != null)
                localize.enabled = false;

            label.text = "VITAMIN C";

            if (statusBars.Find("Blood Drug Level") == null)
            {
                Transform background = widget.transform.GetChild(3);
                Vector3 scale = background.localScale;
                scale.x -= 0.2f;
                background.localScale = scale;
            }

            return widget;
        }

        private static void ApplyValue(GameObject widget, float amount, float maximum)
        {
            UILabel value = widget.transform.GetChild(2).GetComponent<UILabel>();
            value.text = amount.ToString("0.#");

            float normalized = Mathf.Clamp01(amount / maximum);
            value.color = normalized < 0.2f
                ? new Color(0.8f, 0.2f, 0.23f, 1f)
                : normalized < 0.4f
                    ? new Color(0.95f, 0.55f, 0.15f, 1f)
                    : Color.white;
        }

        private static void Refresh(Panel_FirstAid panel)
        {
            if (HudEditor.IsEditingFirstAid)
                return;

            if (!TryGetCalories(panel, out Transform statusBars, out Transform calories))
                return;

            GameObject? widget = calories.parent.Find(WidgetName)?.gameObject;
            if (!Settings.options.ShowVitaminCFirstAid)
            {
                if (widget != null)
                    widget.SetActive(false);

                return;
            }

            if (!TryGetVitaminC(out float amount, out float maximum))
                return;

            if (widget == null)
                widget = CreateWidget(statusBars, calories);

            widget.transform.position = calories.position + WidgetOffset;
            ApplyValue(widget, amount, maximum);
            widget.SetActive(true);
        }

        internal static void RenderEditorPreview(Panel_FirstAid panel)
        {
            EditorWidget = null;
            EditorCalories = null;

            if (!TryGetCalories(panel, out Transform statusBars, out Transform calories))
                return;

            GameObject? widget = calories.parent.Find(WidgetName)?.gameObject;
            if (widget == null)
                widget = CreateWidget(statusBars, calories);

            widget.transform.position = calories.position + WidgetOffset;

            if (TryGetVitaminC(out float amount, out float maximum))
                ApplyValue(widget, amount, maximum);

            widget.SetActive(true);

            EditorWidget = widget.transform;
            EditorCalories = calories;
        }

        internal static void EndEditorPreview(Panel_FirstAid? panel)
        {
            UILabel? nameLabel = EditorNameLabel;
            UILabel? caloriesNameLabel = GetChildLabel(EditorCalories, 1);

            if (nameLabel != null)
                nameLabel.color = caloriesNameLabel != null ? caloriesNameLabel.color : Color.white;

            EditorWidget = null;
            EditorCalories = null;

            if (panel != null)
                Refresh(panel);
        }

        [HarmonyPatch(typeof(Panel_FirstAid), nameof(Panel_FirstAid.Initialize))]
        private static class InitializePatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Panel_FirstAid __instance)
            {
                Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(Panel_FirstAid), nameof(Panel_FirstAid.RefreshStatusLabels))]
        private static class RefreshStatusLabelsPatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Panel_FirstAid __instance)
            {
                Refresh(__instance);
            }
        }

        [HarmonyPatch(typeof(Panel_FirstAid), nameof(Panel_FirstAid.Update))]
        private static class UpdatePatch
        {
            private static void Postfix(Panel_FirstAid __instance)
            {
                if (!__instance.IsEnabled())
                    return;

                HudEditor.OnFirstAidUpdate(__instance);

                if (HudEditor.IsEditingFirstAid || Time.unscaledTime < _nextRefreshTime)
                    return;

                _nextRefreshTime = Time.unscaledTime + 0.5f;
                Refresh(__instance);
            }
        }
    }

    internal static class Patches
    {
        private const double TemperatureUpdateIntervalMinutes = 0.01d;
        private const double GeneralHudUpdateIntervalMinutes = 0.01d;

        private static double GetElapsedMinutes()
        {
            var timer = GameManager.GetHighResolutionTimerManager();
            return timer != null ? timer.GetElapsedMinutes() : 0d;
        }

        internal static void ForceRefresh()
        {
            MainHudPatch.LastUpdateMinutes = 0d;
            DayTimeHudPatch.LastUpdateMinutes = 0d;
            ActiveItemHudPatch.LastUpdateMinutes = 0d;
            WeakIceHudPatch.LastUpdateMinutes = 0d;
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Start))]
        private static class ResetHudRefsOnGameStart
        {
            private static void Postfix()
            {
                HudEditor.Close(save: true);
                HudRenderer.Reset();
                HudLogic.ResetWeakIceTracking();
                MajorMiseriesIntegration.Reset();
                WeatherOverhaulIntegration.Reset();
                MainHudPatch.LastUpdateMinutes = 0d;
                DayTimeHudPatch.LastUpdateMinutes = 0d;
                ActiveItemHudPatch.LastUpdateMinutes = 0d;
                WeakIceHudPatch.LastUpdateMinutes = 0d;
            }
        }

        [HarmonyPatch(typeof(StatusBar), nameof(StatusBar.Update))]
        private static class MainHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(StatusBar __instance)
            {
                bool isHudColdBar = __instance.m_IsOnHUD
                    && __instance.m_StatusBarType == StatusBar.StatusBarType.Cold;

                if (isHudColdBar)
                    HudRenderer.EnsureMainAnchor(__instance);

                if (HudEditor.IsEditingHud)
                    return;

                if (!HudLogic.HasMainHudContent())
                {
                    HudRenderer.HideMainBlock();
                    return;
                }

                if (!isHudColdBar)
                    return;

                if (HudLogic.HasWindHudContent())
                {
                    if (HudLogic.TryGetWindDirectionHudData(out WindDirectionHudData windData))
                        HudRenderer.RenderWindDirectionBlock(windData);
                    else
                        HudRenderer.HideWindDirectionBlock();
                }
                else
                {
                    HudRenderer.HideWindDirectionBlock();
                }

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < TemperatureUpdateIntervalMinutes)
                    return;

                bool needTemperatureData = HudLogic.HasTemperatureHudContent();
                bool needBodyHeatData = HudLogic.HasBodyHeatHudContent();
                bool needWeightData = Settings.options.ShowWeight;
                bool needMovementSpeedData = Settings.options.ShowMovementSpeed;
                bool needPlayerCoordinatesData = Settings.options.ShowPlayerCoordinates;

                TemperatureHudData temperatureData = default;
                BodyHeatHudData bodyHeatData = default;
                WeightHudData weightData = default;
                MovementSpeedHudData movementSpeedData = default;
                PlayerCoordinatesHudData playerCoordinatesData = default;

                bool hasTemperatureData = !needTemperatureData || HudLogic.TryGetTemperatureHudData(out temperatureData);
                bool hasBodyHeatData = !needBodyHeatData || HudLogic.TryGetBodyHeatHudData(out bodyHeatData);
                bool hasWeightData = !needWeightData || HudLogic.TryGetWeightHudData(out weightData);
                bool hasMovementSpeedData = !needMovementSpeedData || HudLogic.TryGetMovementSpeedHudData(out movementSpeedData);
                bool hasPlayerCoordinatesData = !needPlayerCoordinatesData || HudLogic.TryGetPlayerCoordinatesHudData(out playerCoordinatesData);

                HudRenderer.RenderMainBlock(
                    needTemperatureData && hasTemperatureData,
                    temperatureData,
                    needBodyHeatData && hasBodyHeatData,
                    bodyHeatData,
                    needWeightData && hasWeightData,
                    weightData,
                    needMovementSpeedData && hasMovementSpeedData,
                    movementSpeedData,
                    needPlayerCoordinatesData && hasPlayerCoordinatesData,
                    playerCoordinatesData);

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
        private static class DayTimeHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix()
            {
                if (HudEditor.IsEditingHud)
                    return;

                if (!Settings.options.ShowDay && !Settings.options.ShowTime && !Settings.options.ShowSceneName && !Settings.options.ShowWeatherOverhaulStage)
                {
                    HudRenderer.HideDayBlock();
                    HudRenderer.HideTimeBlock();
                    HudRenderer.HideSceneBlock();
                    HudRenderer.HideWeatherOverhaulStageBlock();
                    return;
                }

                double now = GetElapsedMinutes();
                if (now - LastUpdateMinutes < GeneralHudUpdateIntervalMinutes)
                    return;

                if (HudLogic.TryGetDayNightHudData(out DayNightHudData dayNightData))
                {
                    if (Settings.options.ShowDay)
                        HudRenderer.RenderDayBlock(HudLogic.FormatDayHudText(dayNightData));
                    else
                        HudRenderer.HideDayBlock();

                    if (Settings.options.ShowTime)
                        HudRenderer.RenderTimeBlock(HudLogic.FormatTimeHudText(dayNightData));
                    else
                        HudRenderer.HideTimeBlock();
                }
                else
                {
                    HudRenderer.HideDayBlock();
                    HudRenderer.HideTimeBlock();
                }

                if (Settings.options.ShowSceneName && HudLogic.TryGetSceneHudText(out string sceneText))
                    HudRenderer.RenderSceneBlock(sceneText);
                else
                    HudRenderer.HideSceneBlock();

                if (Settings.options.ShowWeatherOverhaulStage && WeatherOverhaulIntegration.TryGetCurrentStageDisplayName(out string weatherOverhaulStageText))
                    HudRenderer.RenderWeatherOverhaulStageBlock(weatherOverhaulStageText);
                else
                    HudRenderer.HideWeatherOverhaulStageBlock();

                LastUpdateMinutes = now;
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
        private static class ActiveItemHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                if (HudEditor.IsEditingHud)
                    return;

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

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
        private static class WeakIceHudPatch
        {
            internal static double LastUpdateMinutes = 0d;

            private static void Postfix(Panel_HUD __instance)
            {
                if (HudEditor.IsEditingHud)
                    return;

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

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.Enable))]
        private static class IceFishingHoleClearEnablePatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance)
            {
                UpdateBreakIceToolLabel(__instance);
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.Update))]
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