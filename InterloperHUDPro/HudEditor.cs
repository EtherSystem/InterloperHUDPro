namespace InterloperHudPro
{
    // -------------------------------------------------------------------------
    //                        VISUAL LAYOUT EDITOR
    // -------------------------------------------------------------------------
    internal static class HudEditor
    {
        private enum EditorMode
        {
            None,
            Hud,
            FirstAid
        }

        private const string TagLabelName = "InterloperHudPro_EditorTag";

        private const float HitPaddingPixels = 6f;
        private const float ContextFreshnessSeconds = 0.5f;
        private const float NudgeRepeatDelay = 0.35f;
        private const float NudgeRepeatInterval = 0.04f;
        private const int OverlayDepth = 500;

        private static readonly Color EditColor = new(0.55f, 0.85f, 1f, 1f);
        private static readonly Color HoverColor = new(1f, 1f, 0.65f, 1f);
        private static readonly Color SelectedColor = new(1f, 0.78f, 0.2f, 1f);

        private static EditorMode _mode = EditorMode.None;

        internal static bool IsActive => _mode != EditorMode.None;
        internal static bool IsEditingHud => _mode == EditorMode.Hud;
        internal static bool IsEditingFirstAid => _mode == EditorMode.FirstAid;

        private static Panel_HUD? _hud;
        private static float _lastHudUpdateTime = -100f;
        private static Panel_FirstAid? _firstAid;
        private static float _lastFirstAidUpdateTime = -100f;

        private static UnityEngine.Camera? _uiCamera;

        private static Element? _hovered;
        private static Element? _selected;
        private static bool _dragging;
        private static Vector2 _dragGrabOffset;

        private static UnityEngine.KeyCode _heldArrow = UnityEngine.KeyCode.None;
        private static float _nextNudgeRepeatTime;

        private static UILabel? _tagLabel;

        private static readonly Dictionary<Axis, int> _snapshot = new();

        private static bool _playerLockedByEditor;

        // -------------------------------------------------------------------------
        //                          EDITABLE DISPLAYS
        // -------------------------------------------------------------------------
        private sealed class Axis
        {
            private readonly Func<ModSettings, int> _get;
            private readonly Action<ModSettings, int> _set;

            private readonly Func<int> _default;

            internal readonly int Min;
            internal readonly int Max;

            internal Axis(Func<ModSettings, int> get, Action<ModSettings, int> set, int min, int max, int defaultValue)
                : this(get, set, min, max, () => defaultValue)
            {
            }

            internal Axis(Func<ModSettings, int> get, Action<ModSettings, int> set, int min, int max, Func<int> defaultValue)
            {
                _get = get;
                _set = set;
                Min = min;
                Max = max;
                _default = defaultValue;
            }

            internal int Default => _default();

            internal int Get() => _get(Settings.options);

            internal void Set(int value) => _set(Settings.options, Mathf.Clamp(value, Min, Max));
        }

        private sealed class Element
        {
            internal readonly string Name;
            internal readonly Func<Transform?> Target;
            internal readonly Func<UIWidget?[]> Bounds;
            internal readonly Func<UILabel?[]> Highlights;
            internal readonly Axis? X;
            internal readonly Axis? Y;
            internal readonly Func<Vector2, Vector2>? LocalToSetting;
            internal readonly bool InvertY;

            internal Element(
                string name,
                Func<Transform?> target,
                Func<UIWidget?[]> bounds,
                Func<UILabel?[]> highlights,
                Axis? x,
                Axis? y,
                Func<Vector2, Vector2>? localToSetting = null,
                bool invertY = false)
            {
                Name = name;
                Target = target;
                Bounds = bounds;
                Highlights = highlights;
                X = x;
                Y = y;
                LocalToSetting = localToSetting;
                InvertY = invertY;
            }

            internal string AxisHint => X != null && Y != null
                ? string.Empty
                : X != null ? " (horizontal only)" : " (vertical only)";

            internal IEnumerable<Axis> Axes
            {
                get
                {
                    if (X != null) yield return X;
                    if (Y != null) yield return Y;
                }
            }

            internal void ApplyLocalPosition(Vector2 local)
            {
                Vector2 setting = LocalToSetting != null ? LocalToSetting(local) : local;

                X?.Set(Mathf.RoundToInt(setting.x));
                Y?.Set(Mathf.RoundToInt(setting.y));
            }

            internal void Nudge(int dx, int dy)
            {
                if (X != null && dx != 0)
                    X.Set(X.Get() + dx);

                if (Y != null && dy != 0)
                    Y.Set(Y.Get() + (InvertY ? -dy : dy));
            }

            internal void ResetToDefault()
            {
                foreach (Axis axis in Axes)
                    axis.Set(axis.Default);
            }
        }

        private static Element HudLabel(string name, Func<UILabel?> label, Axis? x, Axis? y, Func<Vector2, Vector2>? localToSetting = null, bool invertY = false)
        {
            return new Element(
                name,
                () => { UILabel? l = label(); return l != null ? l.transform : null; },
                () => new UIWidget?[] { label() },
                () => new[] { label() },
                x,
                y,
                localToSetting,
                invertY);
        }

        private static Vector2 WindSpeedLocalToSetting(Vector2 local)
        {
            return new Vector2(0f, -local.y);
        }

        private static Vector2 ThinIceLocalToSetting(Vector2 local)
        {
            float baseY = _hud != null && HudRenderer.TryGetThinIceTimerBaseY(_hud, out float value) ? value : 0f;
            return new Vector2(0f, baseY - local.y);
        }

        private static Vector2 VitaminCLocalToSetting(Vector2 local)
        {
            Transform? widget = FirstAidVitaminC.EditorWidget;
            Transform? calories = FirstAidVitaminC.EditorCalories;

            if (widget == null || calories == null || widget.parent == null)
                return new Vector2(Settings.options.FirstAidVitaminCX, Settings.options.FirstAidVitaminCY);

            Vector3 world = widget.parent.TransformPoint(new Vector3(local.x, local.y, widget.localPosition.z));
            Vector3 delta = world - calories.position;
            return new Vector2(delta.x * 100f, delta.y * 100f);
        }

        private static Axis SharedX(Func<ModSettings, int> get, Action<ModSettings, int> set, int defaultValue)
        {
            return new Axis(get, set, Settings.SharedMinX, Settings.SharedMaxX, defaultValue);
        }

        private static Axis SharedY(Func<ModSettings, int> get, Action<ModSettings, int> set, int defaultValue)
        {
            return new Axis(get, set, Settings.SharedMinY, Settings.SharedMaxY, defaultValue);
        }

        private static Axis LegacyX(Func<ModSettings, int> get, Action<ModSettings, int> set, int legacyDefault)
        {
            return new Axis(get, set, Settings.SharedMinX, Settings.SharedMaxX, () => Settings.ClampSharedX(HudRenderer.LegacyParentXToShared(legacyDefault)));
        }

        private static Axis LegacyY(Func<ModSettings, int> get, Action<ModSettings, int> set, int legacyDefault)
        {
            return new Axis(get, set, Settings.SharedMinY, Settings.SharedMaxY, () => Settings.ClampSharedY(HudRenderer.LegacyParentYToShared(legacyDefault)));
        }

        private static readonly Element[] HudElements =
        {
            HudLabel("Air temperature", () => HudRenderer.AirTemperatureLabel,
                SharedX(o => o.AirTemperatureX, (o, v) => o.AirTemperatureX = v, 0),
                SharedY(o => o.AirTemperatureY, (o, v) => o.AirTemperatureY = v, 0)),

            HudLabel("Wind chill", () => HudRenderer.WindChillLabel,
                SharedX(o => o.WindChillX, (o, v) => o.WindChillX = v, 60),
                SharedY(o => o.WindChillY, (o, v) => o.WindChillY = v, 0)),

            HudLabel("Feels like temperature", () => HudRenderer.FeelsLikeLabel,
                SharedX(o => o.FeelsLikeX, (o, v) => o.FeelsLikeX = v, 120),
                SharedY(o => o.FeelsLikeY, (o, v) => o.FeelsLikeY = v, 0)),

            HudLabel("Carried weight", () => HudRenderer.WeightLabel,
                SharedX(o => o.WeightX, (o, v) => o.WeightX = v, 195),
                SharedY(o => o.WeightY, (o, v) => o.WeightY = v, 0)),

            HudLabel("Movement speed", () => HudRenderer.MovementSpeedLabel,
                SharedX(o => o.MovementSpeedX, (o, v) => o.MovementSpeedX = v, 50),
                SharedY(o => o.MovementSpeedY, (o, v) => o.MovementSpeedY = v, -97)),

            HudLabel("Player coordinates", () => HudRenderer.PlayerCoordinatesLabel,
                SharedX(o => o.PlayerCoordinatesX, (o, v) => o.PlayerCoordinatesX = v, 140),
                SharedY(o => o.PlayerCoordinatesY, (o, v) => o.PlayerCoordinatesY = v, -97)),

            new Element("Wind HUD",
                () => HudRenderer.WindRootTransform,
                () => new UIWidget?[] { HudRenderer.WindArrowLabel },
                () => new[] { HudRenderer.WindArrowLabel },
                SharedX(o => o.WindDirectionX, (o, v) => o.WindDirectionX = v, 355),
                SharedY(o => o.WindDirectionY, (o, v) => o.WindDirectionY = v, -45)),

            HudLabel("Wind speed (distance below arrow)", () => HudRenderer.WindSpeedLabel,
                null,
                new Axis(o => o.WindSpeedYOffset, (o, v) => o.WindSpeedYOffset = v, 10, 60, 50),
                WindSpeedLocalToSetting,
                invertY: true),

            HudLabel("Day", () => HudRenderer.DayLabel,
                LegacyX(o => o.DayHudX, (o, v) => o.DayHudX = v, -20),
                LegacyY(o => o.DayHudY, (o, v) => o.DayHudY = v, 95),
                HudRenderer.ParentToSharedSpace),

            HudLabel("Time", () => HudRenderer.TimeLabel,
                LegacyX(o => o.TimeHudX, (o, v) => o.TimeHudX = v, 100),
                LegacyY(o => o.TimeHudY, (o, v) => o.TimeHudY = v, 95),
                HudRenderer.ParentToSharedSpace),

            HudLabel("Scene name", () => HudRenderer.SceneLabel,
                LegacyX(o => o.SceneHudX, (o, v) => o.SceneHudX = v, 30),
                LegacyY(o => o.SceneHudY, (o, v) => o.SceneHudY = v, -95),
                HudRenderer.ParentToSharedSpace),

            HudLabel("Body heat (MajorMiseries)", () => HudRenderer.BodyHeatLabel,
                SharedX(o => o.BodyHeatX, (o, v) => o.BodyHeatX = v, 50),
                SharedY(o => o.BodyHeatY, (o, v) => o.BodyHeatY = v, -97)),

            HudLabel("Weather stage (WeatherOverhaul)", () => HudRenderer.WeatherOverhaulStageLabel,
                LegacyX(o => o.WeatherOverhaulStageX, (o, v) => o.WeatherOverhaulStageX = v, 10),
                LegacyY(o => o.WeatherOverhaulStageY, (o, v) => o.WeatherOverhaulStageY = v, -92),
                HudRenderer.ParentToSharedSpace),

            HudLabel("Held item condition", () => HudRenderer.ActiveItemLabel,
                new Axis(o => o.ActiveItemConditionX, (o, v) => o.ActiveItemConditionX = v, 0, 100, 95),
                null),

            HudLabel("Thin ice timer", () => HudRenderer.WeakIceTimerLabel,
                null,
                new Axis(o => o.ThinIceTimerYOffset, (o, v) => o.ThinIceTimerYOffset = v, 120, 300, 210),
                ThinIceLocalToSetting,
                invertY: true),
        };

        private static readonly Element[] FirstAidElements =
        {
            new Element("Vitamin C",
                () => FirstAidVitaminC.EditorWidget,
                () => new UIWidget?[] { FirstAidVitaminC.EditorNameLabel, FirstAidVitaminC.EditorValueLabel },
                () => new[] { FirstAidVitaminC.EditorNameLabel, FirstAidVitaminC.EditorValueLabel },
                new Axis(o => o.FirstAidVitaminCX, (o, v) => o.FirstAidVitaminCX = v, -500, 500, 35),
                new Axis(o => o.FirstAidVitaminCY, (o, v) => o.FirstAidVitaminCY = v, -500, 500, 98),
                VitaminCLocalToSetting),
        };

        private static Element[] CurrentElements => _mode switch
        {
            EditorMode.Hud => HudElements,
            EditorMode.FirstAid => FirstAidElements,
            _ => Array.Empty<Element>()
        };

        // -------------------------------------------------------------------------
        //                              LIFECYCLE
        // -------------------------------------------------------------------------
        internal static void OnUpdate()
        {
            if (Settings.options == null)
                return;

            if (IsActive)
            {
                if (!IsContextAlive(_mode))
                {
                    Close(save: true);
                    return;
                }

                HandleInput();
                return;
            }

            UnityEngine.KeyCode key = Settings.options.HudEditorKey;
            if (key == UnityEngine.KeyCode.None || !InputManager.GetKeyDown(InputManager.m_CurrentContext, key))
                return;

            if (IsContextAlive(EditorMode.FirstAid))
            {
                Open(EditorMode.FirstAid);
                return;
            }

            if (IsContextAlive(EditorMode.Hud))
            {
                Open(EditorMode.Hud);
                return;
            }

            InterloperHudProMain.Log("Layout editor: neither the in-game HUD nor the First Aid panel is available right now.", false);
        }

        internal static void OnHudUpdate(Panel_HUD hud)
        {
            _hud = hud;
            _lastHudUpdateTime = Time.unscaledTime;

            if (!IsEditingHud)
                return;

            HudRenderer.RenderEditorPreview(hud);
            ApplyHighlights();
            UpdateTag(hud.transform);
        }

        internal static void OnFirstAidUpdate(Panel_FirstAid panel)
        {
            _firstAid = panel;
            _lastFirstAidUpdateTime = Time.unscaledTime;

            if (!IsEditingFirstAid)
                return;

            FirstAidVitaminC.RenderEditorPreview(panel);
            ApplyHighlights();
            UpdateTag(panel.transform);
        }

        private static bool IsContextAlive(EditorMode mode)
        {
            float now = Time.unscaledTime;

            return mode switch
            {
                EditorMode.Hud => HudRenderer.HasMainRoot
                    && _hud != null
                    && now - _lastHudUpdateTime <= ContextFreshnessSeconds,
                EditorMode.FirstAid => _firstAid != null
                    && now - _lastFirstAidUpdateTime <= ContextFreshnessSeconds,
                _ => false
            };
        }

        private static void Open(EditorMode mode)
        {
            _mode = mode;
            _uiCamera = null;

            if (GetUiCamera() == null)
            {
                _mode = EditorMode.None;
                InterloperHudProMain.Log("Layout editor: no UI camera found.", false);
                return;
            }

            _snapshot.Clear();
            foreach (Element element in CurrentElements)
            {
                foreach (Axis axis in element.Axes)
                    _snapshot[axis] = axis.Get();
            }

            _hovered = null;
            _selected = null;
            _dragging = false;
            _heldArrow = UnityEngine.KeyCode.None;

            if (mode == EditorMode.Hud)
            {
                LockPlayer();
                InterfaceManager.UpdateMousePointerState();
            }

            InterloperHudProMain.Log($"Layout editor opened ({mode}).", false);
        }

        internal static void Close(bool save)
        {
            if (!IsActive)
                return;

            EditorMode closedMode = _mode;
            _mode = EditorMode.None;
            _dragging = false;
            _hovered = null;
            _selected = null;
            _uiCamera = null;

            if (!save)
            {
                foreach (KeyValuePair<Axis, int> entry in _snapshot)
                    entry.Key.Set(entry.Value);
            }

            bool changed = false;
            foreach (KeyValuePair<Axis, int> entry in _snapshot)
            {
                if (entry.Key.Get() != entry.Value)
                {
                    changed = true;
                    break;
                }
            }

            _snapshot.Clear();

            if (changed)
                Settings.options.SaveFromEditor();

            DestroyTag();

            if (closedMode == EditorMode.Hud)
            {
                UnlockPlayer();
                InterfaceManager.UpdateMousePointerState();
                HudRenderer.EndEditorPreview();
                Patches.ForceRefresh();
            }
            else if (closedMode == EditorMode.FirstAid)
            {
                FirstAidVitaminC.EndEditorPreview(_firstAid);
            }

            InterloperHudProMain.Log(
                save ? (changed ? "Layout editor closed, positions saved." : "Layout editor closed, nothing changed.")
                     : "Layout editor cancelled, positions restored.",
                false);
        }

        // -------------------------------------------------------------------------
        //                                INPUT
        // -------------------------------------------------------------------------
        private static readonly UnityEngine.KeyCode[] ArrowKeys =
        {
            UnityEngine.KeyCode.LeftArrow,
            UnityEngine.KeyCode.RightArrow,
            UnityEngine.KeyCode.UpArrow,
            UnityEngine.KeyCode.DownArrow
        };

        private static void HandleInput()
        {
            UnityEngine.KeyCode editorKey = Settings.options.HudEditorKey;

            if ((editorKey != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(editorKey))
                || UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return)
                || UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
            {
                Close(save: true);
                return;
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Backspace))
            {
                Close(save: false);
                return;
            }

            UnityEngine.Camera? cam = GetUiCamera();
            if (cam == null)
                return;

            bool shift = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift)
                || UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);

            Vector3 mouse = UnityEngine.Input.mousePosition;
            _hovered = _dragging ? _selected : FindElementUnderMouse(cam, mouse);

            if (UnityEngine.Input.GetMouseButtonDown(0) && _hovered != null)
            {
                Transform? target = _hovered.Target();
                if (target != null && target.parent != null)
                {
                    _selected = _hovered;
                    _dragging = true;
                    _dragGrabOffset = (Vector2)target.localPosition - MouseToLocal(cam, target.parent, mouse);
                }
            }

            if (_dragging)
            {
                Transform? target = _selected?.Target();
                if (_selected == null || target == null || target.parent == null || !UnityEngine.Input.GetMouseButton(0))
                    _dragging = false;
                else
                    _selected.ApplyLocalPosition(MouseToLocal(cam, target.parent, mouse) + _dragGrabOffset);
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Tab) && !_dragging)
                SelectNext();

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.R) && !_dragging)
            {
                if (shift)
                {
                    foreach (Element element in CurrentElements)
                        element.ResetToDefault();
                }
                else
                {
                    _selected?.ResetToDefault();
                }
            }

            if (_selected != null && !_dragging)
                HandleNudge(_selected, shift ? 10 : 1);
        }

        private static void HandleNudge(Element element, int step)
        {
            UnityEngine.KeyCode key = UnityEngine.KeyCode.None;

            foreach (UnityEngine.KeyCode arrow in ArrowKeys)
            {
                if (!UnityEngine.Input.GetKeyDown(arrow))
                    continue;

                key = arrow;
                _heldArrow = arrow;
                _nextNudgeRepeatTime = Time.unscaledTime + NudgeRepeatDelay;
                break;
            }

            if (key == UnityEngine.KeyCode.None && _heldArrow != UnityEngine.KeyCode.None)
            {
                if (!UnityEngine.Input.GetKey(_heldArrow))
                {
                    _heldArrow = UnityEngine.KeyCode.None;
                }
                else if (Time.unscaledTime >= _nextNudgeRepeatTime)
                {
                    key = _heldArrow;
                    _nextNudgeRepeatTime = Time.unscaledTime + NudgeRepeatInterval;
                }
            }

            switch (key)
            {
                case UnityEngine.KeyCode.LeftArrow: element.Nudge(-step, 0); break;
                case UnityEngine.KeyCode.RightArrow: element.Nudge(step, 0); break;
                case UnityEngine.KeyCode.UpArrow: element.Nudge(0, step); break;
                case UnityEngine.KeyCode.DownArrow: element.Nudge(0, -step); break;
            }
        }

        private static void SelectNext()
        {
            List<Element> available = new();
            foreach (Element element in CurrentElements)
            {
                if (IsAvailable(element))
                    available.Add(element);
            }

            if (available.Count == 0)
            {
                _selected = null;
                return;
            }

            int index = _selected != null ? available.IndexOf(_selected) : -1;
            _selected = available[(index + 1) % available.Count];
        }

        private static Element? FindElementUnderMouse(UnityEngine.Camera cam, Vector3 mouse)
        {
            Element? best = null;
            float bestArea = float.MaxValue;
            Vector2 point = new(mouse.x, mouse.y);

            foreach (Element element in CurrentElements)
            {
                if (!IsAvailable(element) || !TryGetElementScreenRect(element, cam, out UnityEngine.Rect rect))
                    continue;

                if (!rect.Contains(point))
                    continue;

                float area = rect.width * rect.height;
                if (area < bestArea)
                {
                    best = element;
                    bestArea = area;
                }
            }

            return best;
        }

        private static bool IsAvailable(Element element)
        {
            if (element.Target() == null)
                return false;

            foreach (UIWidget? widget in element.Bounds())
            {
                if (widget != null && widget.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        // -------------------------------------------------------------------------
        //                              DRAWING
        // -------------------------------------------------------------------------
        private static void ApplyHighlights()
        {
            foreach (Element element in CurrentElements)
            {
                Color color = element == _selected
                    ? SelectedColor
                    : element == _hovered ? HoverColor : EditColor;

                foreach (UILabel? label in element.Highlights())
                {
                    if (label != null)
                        label.color = color;
                }
            }
        }

        private static void UpdateTag(Transform parent)
        {
            UnityEngine.Camera? cam = GetUiCamera();
            if (cam == null)
                return;

            if (_tagLabel == null || _tagLabel.transform.parent != parent)
            {
                DestroyTag();
                _tagLabel = CreateTagLabel(parent);
            }

            Element? tagged = _hovered ?? _selected;
            if (tagged == null || !IsAvailable(tagged) || !TryGetElementScreenRect(tagged, cam, out UnityEngine.Rect rect))
            {
                _tagLabel.gameObject.SetActive(false);
                return;
            }

            _tagLabel.text = tagged.Name + tagged.AxisHint;
            _tagLabel.color = tagged == _selected ? SelectedColor : HoverColor;
            _tagLabel.gameObject.SetActive(true);
            PlaceAtScreenPoint(_tagLabel, cam, new Vector2(rect.center.x, rect.yMax + 14f));
        }

        private static UILabel CreateTagLabel(Transform parent)
        {
            GameObject go = new(TagLabelName);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;

            UILabel label = go.AddComponent<UILabel>();
            HudRenderer.ConfigureEditorOverlayLabel(label, 16);
            label.color = SelectedColor;
            label.depth = OverlayDepth;
            label.text = string.Empty;

            return label;
        }

        private static void DestroyTag()
        {
            if (_tagLabel != null)
                UnityEngine.Object.Destroy(_tagLabel.gameObject);

            _tagLabel = null;
        }

        // -------------------------------------------------------------------------
        //                         SCREEN / UI SPACE
        // -------------------------------------------------------------------------
        private static Transform? GetCameraReference()
        {
            if (_mode == EditorMode.Hud)
            {
                Transform? mainRootParent = HudRenderer.MainRootParent;
                if (mainRootParent != null)
                    return mainRootParent;

                return _hud != null ? _hud.transform : null;
            }

            if (_mode == EditorMode.FirstAid)
            {
                Transform? calories = FirstAidVitaminC.EditorCalories;
                if (calories != null)
                    return calories;

                return _firstAid != null ? _firstAid.transform : null;
            }

            return null;
        }

        private static UnityEngine.Camera? GetUiCamera()
        {
            if (_uiCamera != null && _uiCamera.isActiveAndEnabled)
                return _uiCamera;

            _uiCamera = null;

            Transform? reference = GetCameraReference();
            if (reference == null)
                return null;

            int mask = 1 << reference.gameObject.layer;
            UnityEngine.Camera? fallback = null;

            foreach (UnityEngine.Camera cam in UnityEngine.Camera.allCameras)
            {
                if (cam == null || (cam.cullingMask & mask) == 0)
                    continue;

                if (cam.orthographic)
                {
                    _uiCamera = cam;
                    return cam;
                }

                if (fallback == null)
                    fallback = cam;
            }

            _uiCamera = fallback;
            return fallback;
        }

        private static Vector2 MouseToLocal(UnityEngine.Camera cam, Transform parent, Vector3 mouse)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f));
            Vector3 local = parent.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        private static void PlaceAtScreenPoint(UILabel label, UnityEngine.Camera cam, Vector2 screenPoint)
        {
            Transform parent = label.transform.parent;
            if (parent == null)
                return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            Vector3 local = parent.InverseTransformPoint(world);
            local.z = 0f;
            label.transform.localPosition = local;
        }

        private static bool TryGetElementScreenRect(Element element, UnityEngine.Camera cam, out UnityEngine.Rect rect)
        {
            rect = default;
            bool found = false;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

            foreach (UIWidget? widget in element.Bounds())
            {
                if (widget == null || !widget.gameObject.activeInHierarchy)
                    continue;

                AccumulateWidgetScreenBounds(widget, cam, ref minX, ref minY, ref maxX, ref maxY);
                found = true;
            }

            if (!found || minX > maxX || minY > maxY)
                return false;

            rect = UnityEngine.Rect.MinMaxRect(minX - HitPaddingPixels, minY - HitPaddingPixels, maxX + HitPaddingPixels, maxY + HitPaddingPixels);
            return true;
        }

        private static void AccumulateWidgetScreenBounds(UIWidget widget, UnityEngine.Camera cam, ref float minX, ref float minY, ref float maxX, ref float maxY)
        {
            float width = Mathf.Max(widget.width, 8f);
            float height = Mathf.Max(widget.height, 8f);

            Vector2 pivotOffset = widget.pivotOffset;
            float x0 = -pivotOffset.x * width;
            float y0 = -pivotOffset.y * height;

            Transform transform = widget.transform;

            for (int i = 0; i < 4; i++)
            {
                float cx = (i & 1) == 0 ? x0 : x0 + width;
                float cy = (i & 2) == 0 ? y0 : y0 + height;

                Vector3 screen = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(cx, cy, 0f)));
                minX = Mathf.Min(minX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxX = Mathf.Max(maxX, screen.x);
                maxY = Mathf.Max(maxY, screen.y);
            }
        }

        // -------------------------------------------------------------------------
        //                           PLAYER CONTROLS
        // -------------------------------------------------------------------------
        private static void LockPlayer()
        {
            _playerLockedByEditor = false;

            PlayerManager? player = GameManager.GetPlayerManagerComponent();
            if (player == null || player.GetControlMode() != PlayerControlMode.Normal)
                return;

            player.SetControlMode(PlayerControlMode.Locked);
            _playerLockedByEditor = true;
        }

        private static void UnlockPlayer()
        {
            if (!_playerLockedByEditor)
                return;

            _playerLockedByEditor = false;

            PlayerManager? player = GameManager.GetPlayerManagerComponent();
            if (player != null)
                player.ReturnToNormalControlMode(PlayerControlMode.Locked);
        }
    }

    [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
    internal static class HudEditorPanelHudPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Panel_HUD __instance)
        {
            HudEditor.OnHudUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(InterfaceManager), nameof(InterfaceManager.ShouldEnableMousePointer))]
    internal static class HudEditorMousePointerPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (HudEditor.IsEditingHud)
                __result = true;
        }
    }
}