namespace InterloperHudPro
{
    internal class ModSettings : JsonModSettings
    {
        [Section("Displayed Information")]

        [Name("Show air temperature")]
        [Description("Displays the current outdoor temperature.")]
        public bool ShowAirTemperature = true;

        [Name("Show wind chill")]
        [Description("Displays the temperature penalty caused by the wind.")]
        public bool ShowWindChill = true;

        [Name("Show feels like temperature")]
        [Description("Displays the temperature you actually feel after clothing and other effects are applied.")]
        public bool ShowFeelsLikeTemperature = true;

        [Name("Wind HUD display mode")]
        [Description("Choose whether to display nothing, the wind arrow, the wind speed text, or both.")]
        [Choice("Nothing", "Arrow only", "Speed only", "Arrow and speed")]
        public WindHudDisplayMode WindDisplayMode = WindHudDisplayMode.ArrowAndSpeed;

        [Name("Show wind HUD indoors")]
        [Description("Keeps the selected wind HUD elements visible indoors. When the arrow is enabled indoors, it spins instead of showing a real direction.")]
        public bool ShowWindHudIndoors = true;

        [Name("Show carried weight")]
        [Description("Displays your current carried weight.")]
        public bool ShowWeight = true;

        [Name("Show movement speed")]
        [Description("Displays your current horizontal movement speed.")]
        public bool ShowMovementSpeed = true;

        [Name("Show player coordinates")]
        [Description("Displays your current world-space X, Y and Z coordinates.")]
        public bool ShowPlayerCoordinates = false;

        [Name("Show coordinate decimals")]
        [Description("Displays coordinates with two decimal places instead of rounded whole numbers.")]
        public bool ShowPlayerCoordinateDecimals = true;

        [Name("Show day")]
        [Description("Displays the current day.")]
        public bool ShowDay = true;

        [Name("Show time")]
        [Description("Displays the current time.")]
        public bool ShowTime = true;

        [Name("Show scene name")]
        [Description("Displays the name of the currently active scene.")]
        public bool ShowSceneName = false;

        [Name("Show held item condition")]
        [Description("Displays the condition of the item currently held in your hands.")]
        public bool ShowActiveItemCondition = true;

        [Name("Show ice-breaking time")]
        [Description("Displays the estimated time needed to clear an ice fishing hole next to the selected tool.")]
        public bool ShowBreakIceTime = true;

        [Name("Show thin ice break time")]
        [Description("Displays the remaining time before thin ice breaks under your feet.")]
        public bool ShowThinIceTime = true;

        [Section("External mods")]

        [Name("Show MajorMiseries body heat")]
        [Description("Displays the internal Body Heat tracked by MajorMiseries when its Body Heat system is enabled.")]
        public bool ShowBodyHeat = false;


        [Section("HUD Customization")]

        [Name("Customize temperature HUD")]
        [Description("Show customization settings for the temperature HUD elements.")]
        public bool ShowTemperatureCustomization = false;

        [Name("Air temperature position")]
        [Description("Moves the air temperature display left or right. Default: 0.")]
        [Slider(0, 250, 250)]
        public int AirTemperatureX = 0;

        [Name("Wind chill position")]
        [Description("Moves the wind chill display left or right. Default: 60.")]
        [Slider(0, 250, 250)]
        public int WindChillX = 60;

        [Name("Feels like temperature position")]
        [Description("Moves the feels like temperature display left or right. Default: 120.")]
        [Slider(0, 250, 250)]
        public int FeelsLikeX = 120;

        [Name("Customize player coordinates HUD")]
        [Description("Show customization settings for the player coordinates display.")]
        public bool ShowPlayerCoordinatesCustomization = false;

        [Name("Player coordinates HUD X position")]
        [Description("Moves the player coordinates display left or right. Default: 140.")]
        [Slider(-50, 2000, 2050)]
        public int PlayerCoordinatesX = 140;

        [Name("Player coordinates HUD Y position")]
        [Description("Moves the player coordinates display up or down. Default: -97.")]
        [Slider(-200, 1005, 1205)]
        public int PlayerCoordinatesY = -97;

        [Name("Player coordinates HUD size")]
        [Description("Controls the size of the player coordinates text. Default: 20.")]
        [Slider(14, 48, 34)]
        public int PlayerCoordinatesFontSize = 20;

        [Name("Customize day/time HUD")]
        [Description("Show customization settings for the day and time displays.")]
        public bool ShowDayTimeCustomization = false;

        [Name("Day HUD X position")]
        [Description("Moves the day display anchor left or right. Default: -20.")]
        [Slider(-50, 2000, 2050)]
        public int DayHudX = -20;

        [Name("Day HUD Y position")]
        [Description("Moves the day display anchor up or down. Default: 95.")]
        [Slider(-100, 1005, 1105)]
        public int DayHudY = 95;

        [Name("Time HUD X position")]
        [Description("Moves the time display anchor left or right. Default: 100.")]
        [Slider(-10, 2000, 2010)]
        public int TimeHudX = 100;

        [Name("Time HUD Y position")]
        [Description("Moves the time display anchor up or down. Default: 95.")]
        [Slider(-100, 1005, 1105)]
        public int TimeHudY = 95;

        [Name("Customize scene HUD")]
        [Description("Show customization settings for the scene display.")]
        public bool ShowSceneCustomization = false;

        [Name("Scene HUD X position")]
        [Description("Moves the scene name display left or right. Default: -20.")]
        [Slider(-50, 2000, 2050)]
        public int SceneHudX = 30;

        [Name("Scene HUD Y position")]
        [Description("Moves the scene name display up or down. Default: 125.")]
        [Slider(-100, 1005, 1105)]
        public int SceneHudY = -95;

        [Name("Scene HUD size")]
        [Description("Controls the size of the scene name text. Default: 20.")]
        [Slider(14, 48, 34)]
        public int SceneHudFontSize = 20;

        [Name("Customize wind HUD")]
        [Description("Show customization settings for the wind direction and wind speed HUD.")]
        public bool ShowWindCustomization = false;

        [Name("Indoor wind spin speed")]
        [Description("Rotation speed of the wind arrow when displayed indoors. Default: 250.")]
        [Slider(30, 360, 330)]
        public int IndoorWindSpinSpeed = 250;

        [Name("Indoor wind spin direction")]
        [Description("Controls whether the wind arrow spins clockwise or counter-clockwise indoors.")]
        [Choice("Clockwise", "Counter-clockwise")]
        public WindSpinDirection IndoorWindSpinDirection = WindSpinDirection.Clockwise;

        [Name("Wind HUD X position")]
        [Description("Moves the wind display anchor left or right. Default: 355.")]
        [Slider(-10, 2000, 2010)]
        public int WindDirectionX = 355;

        [Name("Wind HUD Y position")]
        [Description("Moves the wind display anchor up or down. Default: -45.")]
        [Slider(-100, 1005, 1105)]
        public int WindDirectionY = -45;

        [Name("Wind arrow size")]
        [Description("Controls the size of the wind arrow. Default: 50.")]
        [Slider(20, 72, 52)]
        public int WindArrowFontSize = 50;

        [Name("Wind speed size")]
        [Description("Controls the size of the wind speed text. Default: 26.")]
        [Slider(14, 36, 22)]
        public int WindSpeedFontSize = 26;

        [Name("Wind speed Y offset")]
        [Description("Vertical distance between the arrow and the wind speed text. Default: 50.")]
        [Slider(10, 60, 50)]
        public int WindSpeedYOffset = 50;

        [Name("Customize weight HUD")]
        [Description("Show customization settings for the carried weight display.")]
        public bool ShowWeightCustomization = false;

        [Name("Carried weight X position")]
        [Description("Moves the carried weight display left or right. Default: 195.")]
        [Slider(-50, 2000, 2050)]
        public int WeightX = 195;

        [Name("Carried weight Y position")]
        [Description("Moves the carried weight display up or down. Default: 0.")]
        [Slider(-100, 1005, 1105)]
        public int WeightY = 0;

        [Name("Customize movement speed HUD")]
        [Description("Show customization settings for the movement speed display.")]
        public bool ShowMovementSpeedCustomization = false;

        [Name("Movement speed X position")]
        [Description("Moves the movement speed display left or right. Default: 50.")]
        [Slider(-10, 2000, 2010)]
        public int MovementSpeedX = 50;

        [Name("Movement speed Y position")]
        [Description("Moves the movement speed display up or down. Default: -97.")]
        [Slider(-150, 1005, 1155)]
        public int MovementSpeedY = -97;

        [Name("Movement speed size")]
        [Description("Controls the size of the movement speed text. Default: 22.")]
        [Slider(14, 48, 34)]
        public int MovementSpeedFontSize = 22;

        [Name("Customize held item HUD")]
        [Description("Show customization settings for the held item condition display.")]
        public bool ShowHeldItemCustomization = false;

        [Name("Held item condition position")]
        [Description("Moves the held item condition display left or right. Default: 95.")]
        [Slider(0, 100, 100)]
        public int ActiveItemConditionX = 95;

        [Name("Customize thin ice HUD")]
        [Description("Show customization settings for the thin ice timer.")]
        public bool ShowThinIceCustomization = false;

        [Name("Thin ice timer size")]
        [Description("Adjusts the font size of the thin ice timer. Default: 15.")]
        [Slider(8, 24, 17)]
        public int ThinIceTimerFontSize = 15;

        [Name("Thin ice timer vertical offset")]
        [Description("Moves the thin ice timer farther below the warning panel. Default: 210.")]
        [Slider(120, 300, 180)]
        public int ThinIceTimerYOffset = 210;

        [Section("External mods HUD Customization")]

        [Name("Customize body heat HUD")]
        [Description("Show customization settings for the MajorM Body Heat display.")]
        public bool ShowBodyHeatCustomization = false;

        [Name("Body heat HUD X position")]
        [Description("Moves the Body Heat display left or right. Default: 50.")]
        [Slider(-50, 2000, 2050)]
        public int BodyHeatX = 50;

        [Name("Body heat HUD Y position")]
        [Description("Moves the Body Heat display up or down. Default: -97.")]
        [Slider(-150, 1005, 1155)]
        public int BodyHeatY = -97;

        [Name("Body heat HUD size")]
        [Description("Controls the size of the Body Heat text. Default: 22.")]
        [Slider(14, 48, 34)]
        public int BodyHeatFontSize = 22;

        [Section("Advanced")]

        [Name("Show advanced options")]
        [Description("Reveals developer / debug settings.")]
        public bool ShowAdvanced = false;

        [Name("ML Logging")]
        [Description("Add logs for ModData/Boredom/Depression behavior in the ML console.")]
        public bool IsLogging = false;

        internal enum WindSpinDirection
        {
            Clockwise,
            CounterClockwise
        }

        internal enum WindHudDisplayMode
        {
            None,
            ArrowOnly,
            SpeedOnly,
            ArrowAndSpeed
        }

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            base.OnChange(field, oldValue, newValue);
            RefreshVisibility();
        }

        protected override void OnConfirm()
        {
            RefreshVisibility();
            base.OnConfirm();
        }

        internal void RefreshVisibility()
        {
            bool showTemperatureCustomization = ShowTemperatureCustomization;
            SetFieldVisible(nameof(AirTemperatureX), showTemperatureCustomization && ShowAirTemperature);
            SetFieldVisible(nameof(WindChillX), showTemperatureCustomization && ShowWindChill);
            SetFieldVisible(nameof(FeelsLikeX), showTemperatureCustomization && ShowFeelsLikeTemperature);

            SetFieldVisible(nameof(BodyHeatX), ShowBodyHeatCustomization && ShowBodyHeat);
            SetFieldVisible(nameof(BodyHeatY), ShowBodyHeatCustomization && ShowBodyHeat);
            SetFieldVisible(nameof(BodyHeatFontSize), ShowBodyHeatCustomization && ShowBodyHeat);

            SetFieldVisible(nameof(ShowPlayerCoordinateDecimals), ShowPlayerCoordinates);
            SetFieldVisible(nameof(PlayerCoordinatesX), ShowPlayerCoordinatesCustomization && ShowPlayerCoordinates);
            SetFieldVisible(nameof(PlayerCoordinatesY), ShowPlayerCoordinatesCustomization && ShowPlayerCoordinates);
            SetFieldVisible(nameof(PlayerCoordinatesFontSize), ShowPlayerCoordinatesCustomization && ShowPlayerCoordinates);

            bool showWindCustomization = ShowWindCustomization && WindDisplayMode != WindHudDisplayMode.None;
            bool showArrow = WindDisplayMode == WindHudDisplayMode.ArrowOnly || WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;
            bool showSpeed = WindDisplayMode == WindHudDisplayMode.SpeedOnly || WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;

            SetFieldVisible(nameof(DayHudX), ShowDayTimeCustomization && ShowDay);
            SetFieldVisible(nameof(DayHudY), ShowDayTimeCustomization && ShowDay);
            SetFieldVisible(nameof(TimeHudX), ShowDayTimeCustomization && ShowTime);
            SetFieldVisible(nameof(TimeHudY), ShowDayTimeCustomization && ShowTime);

            SetFieldVisible(nameof(SceneHudX), ShowSceneCustomization && ShowSceneName);
            SetFieldVisible(nameof(SceneHudY), ShowSceneCustomization && ShowSceneName);
            SetFieldVisible(nameof(SceneHudFontSize), ShowSceneCustomization && ShowSceneName);

            SetFieldVisible(nameof(IndoorWindSpinSpeed), showWindCustomization && ShowWindHudIndoors && showArrow);
            SetFieldVisible(nameof(IndoorWindSpinDirection), showWindCustomization && ShowWindHudIndoors && showArrow);
            SetFieldVisible(nameof(WindDirectionX), showWindCustomization);
            SetFieldVisible(nameof(WindDirectionY), showWindCustomization);
            SetFieldVisible(nameof(WindArrowFontSize), showWindCustomization && showArrow);
            SetFieldVisible(nameof(WindSpeedFontSize), showWindCustomization && showSpeed);
            SetFieldVisible(nameof(WindSpeedYOffset), showWindCustomization && showSpeed);

            SetFieldVisible(nameof(WeightX), ShowWeightCustomization && ShowWeight);
            SetFieldVisible(nameof(WeightY), ShowWeightCustomization && ShowWeight);
            SetFieldVisible(nameof(MovementSpeedX), ShowMovementSpeedCustomization && ShowMovementSpeed);
            SetFieldVisible(nameof(MovementSpeedY), ShowMovementSpeedCustomization && ShowMovementSpeed);
            SetFieldVisible(nameof(MovementSpeedFontSize), ShowMovementSpeedCustomization && ShowMovementSpeed);
            SetFieldVisible(nameof(ActiveItemConditionX), ShowHeldItemCustomization && ShowActiveItemCondition);
            SetFieldVisible(nameof(ThinIceTimerFontSize), ShowThinIceCustomization && ShowThinIceTime);
            SetFieldVisible(nameof(ThinIceTimerYOffset), ShowThinIceCustomization && ShowThinIceTime);

            SetFieldVisible(nameof(IsLogging), ShowAdvanced);

            if (!ShowAdvanced)
                IsLogging = false;
        }
    }

    internal static class Settings
    {
        public static ModSettings options;

        public static void OnLoad()
        {
            options = new ModSettings();
            options.AddToModSettings("Interloper HUD PRO");
            options.RefreshVisibility();

            if (!options.ShowAdvanced)
                options.IsLogging = false;
        }
    }
}