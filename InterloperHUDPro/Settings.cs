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

        [Name("Customize HUD elements")]
        [Description("Show customization sliders for HUD elements.")]
        public bool ShowLayoutOptions = false;

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

        [Name("Carried weight position")]
        [Description("Moves the carried weight display left or right. Default: 195.")]
        [Slider(0, 250, 250)]
        public int WeightX = 195;

        [Name("Held item condition position")]
        [Description("Moves the held item condition display left or right. Default: 95.")]
        [Slider(0, 100, 100)]
        public int ActiveItemConditionX = 95;

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
        [Description("Add some logs in the ML console.")]
        public bool IsLogging = false;

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            base.OnChange(field, oldValue, newValue);

            if (field.Name == nameof(ShowLayoutOptions))
                UpdateLayoutVisibility();

            if (field.Name == nameof(ShowAdvanced))
            {
                SetFieldVisible(nameof(IsLogging), ShowAdvanced);

                if (!ShowAdvanced)
                    IsLogging = false;
            }
        }

        protected override void OnConfirm()
        {
            UpdateLayoutVisibility();

            SetFieldVisible(nameof(IsLogging), ShowAdvanced);

            if (!ShowAdvanced)
                IsLogging = false;

            base.OnConfirm();
        }

        private void UpdateLayoutVisibility()
        {
            SetFieldVisible(nameof(AirTemperatureX), ShowLayoutOptions);
            SetFieldVisible(nameof(WindChillX), ShowLayoutOptions);
            SetFieldVisible(nameof(FeelsLikeX), ShowLayoutOptions);
            SetFieldVisible(nameof(WeightX), ShowLayoutOptions);
            SetFieldVisible(nameof(ActiveItemConditionX), ShowLayoutOptions);
            SetFieldVisible(nameof(ThinIceTimerFontSize), ShowLayoutOptions);
            SetFieldVisible(nameof(ThinIceTimerYOffset), ShowLayoutOptions);
        }
    }

    internal static class Settings
    {
        public static ModSettings options;

        public static void OnLoad()
        {
            options = new ModSettings();
            options.AddToModSettings("Interloper HUD PRO");

            options.SetFieldVisible(nameof(ModSettings.AirTemperatureX), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.WindChillX), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.FeelsLikeX), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.WeightX), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.ActiveItemConditionX), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.ThinIceTimerFontSize), options.ShowLayoutOptions);
            options.SetFieldVisible(nameof(ModSettings.ThinIceTimerYOffset), options.ShowLayoutOptions);

            options.SetFieldVisible(nameof(ModSettings.IsLogging), options.ShowAdvanced);

            if (!options.ShowAdvanced)
                options.IsLogging = false;
        }
    }
}