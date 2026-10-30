using HarmonyLib;
using UnityEngine;

namespace RestoryQOL
{
    /// <summary>
    /// Loader-agnostic mod core. All patches and per-frame logic live here and
    /// talk only to the game; the mod loader (MelonLoader or BepInEx) is wired
    /// in by the bootstrap types in Bootstrap.cs through ILog/IConfigStore.
    /// </summary>
    public static class Core
    {
        #region Config

        public static BoolEntry MenuEnabled;
        public static BoolEntry NoDeduction;
        public static BoolEntry InfiniteMoney;
        public static BoolEntry SkipLogos;
        public static BoolEntry FixSaveHang;
        public static BoolEntry AutoPartsPage;
        public static BoolEntry SortPartsByNotebook;
        public static BoolEntry SortBoxParts;
        public static BoolEntry HighlightMissingParts;
        public static BoolEntry AutoScrew;
        public static BoolEntry ResetTimerOnFail;
        public static BoolEntry InstantUltrasonic;
        public static BoolEntry AutoTool;
        public static BoolEntry AdBlock;
        public static BoolEntry SnapToSocket;
        public static BoolEntry QuickDispose;
        public static BoolEntry QuickDisposeManualCleaner;
        public static BoolEntry GatherParts;
        public static BoolEntry RefreshMarketplace;
        public static BoolEntry CountUltrasonicInNotebook;

        #endregion

        #region Logging

        public static ILog Log;

        #endregion

        #region UI State

        private static Rect _windowRect = new Rect(20f, 80f, 380f, 420f);
        private static bool _visible = true;
        private static int _windowId = 266622;

        // 中文字体（Unity 内置 GUI 字体不含汉字，必须换成系统雅黑之类）
        private static Font _cjkFont;
        private static GUIStyle _lbl;
        private static GUIStyle _lblBold;
        private static GUIStyle _lblCenter;
        private static GUIStyle _toggle;
        private static bool _stylesReady;

        #endregion

        public static void Initialize(ILog log, IConfigStore config)
        {
            Log = log;

            MenuEnabled          = config.CreateBool("MenuEnabled",          true,  "启动时打开菜单");
            NoDeduction          = config.CreateBool("InfinityMoney",        false, "购买时不扣钱包余额");
            InfiniteMoney        = config.CreateBool("FakeMoneyUI",          false, "界面假显示无限金钱（仅界面判定用）");
            SkipLogos            = config.CreateBool("SkipLogos",            true,  "跳过启动厂商 logo");
            FixSaveHang          = config.CreateBool("FixSaveHang",          true,  "修复存档卡死（跳过贴图转换等待）");
            AutoPartsPage        = config.CreateBool("AutoPartsPage",        true,  "放上设备后自动打开零件页");
            SortPartsByNotebook  = config.CreateBool("SortPartsByNotebook",  true,  "零件商店按笔记本里的零件顺序排序");
            SortBoxParts         = config.CreateBool("SortBoxParts",         true,  "零件箱先按设备、再按装配顺序排序");
            HighlightMissingParts = config.CreateBool("HighlightMissingParts", false, "高亮当前设备缺失的零件");
            AutoScrew            = config.CreateBool("AutoScrew",            true,  "按住 Z 全部拧紧 / X 全部拧松螺丝");
            ResetTimerOnFail     = config.CreateBool("ResetTimerOnFail",     false, "比赛失败时重置计时");
            InstantUltrasonic    = config.CreateBool("InstantUltrasonic",    false, "超声波清洗机立即完成");
            AutoTool             = config.CreateBool("AutoTool",             true,  "按零件状态自动选清洁工具或烙铁");
            AdBlock              = config.CreateBool("AdBlock",              true,  "屏蔽浏览器商店页的推广横幅");
            SnapToSocket         = config.CreateBool("SnapToSocket",         true,  "按住 ALT 把放下的零件吸附进插槽");
            QuickDispose         = config.CreateBool("QuickDispose",         true,  "按住 SHIFT 丢零件：坏→粉碎机，脏→清洗机，好→零件箱");
            QuickDisposeManualCleaner = config.CreateBool("QuickDisposeManualCleaner", false, "SHIFT 丢脏件去手动清洁（刷子/吹气）而不是超声波");
            GatherParts          = config.CreateBool("GatherParts",          true,  "按 G 把散落的零件聚到台面上");
            RefreshMarketplace   = config.CreateBool("RefreshMarketplace",   true,  "按 CTRL+R 刷新设备商店");
            CountUltrasonicInNotebook = config.CreateBool("CountUltrasonicInNotebook", true,  "超声波里的零件算作已在台面（笔记本里不再标缺失）");
            config.Save();

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.InfinityMoney));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.FakeMoneyUI));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.SkipLogos));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.SaveFixPatches));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.PartsPagePatches));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.PartsShopPatches));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.PartsBoxSortPatches));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.PartsBoxRemovePatch));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.ResetTimerOnFail));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.InstantUltrasonic));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.AutoTool));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.AdBlock));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.SnapToSocket));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.QuickDispose));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Mods.UltrasonicNotebookPatch));

            _visible = MenuEnabled.Value;
            Log.Msg("RestoryQOL 已加载（中文界面版），按 F8 开关菜单。");
        }

        // 拖动结束后记一行窗口位置：用户反馈「拖不动」时，日志能直接区分
        // 「没收到拖拽」和「拖了但位置没变」。
        private static Vector2 _lastWindowPos;
        private static float _windowPosSettleTime;

        public static void RunFrame()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                _visible = !_visible;
                Log.Msg("[菜单] F8 切换 -> 显示: " + _visible);
            }

            var pos = _windowRect.position;
            if (pos != _lastWindowPos)
            {
                _lastWindowPos = pos;
                _windowPosSettleTime = UnityEngine.Time.unscaledTime;
            }
            else if (_windowPosSettleTime > 0f && UnityEngine.Time.unscaledTime - _windowPosSettleTime > 0.3f)
            {
                _windowPosSettleTime = 0f;
                Log.Msg($"[窗口] 位置 -> ({pos.x:0}, {pos.y:0})");
            }
            Mods.AutoScrew.Run();
            Mods.RefreshMarketplace.Run();
            Mods.GatherParts.Run();
        }

        public static void DrawGUI()
        {
            // NOTE: F8 is NOT handled here on purpose. OnGUI fires multiple
            // times per frame and would toggle the menu several times for a
            // single keypress. It is handled exclusively in RunFrame(), where
            // Input.GetKeyDown fires exactly once per frame.
            if (!_visible) return;

            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "维修物语 QoL 设置");
        }

        private static void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            try
            {
                _cjkFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "SimSun", "宋体" }, 14);
            }
            catch { }

            if (_cjkFont != null)
            {
                GUI.skin.font = _cjkFont;
                GUI.skin.label.font = _cjkFont;
                GUI.skin.box.font = _cjkFont;
                GUI.skin.button.font = _cjkFont;
                GUI.skin.window.font = _cjkFont;
                GUI.skin.toggle.font = _cjkFont;
            }

            _lbl       = new GUIStyle(GUI.skin.label);
            _lblBold   = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _lblCenter = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            _toggle    = new GUIStyle(GUI.skin.toggle);

            if (_cjkFont != null)
            {
                _lbl.font = _cjkFont;
                _lblBold.font = _cjkFont;
                _lblCenter.font = _cjkFont;
                _toggle.font = _cjkFont;
            }
        }

        private static void DrawWindow(int id)
        {
            EnsureStyles();

            GUILayout.BeginVertical();

            GUILayout.Label("按 F8 关闭菜单 / 也可拖动标题栏", _lblCenter);

            GUILayout.Label("—— 作弊 ——", _lblBold);
            NoDeduction.Value = GUILayout.Toggle(NoDeduction.Value, " 购买不扣钱", _toggle);
            InfiniteMoney.Value   = GUILayout.Toggle(InfiniteMoney.Value,   " 无限金钱", _toggle);
            ResetTimerOnFail.Value = GUILayout.Toggle(ResetTimerOnFail.Value, " 比赛失败重置计时", _toggle);
            InstantUltrasonic.Value = GUILayout.Toggle(InstantUltrasonic.Value, " 超声波清洗瞬间完成", _toggle);

            GUILayout.Space(8f);
            GUILayout.Label("—— 便利优化 ——", _lblBold);
            SkipLogos.Value    = GUILayout.Toggle(SkipLogos.Value,    " 跳过启动 logo", _toggle);
            MenuEnabled.Value  = GUILayout.Toggle(MenuEnabled.Value,  " 启动时打开菜单", _toggle);
            HighlightMissingParts.Value = GUILayout.Toggle(HighlightMissingParts.Value, " 高亮缺失的零件", _toggle);
            AutoPartsPage.Value       = GUILayout.Toggle(AutoPartsPage.Value,       " 放上设备自动开零件页", _toggle);
            SortPartsByNotebook.Value = GUILayout.Toggle(SortPartsByNotebook.Value, " 零件商店按笔记本顺序排序", _toggle);
            SortBoxParts.Value         = GUILayout.Toggle(SortBoxParts.Value,        " 零件箱按设备/装配顺序排序", _toggle);
            CountUltrasonicInNotebook.Value = GUILayout.Toggle(CountUltrasonicInNotebook.Value, " 超声波内零件计入笔记本", _toggle);
            AutoTool.Value = GUILayout.Toggle(AutoTool.Value, " 自动选工具（脏件用上次清洁工具，烧焦用烙铁）", _toggle);
            AdBlock.Value = GUILayout.Toggle(AdBlock.Value, " 屏蔽商店页广告横幅", _toggle);

            GUILayout.Space(8f);
            GUILayout.Label("—— 快捷键 ——", _lblBold);
            SnapToSocket.Value = GUILayout.Toggle(SnapToSocket.Value, " 按住 ALT：零件自动吸附进插槽", _toggle);
            QuickDispose.Value = GUILayout.Toggle(QuickDispose.Value, " 按住 SHIFT 丢下：按状态自动归类", _toggle);
            QuickDisposeManualCleaner.Value = GUILayout.Toggle(QuickDisposeManualCleaner.Value, " SHIFT 丢脏件去手动清洁，不进超声波", _toggle);
            RefreshMarketplace.Value = GUILayout.Toggle(RefreshMarketplace.Value, " CTRL+R：刷新商店", _toggle);
            GatherParts.Value = GUILayout.Toggle(GatherParts.Value, " G：把散落零件聚到台面", _toggle);
            AutoScrew.Value = GUILayout.Toggle(AutoScrew.Value, " 按住 Z：全部拧紧 / X：全部拧松", _toggle);

            GUILayout.Space(8f);
            GUILayout.Label("—— Bug 修复 ——", _lblBold);
            FixSaveHang.Value  = GUILayout.Toggle(FixSaveHang.Value,  " 修复存档卡死", _toggle);

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                Rect last = GUILayoutUtility.GetLastRect();
                if (last.yMax > 0f)
                    _windowRect.height = last.yMax + 34f;
            }

            // 标题栏拖动。GUI.DragWindow 的参数是「窗口内坐标」，不是屏幕坐标：
            // 上游传的是 _windowRect（屏幕坐标 20,80,...），拖拽区被整体偏移到窗口中下部，
            // 标题栏（y 0..26）恰好在区外 —— 所以按标题栏拖不动，反而在正文里拖动会整窗乱跑。
            // 这里只认标题栏这一条 26px 高的区域，正文里的点击/开关不受影响。
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 26f));
        }
    }
}
