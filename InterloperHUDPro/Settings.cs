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
        public bool ShowWindHudIndoors = false;

        [Name("Show carried weight")]
        [Description("Displays your current carried weight.")]
        public bool ShowWeight = true;

        [Name("Show day and time")]
        [Description("Displays the current day and time above the HUD block.")]
        public bool ShowDayNight = true;

        [Name("Show held item condition")]
        [Description("Displays the condition of the item currently held in your hands.")]
        public bool ShowActiveItemCondition = true;

        [Name("Show ice-breaking time")]
        [Description("Displays the estimated time needed to clear an ice fishing hole next to the selected tool.")]
        public bool ShowBreakIceTime = true;

        [Name("Show thin ice break time")]
        [Description("Displays the remaining time before thin ice breaks under your feet.")]
        public bool ShowThinIceTime = true;


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
        [Description("Moves the wind display left or right. Default: 355.")]
        [Slider(-10, 2000, 2010)]
        public int WindDirectionX = 355;

        [Name("Wind HUD Y position")]
        [Description("Moves the wind display up or down. Default: -45.")]
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

        [Name("Carried weight position")]
        [Description("Moves the carried weight display left or right. Default: 195.")]
        [Slider(0, 250, 250)]
        public int WeightX = 195;


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

            bool showWindCustomization = ShowWindCustomization && WindDisplayMode != WindHudDisplayMode.None;
            bool showArrow = WindDisplayMode == WindHudDisplayMode.ArrowOnly || WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;
            bool showSpeed = WindDisplayMode == WindHudDisplayMode.SpeedOnly || WindDisplayMode == WindHudDisplayMode.ArrowAndSpeed;

            SetFieldVisible(nameof(IndoorWindSpinSpeed), showWindCustomization && ShowWindHudIndoors && showArrow);
            SetFieldVisible(nameof(IndoorWindSpinDirection), showWindCustomization && ShowWindHudIndoors && showArrow);
            SetFieldVisible(nameof(WindDirectionX), showWindCustomization);
            SetFieldVisible(nameof(WindDirectionY), showWindCustomization);
            SetFieldVisible(nameof(WindArrowFontSize), showWindCustomization && showArrow);
            SetFieldVisible(nameof(WindSpeedFontSize), showWindCustomization && showSpeed);
            SetFieldVisible(nameof(WindSpeedYOffset), showWindCustomization && showSpeed);

            SetFieldVisible(nameof(WeightX), ShowWeightCustomization && ShowWeight);
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