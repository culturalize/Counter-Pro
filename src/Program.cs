using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace CounterPro
{
    public class Theme
    {
        public string Name;
        public Color BgTop, BgBottom, Border, PanelBg, PanelBorder;
        public Color TextPrimary, TextSecondary, TextMuted;
        public Color Accent, AccentSoftBg, AccentSoftBorder;
        public Color RowActiveBg, RowActiveBorder, RowIdleBg, RowIdleBorder;
        public Color BtnBg, BtnBorder, BtnHoverBg;

        public Theme(string name, string bgTop, string bgBot, string border, string panelBg, string panelBorder,
                     string txtPri, string txtSec, string txtMut, string accent, string accBg, string accBorder,
                     string rowActBg, string rowActBorder, string rowIdleBg, string rowIdleBorder,
                     string btnBg, string btnBorder, string btnHover)
        {
            Name = name;
            BgTop = C(bgTop); BgBottom = C(bgBot); Border = C(border);
            PanelBg = C(panelBg); PanelBorder = C(panelBorder);
            TextPrimary = C(txtPri); TextSecondary = C(txtSec); TextMuted = C(txtMut);
            Accent = C(accent); AccentSoftBg = C(accBg); AccentSoftBorder = C(accBorder);
            RowActiveBg = C(rowActBg); RowActiveBorder = C(rowActBorder);
            RowIdleBg = C(rowIdleBg); RowIdleBorder = C(rowIdleBorder);
            BtnBg = C(btnBg); BtnBorder = C(btnBorder); BtnHoverBg = C(btnHover);
        }

        public static Theme FromCustomColors(Color bg, Color accent, Color text)
        {
            Color bgBot = Shift(bg, 10);
            Color panel = Shift(bg, -8);
            Color border = Blend(bg, accent, 0.32f);
            Color panelBorder = Blend(bg, accent, 0.20f);
            Color txtSec = Blend(text, bg, 0.35f);
            Color txtMut = Blend(text, bg, 0.55f);
            Color accSoftBg = Blend(bg, accent, 0.18f);
            Color accSoftBorder = Blend(bg, accent, 0.45f);
            Color rowActBg = Blend(bg, accent, 0.15f);
            Color rowActBorder = Blend(bg, accent, 0.40f);
            Color rowIdleBg = Shift(bg, 4);
            Color rowIdleBorder = Shift(bg, 14);
            Color btnBg = Shift(bg, 12);
            Color btnBorder = Shift(bg, 24);
            Color btnHover = Blend(btnBg, accent, 0.25f);

            return new Theme("Custom Theme",
                ToHex(bg), ToHex(bgBot), ToHex(border), ToHex(panel), ToHex(panelBorder),
                ToHex(text), ToHex(txtSec), ToHex(txtMut), ToHex(accent), ToHex(accSoftBg), ToHex(accSoftBorder),
                ToHex(rowActBg), ToHex(rowActBorder), ToHex(rowIdleBg), ToHex(rowIdleBorder),
                ToHex(btnBg), ToHex(btnBorder), ToHex(btnHover));
        }

        static Color Shift(Color c, int delta)
        {
            return Color.FromArgb(255, Clamp(c.R + delta), Clamp(c.G + delta), Clamp(c.B + delta));
        }

        public static Color Blend(Color a, Color b, float t)
        {
            int r = Clamp((int)(a.R + (b.R - a.R) * t));
            int g = Clamp((int)(a.G + (b.G - a.G) * t));
            int bl = Clamp((int)(a.B + (b.B - a.B) * t));
            return Color.FromArgb(255, r, g, bl);
        }

        static int Clamp(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }
        public static string ToHex(Color c) { return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B); }
        public static Color C(string hex) { try { return ColorTranslator.FromHtml(hex); } catch { return Color.White; } }
    }

    public class OverlaySticker
    {
        // Either a file path OR a built-in vector icon ("builtin:chest", "builtin:key", "builtin:crown", "builtin:gem", "builtin:skull", "builtin:flame", "builtin:star", "builtin:trophy")
        public string Source = "";
        // Offset relative to the center of the Hero Number area
        public int OffsetX = -110;
        public int OffsetY = 0;
        public int SizePx = 56;
        public Bitmap CachedBmp = null;
        public int CachedSize = 0;
        public Rectangle LastDrawnBounds = Rectangle.Empty;

        public void EnsureCache()
        {
            if (string.IsNullOrEmpty(Source) || Source.StartsWith("builtin:")) return;
            if (CachedBmp != null && CachedSize == SizePx) return;
            if (CachedBmp != null) { try { CachedBmp.Dispose(); } catch { } CachedBmp = null; }
            if (!File.Exists(Source)) return;
            try
            {
                using (Image raw = Image.FromFile(Source))
                {
                    int sz = Math.Max(16, Math.Min(320, SizePx));
                    float aspect = (float)raw.Width / Math.Max(1, raw.Height);
                    int w = aspect >= 1f ? sz : (int)(sz * aspect);
                    int h = aspect >= 1f ? (int)(sz / aspect) : sz;
                    var bmp = new Bitmap(Math.Max(1, w), Math.Max(1, h));
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawImage(raw, 0, 0, bmp.Width, bmp.Height);
                    }
                    CachedBmp = bmp;
                    CachedSize = SizePx;
                }
            }
            catch { }
        }

        public void DisposeCache()
        {
            if (CachedBmp != null) { try { CachedBmp.Dispose(); } catch { } CachedBmp = null; }
        }
    }

    public class TriggerBind
    {
        public string Kind = "None";
        public int Code = 0;

        public TriggerBind() { }
        public TriggerBind(string kind, int code) { Kind = kind; Code = code; }

        public bool MatchesMouse(int m) { return Code == m && string.Equals(Kind, "Mouse", StringComparison.OrdinalIgnoreCase); }
        public bool MatchesKey(int vk) { return Code == vk && string.Equals(Kind, "Key", StringComparison.OrdinalIgnoreCase); }

        public string DisplayName()
        {
            if (string.Equals(Kind, "Mouse", StringComparison.OrdinalIgnoreCase))
            {
                if (Code == 1) return "Mouse 4 (Side Back)";
                if (Code == 2) return "Mouse 5 (Side Front)";
                if (Code == 3) return "Middle Mouse (Wheel)";
                return "Mouse " + Code;
            }
            if (string.Equals(Kind, "Key", StringComparison.OrdinalIgnoreCase) && Code > 0)
                return FormatVk(Code);
            return "Unbound";
        }

        public string ShortName()
        {
            if (string.Equals(Kind, "Mouse", StringComparison.OrdinalIgnoreCase))
            {
                if (Code == 1) return "MOUSE 4";
                if (Code == 2) return "MOUSE 5";
                if (Code == 3) return "MMB";
                return "M" + Code;
            }
            if (string.Equals(Kind, "Key", StringComparison.OrdinalIgnoreCase) && Code > 0)
                return FormatVk(Code).ToUpperInvariant();
            return "NONE";
        }

        public static string FormatVk(int vk)
        {
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x60 && vk <= 0x69) return "Num " + (vk - 0x60);
            if (vk >= 0x70 && vk <= 0x87) return "F" + (vk - 0x6F);
            switch (vk)
            {
                case 0x6B: return "Num +";
                case 0x6D: return "Num -";
                case 0x6A: return "Num *";
                case 0x6F: return "Num /";
                case 0x6E: return "Num .";
                case 0xBB: return "=";
                case 0xBD: return "-";
                case 0x20: return "Space";
                case 0x0D: return "Enter";
                case 0x09: return "Tab";
                case 0x14: return "Caps";
                case 0xA0: case 0xA1: case 0x10: return "Shift";
                case 0xA2: case 0xA3: case 0x11: return "Ctrl";
                case 0xA4: case 0xA5: case 0x12: return "Alt";
                case 0x2D: return "Ins";
                case 0x2E: return "Del";
                case 0x24: return "Home";
                case 0x23: return "End";
                case 0x21: return "PgUp";
                case 0x22: return "PgDn";
                case 0x26: return "Up";
                case 0x28: return "Down";
                case 0x25: return "Left";
                case 0x27: return "Right";
                case 0xC0: return "`";
                case 0xDB: return "[";
                case 0xDD: return "]";
                case 0xDC: return "\\";
                case 0xBA: return ";";
                case 0xDE: return "'";
                case 0xBC: return ",";
                case 0xBE: return ".";
                case 0xBF: return "/";
                default: return ((Keys)vk).ToString();
            }
        }
    }

    public class UiButton
    {
        public string Id, Label, Tooltip;
        public Rectangle Bounds;
        public bool IsClose, IsAccent, IsActive;
        public Action OnClick;
    }

    public class VisualKeyNode
    {
        public string Label, Kind;
        public int Code;
        public Rectangle Bounds;
    }

    public class CounterForm : Form
    {
        const int WH_KEYBOARD_LL = 13;
        const int WH_MOUSE_LL = 14;
        const uint WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105;
        const uint WM_MBUTTONDOWN = 0x0207;
        const uint WM_XBUTTONDOWN = 0x020B;

        public const int WM_APP_MOUSE = 0x8010;
        public const int WM_APP_KEY = 0x8011;
        public const int WM_APP_SHOW_EXISTING = 0x8012;

        const int WS_EX_LAYERED = 0x00080000;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_TOPMOST = 0x00000008;
        const int WS_EX_TOOLWINDOW = 0x00000080;
        const int GWL_EXSTYLE = -20;
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 3;
        const uint GW_HWNDPREV = 3;
        const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010, SWP_SHOWWINDOW = 0x0040;

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] static extern int GetKeyboardType(int nTypeFlag);
        [DllImport("user32.dll")] static extern short GetKeyState(int nVirtKey);
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("psapi.dll")] static extern bool EmptyWorkingSet(IntPtr hProcess);
        [DllImport("kernel32.dll")] static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr minSize, IntPtr maxSize);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] static extern int ChangeDisplaySettings(ref DEVMODE lpDevMode, int dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        static HookProc mouseProc;
        static HookProc keyProc;
        static WinEventDelegate winEventProc;
        static IntPtr mouseHook;
        static IntPtr keyHook;
        static IntPtr fgEventHook;
        static IntPtr minEventHook;
        System.Windows.Forms.Timer topmostTimer;
        public static IntPtr SharedHwnd;
        public static CounterForm ProgramFormInstance;

        static bool[] keysDown = new bool[256];

        // Dedicated high-priority input hook thread (UI painting / GC can never delay your mouse or keyboard)
        [StructLayout(LayoutKind.Sequential)]
        struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam; public uint time; public int ptX; public int ptY; }
        [DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")] static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
        [DllImport("user32.dll")] static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        static Thread hookThread;
        static volatile uint hookThreadId;

        // Fortnite automation
        bool autoLaunchedWithGame = false;
        bool autoStretchRes = false;
        bool autoSessionSection = false;
        bool autoLaunchRegistered = false;
        int gameMissCount = 0;
        System.Windows.Forms.Timer gameExitTimer;
        bool overlayInfraStarted = false;
        string backupDir;
        string filePath;
        string settingsPath;
        FileSystemWatcher watcher;
        volatile bool ignoreNextWatchEvent;
        readonly object fileLock = new object();

        List<long> sections = new List<long>();
        List<long> sectionStarts = new List<long>();
        long sessionStartValue = 0;
        bool sessionInitialized = false;
        List<DateTime> recentClicks = new List<DateTime>();

        // 6 Configurable Binds
        TriggerBind bindIncrement = new TriggerBind("Mouse", 2);
        TriggerBind bindSection = new TriggerBind("Key", 0x78);
        TriggerBind bindDecrement = new TriggerBind("None", 0);
        TriggerBind bindUndo = new TriggerBind("None", 0);
        TriggerBind bindGhost = new TriggerBind("Key", 0x77);
        TriggerBind bindMode = new TriggerBind("Key", 0x76);

        int selectedBindSlot = 1;
        bool isListeningForKey = false;
        int lastPhysicallyPressedVk = 0;
        int lastPhysicallyPressedMouse = 0;

        int kbSizeMode = -1;
        string detectedKbSummary = "100% Full";

        List<Theme> themes = BuildThemes();
        int themeIdx = 0;
        Color customBg = ColorTranslator.FromHtml("#02072D");
        Color customAccent = ColorTranslator.FromHtml("#F40000");
        Color customText = ColorTranslator.FromHtml("#FFFFFF");

        // View Modes: 0 = Full, 1 = Mini Card, 2 = Slim HUD Pill, 3 = Just Number (Transparent Floating)
        int viewMode = 0;
        bool isSettingsOpen = false;
        // 5 Spacious Settings Tabs: 0 = Visual Keys & Mouse, 1 = Stickers & Overlay Studio, 2 = Themes (19), 3 = Fonts & Effects, 4 = Game & Goals
        int settingsTab = 0;

        bool isGhostMode = false;
        bool gameOverlayNoActivate = true;
        bool soundEnabled = false;
        double userOpacity = 0.96;
        int stepAmount = 1;
        long targetGoal = 2500;
        bool showGoalBar = true;
        bool showBadges = true;
        bool showSparkline = true;

        // Free-Draggable Overlay Stickers / Images
        List<OverlaySticker> stickers = new List<OverlaySticker>();
        int draggingStickerIndex = -1;
        int hoveredStickerIndex = -1;
        Point stickerDragStartMouse;
        Point stickerDragStartOffset;

        int vectorEmblem = 0;
        // Number Text Effect: 0 = Outline + Glow, 1 = Crisp Outline, 2 = 3D Shadow, 3 = Custom Gradient, 4 = Clean Flat
        int numberEffectStyle = 3;
        int outlineThickness = 3;
        Color gradColor1 = ColorTranslator.FromHtml("#00F5D4");
        Color gradColor2 = ColorTranslator.FromHtml("#7B2CBF");
        // 0 = Vertical, 1 = Horizontal, 2 = Diagonal Down (45 deg), 3 = Diagonal Up (135 deg)
        int gradDirection = 0;
        bool useCustomGradColors = false;
        string numberPrefix = "";
        string numberSuffix = "";

        string[] fontFamilies = new string[] { "Segoe UI", "Bahnschrift", "Consolas", "Trebuchet MS", "Verdana", "Impact", "Georgia", "Arial Black" };
        int fontFamilyIdx = 0;
        int heroFontSizePt = 48;

        // Custom user-resized dimensions per mode
        Size customFullSize = new Size(460, 500);
        Size customFloatSize = new Size(420, 140);

        int scrollOffset = 0;
        int maxScroll = 0;

        // Window Drag & Corner Resize States
        bool isMouseDownOnWindow = false;
        bool isDraggingWindow = false;
        bool isResizingWindow = false;
        int resizeCornerDir = 0; // 1=BR, 2=BL, 3=TR, 4=TL
        Point dragCursorStart;
        Rectangle dragWindowStartBounds;
        bool isMouseInsideWindow = false;

        List<UiButton> buttons = new List<UiButton>();
        List<Rectangle> swatchRects = new List<Rectangle>();
        List<VisualKeyNode> visualKeys = new List<VisualKeyNode>();
        int hoveredBtnIndex = -1;
        int hoveredVisualKeyIndex = -1;

        ToolTip toolTip = new ToolTip();
        NotifyIcon trayIcon;
        ContextMenuStrip ctxMenu;
        SoundPlayer tickPlayer;
        bool trimmedAfterFirstPaint = false;

        Font fontHeader, fontBtn, fontBadge, fontHeroFull, fontHeroMini, fontHeroHud, fontHeroNumOnly;
        Font fontSecDivider, fontRowActive, fontRowIdle, fontRowTag, fontSetTitle, fontSetLabel, fontKeyCap;

        readonly Color magicTransKey = Color.FromArgb(255, 1, 2, 3);

        public CounterForm(string counterFile, string modeOverride) : this(counterFile, modeOverride, false) { }

        public CounterForm(string counterFile, string modeOverride, bool launchedWithGame)
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
            autoLaunchedWithGame = launchedWithGame;

            filePath = Path.GetFullPath(counterFile);
            string dir = Path.GetDirectoryName(filePath);
            settingsPath = Path.Combine(dir, "settings.json");
            backupDir = Path.Combine(dir, "backups");

            if (!File.Exists(filePath)) File.WriteAllText(filePath, "0" + Environment.NewLine);
            MakeDailyBackup();

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            Text = "Split Counter Pro";
            Size = customFullSize;
            MinimumSize = new Size(160, 52);

            try
            {
                Icon appIco = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIco != null) Icon = appIco;
            }
            catch { }

            AutoDetectKeyboardSize();
            InitSoftTickSound();
            LoadSettings();
            autoLaunchRegistered = AutoStart.IsRegistered();

            if (!string.IsNullOrEmpty(modeOverride))
            {
                if (string.Equals(modeOverride, "Mouse4", StringComparison.OrdinalIgnoreCase))
                    bindIncrement = new TriggerBind("Mouse", 1);
                else if (string.Equals(modeOverride, "Mouse5", StringComparison.OrdinalIgnoreCase))
                    bindIncrement = new TriggerBind("Mouse", 2);
                else if (string.Equals(modeOverride, "F9", StringComparison.OrdinalIgnoreCase))
                    bindIncrement = new TriggerBind("Key", 0x78);
            }

            RebuildFonts();
            RefreshAllStickerCaches();
            LoadFromFileSync();

            // New section per Fortnite session (only if the current section actually has gains, so no empty sections)
            if (autoLaunchedWithGame && autoSessionSection)
            {
                lock (fileLock)
                {
                    int n = sections.Count;
                    long cur = sections[n - 1];
                    long start = (n >= 2) ? sections[n - 2] : (sectionStarts.Count > 0 ? sectionStarts[0] : cur);
                    if (n == 1 || cur != start)
                    {
                        sections.Add(cur);
                        sectionStarts.Add(cur);
                    }
                }
                SaveToFileSync();
            }

            BuildContextMenuAndTray();
            UpdateTrayText();
            ApplyViewModeGeometry();
            EnsureTextFitsInWindow();
            ClampWindowToActiveScreen();
            SetupFileWatcher();

            // Auto-close when Fortnite exits (only when the watcher launched us with the game)
            if (autoLaunchedWithGame)
            {
                gameExitTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                gameExitTimer.Tick += (s, e) =>
                {
                    if (GameWatch.IsFortniteRunning()) { gameMissCount = 0; return; }
                    if (++gameMissCount >= 2) { gameExitTimer.Stop(); Close(); }
                };
                gameExitTimer.Start();
            }
        }

        // The overlay must NEVER steal focus from the game when it appears (would minimize exclusive-fullscreen Fortnite)
        protected override bool ShowWithoutActivation { get { return true; } }

        void MakeDailyBackup()
        {
            try
            {
                Directory.CreateDirectory(backupDir);
                string today = Path.Combine(backupDir, "counter_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                if (!File.Exists(today)) File.Copy(filePath, today, false);
                string[] all = Directory.GetFiles(backupDir, "counter_*.txt");
                if (all.Length > 30)
                {
                    Array.Sort(all, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < all.Length - 30; i++) { try { File.Delete(all[i]); } catch { } }
                }
            }
            catch { }
        }

        void UpdateTrayText()
        {
            if (trayIcon == null) return;
            try
            {
                long cur, sess;
                lock (fileLock) { cur = sections[sections.Count - 1]; }
                sess = cur - sessionStartValue;
                string txt = "Split Counter • " + numberPrefix.Trim() + " " + cur.ToString("N0") + " • " + (sess >= 0 ? "+" : "") + sess.ToString("N0") + " session";
                if (txt.Length > 63) txt = txt.Substring(0, 63);
                trayIcon.Text = txt;
            }
            catch { }
        }

        void AutoDetectKeyboardSize()
        {
            try
            {
                int fnKeys = GetKeyboardType(2);
                bool numLockOn = (GetKeyState(0x90) & 1) != 0;
                if (fnKeys < 10) { kbSizeMode = 3; detectedKbSummary = "65% Compact"; }
                else if (numLockOn) { kbSizeMode = 0; detectedKbSummary = "100% Full"; }
                else { kbSizeMode = 1; detectedKbSummary = "80% TKL"; }
            }
            catch { kbSizeMode = 0; detectedKbSummary = "100% Full"; }
        }

        void RefreshAllStickerCaches()
        {
            for (int i = 0; i < stickers.Count; i++)
                stickers[i].EnsureCache();
        }

        void InitSoftTickSound()
        {
            try
            {
                int sampleRate = 22050;
                int samples = 80;
                var ms = new MemoryStream();
                var bw = new BinaryWriter(ms);
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + samples * 2);
                bw.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    double env = Math.Exp(-i / 16.0);
                    short val = (short)(Math.Sin(i * 0.45) * 2800 * env);
                    bw.Write(val);
                }
                ms.Position = 0;
                tickPlayer = new SoundPlayer(ms);
            }
            catch { }
        }

        void RebuildFonts()
        {
            string fam = fontFamilies[fontFamilyIdx % fontFamilies.Length];
            int sz = Math.Max(16, Math.Min(130, heroFontSizePt));
            DisposeFont(ref fontHeader); fontHeader = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            DisposeFont(ref fontBtn); fontBtn = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            DisposeFont(ref fontBadge); fontBadge = new Font("Segoe UI", 8.2f, FontStyle.Bold);
            DisposeFont(ref fontHeroFull); fontHeroFull = new Font(fam, sz, FontStyle.Bold);
            DisposeFont(ref fontHeroMini); fontHeroMini = new Font(fam, Math.Max(22, sz - 8), FontStyle.Bold);
            DisposeFont(ref fontHeroHud); fontHeroHud = new Font(fam, 22f, FontStyle.Bold);
            DisposeFont(ref fontHeroNumOnly); fontHeroNumOnly = new Font(fam, sz, FontStyle.Bold);
            DisposeFont(ref fontSecDivider); fontSecDivider = new Font("Segoe UI", 7.8f, FontStyle.Bold);
            DisposeFont(ref fontRowActive); fontRowActive = new Font(fam, 12f, FontStyle.Bold);
            DisposeFont(ref fontRowIdle); fontRowIdle = new Font(fam, 10.5f, FontStyle.Bold);
            DisposeFont(ref fontRowTag); fontRowTag = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            DisposeFont(ref fontSetTitle); fontSetTitle = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            DisposeFont(ref fontSetLabel); fontSetLabel = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            DisposeFont(ref fontKeyCap); fontKeyCap = new Font("Segoe UI", 7.2f, FontStyle.Bold);
        }

        static void DisposeFont(ref Font f)
        {
            if (f != null) { try { f.Dispose(); } catch { } f = null; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
                if (gameOverlayNoActivate)
                    cp.ExStyle |= WS_EX_NOACTIVATE;
                if (isGhostMode && !isSettingsOpen)
                    cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SharedHwnd = this.Handle;

            UpdateDwmAndRegion();

            if (!overlayInfraStarted)
            {
                overlayInfraStarted = true;
                StartHookThread();

                // Event-driven Foreground & Un-Minimize TopMost enforcement (zero game process contact!)
                winEventProc = OnWinForegroundEvent;
                fgEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
                minEventHook = SetWinEventHook(EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

                // Ultra-cheap Z-order keeper: only calls SetWindowPos when the foreground window has actually climbed ABOVE us
                topmostTimer = new System.Windows.Forms.Timer { Interval = 750 };
                topmostTimer.Tick += (s, ev) =>
                {
                    if (!TopMost || !IsHandleCreated || !Visible || WindowState == FormWindowState.Minimized) return;
                    IntPtr fg = GetForegroundWindow();
                    if (fg == IntPtr.Zero || fg == this.Handle) return;
                    IntPtr h = GetWindow(this.Handle, GW_HWNDPREV);
                    int guard = 0;
                    while (h != IntPtr.Zero && guard++ < 64)
                    {
                        if (h == fg)
                        {
                            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                            break;
                        }
                        h = GetWindow(h, GW_HWNDPREV);
                    }
                };
                topmostTimer.Start();

                try
                {
                    Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                }
                catch { }
            }

            EnforceGameTopmost();
        }

        static void StartHookThread()
        {
            if (hookThread != null) return;
            mouseProc = MouseCallback;
            keyProc = KeyCallback;
            // Pre-JIT the hook callbacks so the very first click has zero JIT latency
            try
            {
                RuntimeHelpers.PrepareMethod(typeof(CounterForm).GetMethod("MouseCallback", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle);
                RuntimeHelpers.PrepareMethod(typeof(CounterForm).GetMethod("KeyCallback", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle);
            }
            catch { }

            var ready = new ManualResetEvent(false);
            hookThread = new Thread(() =>
            {
                MSG msg;
                PeekMessage(out msg, IntPtr.Zero, 0, 0, 0); // force message queue creation
                hookThreadId = GetCurrentThreadId();
                IntPtr hMod = GetModuleHandle(null);
                mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseProc, hMod, 0);
                keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyProc, hMod, 0);
                ready.Set();
                while (GetMessage(out msg, IntPtr.Zero, 0, 0) > 0) { }
                if (mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
                if (keyHook != IntPtr.Zero) { UnhookWindowsHookEx(keyHook); keyHook = IntPtr.Zero; }
            });
            hookThread.IsBackground = true;
            hookThread.Name = "SplitCounterInputHook";
            hookThread.Priority = ThreadPriority.Highest;
            hookThread.Start();
            ready.WaitOne(2000);
            ready.Close();
        }

        static void StopHookThread()
        {
            if (hookThread == null) return;
            if (hookThreadId != 0) PostThreadMessage(hookThreadId, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
            try { hookThread.Join(1000); } catch { }
            hookThread = null;
            hookThreadId = 0;
            Array.Clear(keysDown, 0, keysDown.Length);
        }

        void OnWinForegroundEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (IsHandleCreated && TopMost && hwnd != this.Handle)
            {
                EnforceGameTopmost();
            }
        }

        void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    ClampWindowToActiveScreen();
                    UpdateDwmAndRegion();
                    EnforceGameTopmost();
                    Invalidate();
                }));
            }
        }

        public void ClampWindowToActiveScreen()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                int nx = Left;
                int ny = Top;
                if (nx + Width > bounds.Right) nx = Math.Max(bounds.Left + 8, bounds.Right - Width - 12);
                if (nx < bounds.Left) nx = bounds.Left + 8;
                if (ny + Height > bounds.Bottom) ny = Math.Max(bounds.Top + 8, bounds.Bottom - Height - 12);
                if (ny < bounds.Top) ny = bounds.Top + 8;
                if (nx != Left || ny != Top)
                    Location = new Point(nx, ny);
            }
            catch { }
        }

        public void ToggleStretchedDesktopResolution()
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                int cw, ch, chz;
                if (DisplayRes.GetCurrent(out cw, out ch, out chz))
                {
                    int fw, fh;
                    if (!DisplayRes.TryReadFortniteStretch(out fw, out fh)) { fw = 1720; fh = 1080; }
                    if (cw == fw && ch == fh) DisplayRes.Restore(dir);
                    else DisplayRes.Apply(fw, fh, dir);
                    ClampWindowToActiveScreen();
                    EnforceGameTopmost();
                    RebuildButtons();
                    Invalidate();
                }
            }
            catch { }
        }

        public void ApplyGhostExStyle()
        {
            if (!IsHandleCreated) return;
            int ex = GetWindowLong(this.Handle, GWL_EXSTYLE);
            int desired = ex | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
            if (gameOverlayNoActivate) desired |= WS_EX_NOACTIVATE;
            else desired &= ~WS_EX_NOACTIVATE;

            if (isGhostMode && !isSettingsOpen)
                desired |= WS_EX_TRANSPARENT;
            else
                desired &= ~WS_EX_TRANSPARENT;

            if (ex != desired)
                SetWindowLong(this.Handle, GWL_EXSTYLE, desired);
        }

        public void EnforceGameTopmost()
        {
            if (TopMost && IsHandleCreated)
            {
                ApplyGhostExStyle();
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        int lastMeasureSig = 0;

        // Ensures the floating window is always wide/tall enough so text + stickers are NEVER cut off!
        void EnsureTextFitsInWindow()
        {
            if (isSettingsOpen) return;
            try
            {
                long current = sections.Count > 0 ? sections[sections.Count - 1] : 0;
                string displayNum = numberPrefix + current.ToString("N0") + numberSuffix;
                int sig = (displayNum.Length << 20) ^ (fontFamilyIdx << 14) ^ (heroFontSizePt << 4) ^ (viewMode & 0xF) ^ (customFloatSize.Width << 7);
                if (sig == lastMeasureSig) return;
                lastMeasureSig = sig;
                Font f = (viewMode == 3) ? fontHeroNumOnly : (viewMode == 1 ? fontHeroMini : fontHeroFull);
                using (Graphics g = CreateGraphics())
                {
                    SizeF sz = g.MeasureString(displayNum, f);
                    int neededW = (int)Math.Ceiling(sz.Width) + 56;
                    int neededH = (int)Math.Ceiling(sz.Height) + (viewMode == 3 ? 36 : 80);

                    if (viewMode == 3)
                    {
                        int newW = Math.Max(customFloatSize.Width, neededW);
                        int newH = Math.Max(customFloatSize.Height, neededH);
                        if (Width != newW || Height != newH)
                            Size = new Size(newW, newH);
                    }
                    else if (viewMode == 0 && Width < neededW)
                    {
                        Width = neededW;
                    }
                }
            }
            catch { }
        }

        void UpdateDwmAndRegion()
        {
            if (!IsHandleCreated) return;
            if (viewMode == 3 && !isSettingsOpen) // Just Number Mode: transparent background
            {
                try
                {
                    int pref = 1; // DWMWCP_DONOTROUND
                    DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
                }
                catch { }
                Region old = this.Region;
                this.Region = null;
                if (old != null) old.Dispose();
                BackColor = magicTransKey;
                TransparencyKey = magicTransKey;
                Opacity = 1.0;
            }
            else
            {
                TransparencyKey = Color.Empty;
                Opacity = isGhostMode ? Math.Min(userOpacity, 0.78) : userOpacity;
                try
                {
                    int pref = 2; // DWMWCP_ROUND
                    DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
                }
                catch { }
                try
                {
                    using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width, Height), 14))
                    {
                        Region old = this.Region;
                        this.Region = new Region(path);
                        if (old != null) old.Dispose();
                    }
                }
                catch { }
            }
            EnforceGameTopmost();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateDwmAndRegion();
            RebuildButtons();
            Invalidate();
        }

        public static void TrimMemory()
        {
            try
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                IntPtr h = Process.GetCurrentProcess().Handle;
                SetProcessWorkingSetSize(h, (IntPtr)(-1), (IntPtr)(-1));
                EmptyWorkingSet(h);
            }
            catch { }
        }

        void SetupFileWatcher()
        {
            try
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.Changed += (s, e) =>
                {
                    if (ignoreNextWatchEvent) { ignoreNextWatchEvent = false; return; }
                    if (IsHandleCreated)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            LoadFromFileSync();
                            EnsureTextFitsInWindow();
                            Invalidate();
                        }));
                    }
                };
                watcher.EnableRaisingEvents = true;
            }
            catch { }
        }

        void BuildContextMenuAndTray()
        {
            ctxMenu = new ContextMenuStrip();

            var modeMenu = new ToolStripMenuItem("🗔 View Mode");
            var m0 = new ToolStripMenuItem("1. Full Mode (Spacious Card + Sections)"); m0.Click += (s, e) => SetViewMode(0);
            var m1 = new ToolStripMenuItem("2. Mini Card Mode"); m1.Click += (s, e) => SetViewMode(1);
            var m2 = new ToolStripMenuItem("3. Slim Gaming HUD Pill"); m2.Click += (s, e) => SetViewMode(2);
            var m3 = new ToolStripMenuItem("4. Just Number Mode (Transparent Floating Number)"); m3.Click += (s, e) => SetViewMode(3);
            modeMenu.DropDownItems.AddRange(new ToolStripItem[] { m0, m1, m2, m3 });

            var studioItem = new ToolStripMenuItem("🖼 Open Stickers & Overlay Studio...");
            studioItem.Click += (s, e) => { settingsTab = 1; OpenSettingsDrawer(); };

            var keysItem = new ToolStripMenuItem("⌨️ Open Visual Keyboard & Mouse Hotkeys...");
            keysItem.Click += (s, e) => { settingsTab = 0; OpenSettingsDrawer(); };

            var themeMenu = new ToolStripMenuItem("🎨 Colour Theme (19)");
            for (int i = 0; i < themes.Count; i++)
            {
                int idx = i;
                var item = new ToolStripMenuItem(themes[i].Name);
                item.Click += (s, e) => { themeIdx = idx; SaveSettings(); Invalidate(); };
                themeMenu.DropDownItems.Add(item);
            }

            var soundItem = new ToolStripMenuItem("🔇 Mute / Unmute Click Sound");
            soundItem.Click += (s, e) => { soundEnabled = !soundEnabled; SaveSettings(); RebuildButtons(); Invalidate(); };

            var ghostItem = new ToolStripMenuItem("👻 Toggle Click-Through Ghost Mode (" + bindGhost.ShortName() + ")");
            ghostItem.Click += (s, e) => ToggleGhostMode();

            var stretchedItem = new ToolStripMenuItem("🎮 Toggle 1720×1080 Stretched Desktop Res (Fortnite Overlay Fix)");
            stretchedItem.Click += (s, e) => ToggleStretchedDesktopResolution();

            var autoFnItem = new ToolStripMenuItem("🚀 Auto Open/Close with Fortnite");
            autoFnItem.Click += (s, e) =>
            {
                autoLaunchRegistered = !autoLaunchRegistered;
                if (autoLaunchRegistered) AutoStart.Enable(); else AutoStart.Disable();
                RebuildButtons();
                Invalidate();
            };
            ctxMenu.Opening += (s, e) => { autoFnItem.Checked = autoLaunchRegistered; };

            var undoItem = new ToolStripMenuItem("↶ Undo Last Section Split");
            undoItem.Click += (s, e) => DoUndoSection();

            var setValItem = new ToolStripMenuItem("✏️ Set Exact Counter Number...");
            setValItem.Click += (s, e) => PromptSetExactNumber();

            var closeItem = new ToolStripMenuItem("✕ Close Counter");
            closeItem.Font = new Font(ctxMenu.Font, FontStyle.Bold);
            closeItem.Click += (s, e) => Close();

            ctxMenu.Items.AddRange(new ToolStripItem[] {
                modeMenu, studioItem, keysItem, themeMenu, soundItem, ghostItem, stretchedItem, autoFnItem, undoItem, setValItem,
                new ToolStripSeparator(), closeItem
            });

            this.ContextMenuStrip = ctxMenu;

            trayIcon = new NotifyIcon
            {
                Icon = this.Icon ?? SystemIcons.Application,
                Text = "Split Counter Pro (Click to Show / Unlock Ghost Mode)",
                Visible = true,
                ContextMenuStrip = ctxMenu
            };
            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (isGhostMode) ToggleGhostMode();
                    if (!Visible) Show();
                    if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
                    EnforceGameTopmost();
                }
            };
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && !isListeningForKey)
            {
                if (isSettingsOpen) { ToggleSettings(); return true; }
                Close();
                return true;
            }
            return true;
        }

        protected override void WndProc(ref Message m)
        {
            // When Mouse Pass-Through (Ghost Mode) is ON, make the entire window 100% transparent to mouse hover & clicks!
            if (m.Msg == 0x0084 && isGhostMode && !isSettingsOpen) // WM_NCHITTEST
            {
                m.Result = new IntPtr(-1); // HTTRANSPARENT
                return;
            }
            if (m.Msg == WM_MOUSEACTIVATE && gameOverlayNoActivate)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            if (m.Msg == WM_APP_MOUSE)
            {
                HandleMouseTrigger(m.WParam.ToInt32());
                return;
            }
            if (m.Msg == WM_APP_KEY)
            {
                HandleKeyTrigger(m.WParam.ToInt32());
                return;
            }
            if (m.Msg == WM_APP_SHOW_EXISTING)
            {
                if (isGhostMode) ToggleGhostMode();
                if (!Visible) Show();
                if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
                EnforceGameTopmost();
                return;
            }
            base.WndProc(ref m);
        }

        void HandleMouseTrigger(int mouseCode)
        {
            lastPhysicallyPressedMouse = mouseCode;
            lastPhysicallyPressedVk = 0;

            if (isListeningForKey && selectedBindSlot >= 1 && selectedBindSlot <= 6)
            {
                AssignBindToSlot(selectedBindSlot, new TriggerBind("Mouse", mouseCode));
                isListeningForKey = false;
                RebuildButtons();
                Invalidate();
                return;
            }

            if (bindIncrement.MatchesMouse(mouseCode)) DoIncrement();
            else if (bindSection.MatchesMouse(mouseCode)) DoNewSection();
            else if (bindDecrement.MatchesMouse(mouseCode)) DoDecrement();
            else if (bindUndo.MatchesMouse(mouseCode)) DoUndoSection();
            else if (bindGhost.MatchesMouse(mouseCode)) ToggleGhostMode();
            else if (bindMode.MatchesMouse(mouseCode)) SetViewMode((viewMode + 1) % 4);
            else if (isSettingsOpen && settingsTab == 0) Invalidate();
        }

        void HandleKeyTrigger(int vk)
        {
            lastPhysicallyPressedVk = vk;
            lastPhysicallyPressedMouse = 0;

            if (vk >= 0x60 && vk <= 0x6F && kbSizeMode != 0)
            {
                kbSizeMode = 0;
                detectedKbSummary = "100% Full";
                if (isSettingsOpen && settingsTab == 0) RebuildButtons();
            }

            if (isListeningForKey && selectedBindSlot >= 1 && selectedBindSlot <= 6)
            {
                if (vk == 0x1B)
                {
                    isListeningForKey = false;
                    RebuildButtons();
                    Invalidate();
                    return;
                }
                if (vk == 0x2E || vk == 0x08)
                {
                    AssignBindToSlot(selectedBindSlot, new TriggerBind("None", 0));
                    isListeningForKey = false;
                    RebuildButtons();
                    Invalidate();
                    return;
                }
                AssignBindToSlot(selectedBindSlot, new TriggerBind("Key", vk));
                isListeningForKey = false;
                RebuildButtons();
                Invalidate();
                return;
            }

            if (bindIncrement.MatchesKey(vk)) DoIncrement();
            else if (bindSection.MatchesKey(vk)) DoNewSection();
            else if (bindDecrement.MatchesKey(vk)) DoDecrement();
            else if (bindUndo.MatchesKey(vk)) DoUndoSection();
            else if (bindGhost.MatchesKey(vk)) ToggleGhostMode();
            else if (bindMode.MatchesKey(vk)) SetViewMode((viewMode + 1) % 4);
            else if (isSettingsOpen && settingsTab == 0) Invalidate();
        }

        TriggerBind GetBindSlot(int slot)
        {
            switch (slot)
            {
                case 1: return bindIncrement;
                case 2: return bindSection;
                case 3: return bindDecrement;
                case 4: return bindUndo;
                case 5: return bindGhost;
                case 6: return bindMode;
                default: return bindIncrement;
            }
        }

        void AssignBindToSlot(int slot, TriggerBind nb)
        {
            switch (slot)
            {
                case 1: bindIncrement = nb; break;
                case 2: bindSection = nb; break;
                case 3: bindDecrement = nb; break;
                case 4: bindUndo = nb; break;
                case 5: bindGhost = nb; break;
                case 6: bindMode = nb; break;
            }
            SaveSettings();
        }

        public void ToggleGhostMode()
        {
            isGhostMode = !isGhostMode;
            if (isGhostMode)
            {
                isMouseInsideWindow = false;
                isMouseDownOnWindow = false;
                isDraggingWindow = false;
                isResizingWindow = false;
                hoveredBtnIndex = -1;
                hoveredStickerIndex = -1;
            }
            UpdateDwmAndRegion();
            ApplyGhostExStyle();
            SaveSettings();
            RebuildButtons();
            Invalidate();
        }

        void SetViewMode(int mode)
        {
            viewMode = mode % 4;
            isSettingsOpen = false;
            isListeningForKey = false;
            ApplyViewModeGeometry();
            EnsureTextFitsInWindow();
            UpdateDwmAndRegion();
            SaveSettings();
            RebuildButtons();
            Invalidate();
        }

        void OpenSettingsDrawer()
        {
            isSettingsOpen = true;
            ApplyViewModeGeometry();
            UpdateDwmAndRegion();
            RebuildButtons();
            Invalidate();
        }

        void ToggleSettings()
        {
            isSettingsOpen = !isSettingsOpen;
            isListeningForKey = false;
            ApplyViewModeGeometry();
            EnsureTextFitsInWindow();
            UpdateDwmAndRegion();
            RebuildButtons();
            Invalidate();
            if (!isSettingsOpen) TrimMemory();
        }

        void ApplyViewModeGeometry()
        {
            if (isSettingsOpen)
            {
                // Dedicated spacious Settings Studio window (no cramping!)
                Size = new Size(650, 560);
                return;
            }
            if (viewMode == 3) // Just Number Mode
            {
                Size = customFloatSize;
            }
            else if (viewMode == 2) // Slim Gaming HUD Pill
            {
                Size = new Size(340, 72);
            }
            else if (viewMode == 1) // Mini Card
            {
                Size = new Size(380, 210);
            }
            else // Full Spacious Mode
            {
                Size = customFullSize;
            }
        }

        void LoadFromFileSync()
        {
            lock (fileLock)
            {
                var parsed = new List<long>();
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string t = lines[i].Trim();
                        long v;
                        if (long.TryParse(t, out v)) parsed.Add(v);
                    }
                }
                catch { }

                if (parsed.Count == 0) parsed.Add(0);
                sections = parsed;

                if (!sessionInitialized)
                {
                    sessionStartValue = sections[sections.Count - 1];
                    sessionInitialized = true;
                }

                if (sectionStarts.Count == 0)
                {
                    sectionStarts.Add(sections[0]);
                    for (int i = 1; i < sections.Count; i++)
                        sectionStarts.Add(sections[i - 1]);
                }
                else
                {
                    while (sectionStarts.Count < sections.Count)
                    {
                        int idx = sectionStarts.Count;
                        sectionStarts.Add(idx > 0 ? sections[idx - 1] : sections[0]);
                    }
                    while (sectionStarts.Count > sections.Count)
                        sectionStarts.RemoveAt(sectionStarts.Count - 1);
                }
            }
        }

        int saveQueued = 0;

        // Coalesced background save: bursts of clicks collapse into a single disk write that always contains the latest value
        void SaveToFileAsync()
        {
            if (Interlocked.Exchange(ref saveQueued, 1) == 1) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Interlocked.Exchange(ref saveQueued, 0);
                SaveToFileSync();
            });
        }

        void SaveToFileSync()
        {
            lock (fileLock)
            {
                try
                {
                    ignoreNextWatchEvent = true;
                    var sb = new StringBuilder(sections.Count * 18);
                    for (int i = 0; i < sections.Count; i++)
                    {
                        if (i > 0) sb.AppendLine("----------");
                        sb.AppendLine(sections[i].ToString());
                    }
                    File.WriteAllText(filePath, sb.ToString());
                }
                catch { }
            }
        }

        public void DoIncrement()
        {
            lock (fileLock)
            {
                sections[sections.Count - 1] += stepAmount;
            }
            DateTime now = DateTime.UtcNow;
            recentClicks.Add(now);
            while (recentClicks.Count > 0 && (now - recentClicks[0]).TotalSeconds > 60.0) recentClicks.RemoveAt(0);

            SaveToFileAsync();
            if (soundEnabled && tickPlayer != null) { try { tickPlayer.Play(); } catch { } }
            scrollOffset = int.MaxValue;
            EnsureTextFitsInWindow();
            EnforceGameTopmost();
            UpdateTrayText();
            Invalidate();
        }

        public void DoDecrement()
        {
            lock (fileLock)
            {
                sections[sections.Count - 1] -= stepAmount;
            }
            SaveToFileAsync();
            EnsureTextFitsInWindow();
            UpdateTrayText();
            Invalidate();
        }

        public void DoNewSection()
        {
            lock (fileLock)
            {
                long current = sections[sections.Count - 1];
                sections.Add(current);
                sectionStarts.Add(current);
            }
            SaveToFileAsync();
            scrollOffset = int.MaxValue;
            Invalidate();
        }

        public void DoUndoSection()
        {
            lock (fileLock)
            {
                if (sections.Count > 1)
                {
                    long current = sections[sections.Count - 1];
                    sections.RemoveAt(sections.Count - 1);
                    sections[sections.Count - 1] = current;
                    if (sectionStarts.Count > sections.Count)
                        sectionStarts.RemoveAt(sectionStarts.Count - 1);
                }
            }
            SaveToFileAsync();
            Invalidate();
        }

        void PromptSetExactNumber()
        {
            using (var dlg = new Form
            {
                Text = "Set Counter Value",
                Size = new Size(280, 145),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(18, 24, 38),
                ForeColor = Color.White
            })
            {
                var lbl = new Label { Text = "Enter exact counter number:", Left = 16, Top = 14, Width = 230, ForeColor = Color.LightGray };
                var tb = new TextBox { Text = sections[sections.Count - 1].ToString(), Left = 16, Top = 38, Width = 230 };
                var ok = new Button { Text = "Save", Left = 90, Top = 72, Width = 75, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", Left = 171, Top = 72, Width = 75, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                dlg.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    long v;
                    if (long.TryParse(tb.Text.Trim(), out v))
                    {
                        lock (fileLock) { sections[sections.Count - 1] = v; }
                        SaveToFileAsync();
                        EnsureTextFitsInWindow();
                        Invalidate();
                    }
                }
            }
        }

        void PromptCustomPrefixSuffix()
        {
            using (var dlg = new Form
            {
                Text = "Custom Number Prefix / Suffix",
                Size = new Size(320, 190),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(18, 24, 38),
                ForeColor = Color.White
            })
            {
                var l1 = new Label { Text = "Prefix (before number, e.g. 'CHESTS '):", Left = 16, Top = 12, Width = 270, ForeColor = Color.LightGray };
                var tb1 = new TextBox { Text = numberPrefix, Left = 16, Top = 32, Width = 270 };
                var l2 = new Label { Text = "Suffix (after number, e.g. ' WINS'):", Left = 16, Top = 64, Width = 270, ForeColor = Color.LightGray };
                var tb2 = new TextBox { Text = numberSuffix, Left = 16, Top = 84, Width = 270 };
                var ok = new Button { Text = "Apply", Left = 125, Top = 118, Width = 75, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", Left = 208, Top = 118, Width = 75, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                dlg.Controls.AddRange(new Control[] { l1, tb1, l2, tb2, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    numberPrefix = tb1.Text;
                    numberSuffix = tb2.Text;
                    SaveSettings();
                    EnsureTextFitsInWindow();
                    Invalidate();
                }
            }
        }

        void AddStickerFromFile()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "Add Draggable Sticker / Image to Overlay",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.ico;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.ico;*.bmp|All Files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    var st = new OverlaySticker { Source = ofd.FileName, OffsetX = -110 + stickers.Count * 28, OffsetY = 0, SizePx = 56 };
                    st.EnsureCache();
                    stickers.Add(st);
                    SaveSettings();
                    RebuildButtons();
                    Invalidate();
                }
            }
        }

        void AddBuiltInSticker(string code)
        {
            var st = new OverlaySticker { Source = "builtin:" + code, OffsetX = -110 + stickers.Count * 28, OffsetY = 0, SizePx = 52 };
            stickers.Add(st);
            SaveSettings();
            RebuildButtons();
            Invalidate();
        }

        void RebuildVisualKeyMap(int startX, int startY, int availW)
        {
            visualKeys.Clear();
            int kbW = (int)(availW * 0.72);
            int rowH = 26;
            int gap = 3;

            List<string[]> rows = new List<string[]>();
            if (kbSizeMode <= 2)
            {
                if (kbSizeMode == 0)
                    rows.Add(new string[] { "Esc:27", "F1:112", "F2:113", "F3:114", "F4:115", "F5:116", "F6:117", "F7:118", "F8:119", "F9:120", "F10:121", "F11:122", "F12:123", "Ins:45", "Del:46" });
                else
                    rows.Add(new string[] { "Esc:27", "F1:112", "F2:113", "F3:114", "F4:115", "F5:116", "F6:117", "F7:118", "F8:119", "F9:120", "F10:121", "F11:122", "F12:123" });
            }

            if (kbSizeMode == 0) // 100% Full
            {
                rows.Add(new string[] { "`:192", "1:49", "2:50", "3:51", "4:52", "5:53", "6:54", "7:55", "8:56", "9:57", "0:48", "-:189", "=:187", "Bksp:8", "N/:111", "N*:106", "N-:109" });
                rows.Add(new string[] { "Tab:9", "Q:81", "W:87", "E:69", "R:82", "T:84", "Y:89", "U:85", "I:73", "O:79", "P:80", "[:219", "]:221", "N7:103", "N8:104", "N9:105", "N+:107" });
                rows.Add(new string[] { "Caps:20", "A:65", "S:83", "D:68", "F:70", "G:71", "H:72", "J:74", "K:75", "L:76", ";:186", "Enter:13", "N4:100", "N5:101", "N6:102" });
                rows.Add(new string[] { "Shift:16", "Z:90", "X:88", "C:67", "V:86", "B:66", "N:78", "M:77", ",:188", ".:190", "Up:38", "N1:97", "N2:98", "N3:99" });
                rows.Add(new string[] { "Ctrl:17", "Alt:18", "Space:32", "Left:37", "Down:40", "Right:39", "N0:96", "N.:110" });
            }
            else if (kbSizeMode == 4) // 60% Mini
            {
                rowH = 30;
                rows.Add(new string[] { "Esc:27", "1:49", "2:50", "3:51", "4:52", "5:53", "6:54", "7:55", "8:56", "9:57", "0:48", "-:189", "=:187", "Bksp:8" });
                rows.Add(new string[] { "Tab:9", "Q:81", "W:87", "E:69", "R:82", "T:84", "Y:89", "U:85", "I:73", "O:79", "P:80", "[:219", "]:221", "\\:220" });
                rows.Add(new string[] { "Caps:20", "A:65", "S:83", "D:68", "F:70", "G:71", "H:72", "J:74", "K:75", "L:76", ";:186", "':222", "Enter:13" });
                rows.Add(new string[] { "Shift:16", "Z:90", "X:88", "C:67", "V:86", "B:66", "N:78", "M:77", ",:188", ".:190", "/:191" });
                rows.Add(new string[] { "Ctrl:17", "Alt:18", "Space:32" });
            }
            else // 80% TKL, 75%, 65%
            {
                rows.Add(new string[] { "`:192", "1:49", "2:50", "3:51", "4:52", "5:53", "6:54", "7:55", "8:56", "9:57", "0:48", "-:189", "=:187", "Bksp:8", "Home:36" });
                rows.Add(new string[] { "Tab:9", "Q:81", "W:87", "E:69", "R:82", "T:84", "Y:89", "U:85", "I:73", "O:79", "P:80", "[:219", "]:221", "PgUp:33" });
                rows.Add(new string[] { "Caps:20", "A:65", "S:83", "D:68", "F:70", "G:71", "H:72", "J:74", "K:75", "L:76", ";:186", "Enter:13", "PgDn:34" });
                rows.Add(new string[] { "Shift:16", "Z:90", "X:88", "C:67", "V:86", "B:66", "N:78", "M:77", ",:188", ".:190", "Up:38", "End:35" });
                rows.Add(new string[] { "Ctrl:17", "Alt:18", "Space:32", "Ins:45", "Del:46", "Left:37", "Down:40", "Right:39" });
            }

            for (int r = 0; r < rows.Count; r++)
            {
                string[] keys = rows[r];
                int n = keys.Length;
                int x = startX;
                int y = startY + r * (rowH + gap);

                for (int c = 0; c < n; c++)
                {
                    string[] parts = keys[c].Split(':');
                    string lbl = parts[0];
                    int vk = int.Parse(parts[1]);

                    int kw = (kbW - (n - 1) * gap) / n;
                    if (lbl == "Space") kw = kw * 2 + gap;
                    if (c == n - 1) kw = (startX + kbW) - x;

                    visualKeys.Add(new VisualKeyNode { Label = lbl, Kind = "Key", Code = vk, Bounds = new Rectangle(x, y, Math.Max(18, kw), rowH) });
                    x += kw + gap;
                }
            }

            // Sculpted Gaming Mouse Hit Zones (Spacious!)
            int mx = startX + kbW + 22;
            int mw = availW - kbW - 22;
            int bodyX = mx + 30;
            int bodyW = Math.Min(110, mw - 32);
            int bodyY = startY + 4;

            visualKeys.Add(new VisualKeyNode { Label = "M5", Kind = "Mouse", Code = 2, Bounds = new Rectangle(bodyX - 24, bodyY + 42, 28, 32) });
            visualKeys.Add(new VisualKeyNode { Label = "M4", Kind = "Mouse", Code = 1, Bounds = new Rectangle(bodyX - 24, bodyY + 80, 28, 32) });
            visualKeys.Add(new VisualKeyNode { Label = "MMB", Kind = "Mouse", Code = 3, Bounds = new Rectangle(bodyX + bodyW / 2 - 13, bodyY + 18, 26, 38) });
        }

        void RebuildButtons()
        {
            buttons.Clear();
            swatchRects.Clear();
            visualKeys.Clear();

            // In Just Number Mode (when Settings is closed), show sleek corner mini-buttons on hover (only when Pass-Through is OFF)!
            if (viewMode == 3 && !isSettingsOpen)
            {
                if (isMouseInsideWindow && !isGhostMode)
                {
                    int rx = Width - 6;
                    AddBtn("close", "✕", "Close Counter", ref rx, 4, 24, 20, true, false, false, () => Close());
                    AddBtn("mode", "FULL", "Switch back to Full Mode", ref rx, 4, 42, 20, false, true, false, () => SetViewMode(0));
                    AddBtn("set", "⚙", "Open Settings & Overlay Studio", ref rx, 4, 26, 20, false, false, false, () => OpenSettingsDrawer());
                    AddBtn("ghost_num", "👻 PASS", "Enable Mouse Pass-Through Mode (" + bindGhost.ShortName() + " to toggle back)", ref rx, 4, 58, 20, false, true, false, () => ToggleGhostMode());
                }
                return;
            }

            if (viewMode == 2 && !isSettingsOpen) // Slim Gaming HUD Pill
            {
                int rx = Width - 10;
                AddBtn("close", "✕", "Close Counter", ref rx, 8, 24, 20, true, false, false, () => Close());
                AddBtn("mode", "MODE", "Switch View Mode", ref rx, 8, 44, 20, false, true, false, () => SetViewMode((viewMode + 1) % 4));
                AddBtn("ghost", isGhostMode ? "PASS ON" : "👻 PASS", "Toggle Mouse Pass-Through Mode (" + bindGhost.ShortName() + ")", ref rx, 8, 56, 20, false, isGhostMode, isGhostMode, () => ToggleGhostMode());
                return;
            }

            // Top Header Bar
            int x = Width - 16;
            int y = 12;
            int h = 26;

            AddBtn("close", "✕", "Close Counter (Esc / Right-Click)", ref x, y, 28, h, true, false, false, () => Close());
            AddBtn("min", "—", "Minimize Window", ref x, y, 26, h, false, false, false, () => { WindowState = FormWindowState.Minimized; TrimMemory(); });
            string[] modeNames = new string[] { "🗕 Full", "🗕 Mini", "🗕 HUD", "🗕 Num" };
            AddBtn("mode", modeNames[viewMode % 4], "Cycle View Modes: Full -> Mini -> HUD Pill -> Just Number (" + bindMode.ShortName() + ")", ref x, y, 62, h, false, viewMode != 0, false, () => SetViewMode((viewMode + 1) % 4));
            AddBtn("ghost_top", isGhostMode ? "👻 Pass: ON" : "👻 Pass", "Toggle Mouse Pass-Through Mode so mouse goes straight through into your game (" + bindGhost.ShortName() + ")", ref x, y, 76, h, false, isGhostMode, isGhostMode, () => ToggleGhostMode());
            AddBtn("set", isSettingsOpen ? "✓ Done" : "⚙ Setup", "Open Spacious Settings & Overlay Studio", ref x, y, 68, h, false, isSettingsOpen, isSettingsOpen, () => ToggleSettings());
            AddBtn("snd_top", soundEnabled ? "🔊" : "🔇", soundEnabled ? "Click Sound is ON (Click to Mute)" : "Muted / Silent (Click to Enable Tick Sound)", ref x, y, 28, h, false, soundEnabled, soundEnabled, () =>
            {
                soundEnabled = !soundEnabled;
                SaveSettings();
                RebuildButtons();
                Invalidate();
            });

            if (isSettingsOpen)
            {
                BuildSpaciousSettingsButtons();
                return;
            }

            if (viewMode == 1) return; // Mini Card

            // Action Bar in Full Mode (-Step / New Section / Undo / +Step)
            int actY = 192;
            int totalActW = 58 + 10 + 168 + 10 + 72 + 10 + 58;
            int actX = (Width - totalActW) / 2;

            AddFixedBtn("act_minus", "−" + stepAmount, "Subtract " + stepAmount, new Rectangle(actX, actY, 58, 30), false, false, false, () => DoDecrement());
            actX += 68;
            AddFixedBtn("act_split", "⎯⎯  New Section  ⎯⎯", "Start a new section without resetting (" + bindSection.ShortName() + ")", new Rectangle(actX, actY, 168, 30), false, false, false, () => DoNewSection());
            actX += 178;
            AddFixedBtn("act_undo", "↶ Undo", "Merge current section back into previous section", new Rectangle(actX, actY, 72, 30), false, false, false, () => DoUndoSection());
            actX += 82;
            AddFixedBtn("act_plus", "+" + stepAmount, "Add " + stepAmount + " (" + bindIncrement.ShortName() + ")", new Rectangle(actX, actY, 58, 30), false, true, false, () => DoIncrement());
        }

        // Spacious Full-Window Settings Studio (650x560, zero cramping!)
        void BuildSpaciousSettingsButtons()
        {
            int tabY = 160;
            int tabW = (Width - 40 - 32) / 5;
            int tx = 20;
            string[] tabNames = new string[] { "⌨ Visual Keys", "🖼 Stickers & Decor", "🎨 Themes (19)", "🔤 Font & Size", "🎮 Game & Goal" };
            for (int t = 0; t < 5; t++)
            {
                int tabIdx = t;
                AddFixedBtn("tab_" + t, tabNames[t], "Switch settings tab", new Rectangle(tx, tabY, tabW, 30), false, settingsTab == t, settingsTab == t, () =>
                {
                    settingsTab = tabIdx;
                    isListeningForKey = false;
                    RebuildButtons();
                    Invalidate();
                });
                tx += tabW + 8;
            }

            int bodyY = tabY + 46;

            if (settingsTab == 0) // Tab 0: Spacious Visual Keyboard & Sculpted Gaming Mouse
            {
                string[] slotNames = new string[] {
                    "1. Add +" + stepAmount + ": " + bindIncrement.ShortName(),
                    "2. Split: " + bindSection.ShortName(),
                    "3. Sub −" + stepAmount + ": " + bindDecrement.ShortName(),
                    "4. Undo: " + bindUndo.ShortName(),
                    "5. Ghost: " + bindGhost.ShortName(),
                    "6. Mode: " + bindMode.ShortName()
                };
                int colW = (Width - 56) / 3;
                for (int s = 0; s < 6; s++)
                {
                    int slotNum = s + 1;
                    int col = s % 3;
                    int row = s / 3;
                    bool sel = (selectedBindSlot == slotNum);
                    string lbl = (sel && isListeningForKey) ? "▶ Press Key / Click Mouse..." : slotNames[s];
                    AddFixedBtn("slot_" + slotNum, lbl, "Select this slot, then click any key/mouse button on the diagram below (or press physically)",
                        new Rectangle(20 + col * (colW + 8), bodyY + row * 34, colW, 28), false, sel, sel, () =>
                        {
                            selectedBindSlot = slotNum;
                            isListeningForKey = true;
                            RebuildButtons();
                            Invalidate();
                        });
                }

                string[] kbSizes = new string[] { "100% Full", "80% TKL", "75%", "65%", "60% Mini" };
                int kbx = 135;
                for (int k = 0; k < kbSizes.Length; k++)
                {
                    int kMode = k;
                    bool act = (kbSizeMode == kMode);
                    AddFixedBtn("kbsz_" + k, kbSizes[k], "Switch keyboard form factor to " + kbSizes[k],
                        new Rectangle(kbx, bodyY + 72, 76, 25), false, act, act, () =>
                        {
                            kbSizeMode = kMode;
                            detectedKbSummary = kbSizes[kMode];
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                    kbx += 82;
                }

                AddFixedBtn("unbind_slot", "Clear Slot", "Unbind selected hotkey", new Rectangle(Width - 102, bodyY + 72, 82, 25), false, false, false, () =>
                {
                    AssignBindToSlot(selectedBindSlot, new TriggerBind("None", 0));
                    isListeningForKey = false;
                    RebuildButtons();
                    Invalidate();
                });

                RebuildVisualKeyMap(20, bodyY + 108, Width - 40);
            }
            else if (settingsTab == 1) // Tab 1: Draggable Stickers, Custom Images, Vector Emblems & Prefix/Suffix
            {
                int sy = bodyY + 6;
                AddFixedBtn("img_load", "📂 + Add Custom Image / Sticker...", "Add a PNG/GIF/ICO image (you can drag it anywhere & hold-click + wheel to resize!)",
                    new Rectangle(22, sy, 230, 30), false, true, false, () => AddStickerFromFile());

                AddFixedBtn("img_clear", "🗑 Clear Stickers (" + stickers.Count + ")", "Remove all stickers from the overlay",
                    new Rectangle(262, sy, 150, 30), false, false, false, () =>
                    {
                        for (int i = 0; i < stickers.Count; i++) stickers[i].DisposeCache();
                        stickers.Clear();
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });

                AddFixedBtn("txt_affix", "✏️ Prefix / Suffix (" + (string.IsNullOrEmpty(numberPrefix) && string.IsNullOrEmpty(numberSuffix) ? "None" : (numberPrefix + "#" + numberSuffix)) + ")",
                    "Set text before or after the counter (e.g. 'CHESTS ')",
                    new Rectangle(422, sy, 206, 30), false, !string.IsNullOrEmpty(numberPrefix) || !string.IsNullOrEmpty(numberSuffix), false, () => PromptCustomPrefixSuffix());

                sy += 58;
                // Built-in Draggable Game Icon Stickers (Treasure Chest, Key, Crown, Gem, Skull, Flame, Trophy, Star)
                string[] iconCodes = new string[] { "chest", "key", "crown", "gem", "skull", "flame", "trophy", "star" };
                string[] iconLabels = new string[] { "+ 🧰 Chest", "+ 🗝 Key", "+ 👑 Crown", "+ 💎 Gem", "+ 💀 Skull", "+ 🔥 Flame", "+ 🏆 Trophy", "+ ⭐ Star" };
                int iw = (Width - 44 - 28) / 4;
                for (int i = 0; i < iconCodes.Length; i++)
                {
                    string code = iconCodes[i];
                    int col = i % 4;
                    int r = i / 4;
                    AddFixedBtn("bicon_" + i, iconLabels[i], "Add a draggable " + iconLabels[i] + " sticker to the counter (drag it anywhere, hold-click + wheel to resize!)",
                        new Rectangle(22 + col * (iw + 8), sy + r * 34, iw, 28), false, false, false, () => AddBuiltInSticker(code));
                }

                sy += 92;
                string[] emblems = new string[] { "No Frame", "👑 Crown", "🎯 Brackets", "⚡ Cyber HUD", "🔥 Wings", "💀 Skull", "⭐ Stars", "💎 Diamonds" };
                int ew = (Width - 44 - 28) / 4;
                for (int e = 0; e < emblems.Length; e++)
                {
                    int eIdx = e;
                    int col = e % 4;
                    int r = e / 4;
                    bool act = (vectorEmblem == eIdx);
                    AddFixedBtn("emb_" + e, emblems[e], "Toggle centered vector frame around the number",
                        new Rectangle(22 + col * (ew + 8), sy + r * 34, ew, 28), false, act, act, () =>
                        {
                            vectorEmblem = eIdx;
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                sy += 82;
                string[] fxNames = new string[] { "Outline + Glow", "Crisp Outline", "3D Shadow", "Custom Gradient", "Clean Flat" };
                int fxW = (Width - 44 - 32) / 5;
                for (int f = 0; f < fxNames.Length; f++)
                {
                    int fIdx = f;
                    bool act = (numberEffectStyle == fIdx);
                    AddFixedBtn("nfx_" + f, fxNames[f], "Change number text effect",
                        new Rectangle(22 + f * (fxW + 8), sy, fxW, 27), false, act, act, () =>
                        {
                            numberEffectStyle = fIdx;
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                sy += 34;
                AddFixedBtn("grad_c1", "🌈 Gradient Color 1", "Pick first color of custom number gradient",
                    new Rectangle(22, sy, 148, 27), false, true, numberEffectStyle == 3 && useCustomGradColors, () => PickGradientColor(1));
                AddFixedBtn("grad_c2", "🌈 Gradient Color 2", "Pick second color of custom number gradient",
                    new Rectangle(178, sy, 148, 27), false, true, numberEffectStyle == 3 && useCustomGradColors, () => PickGradientColor(2));

                string[] dirNames = new string[] { "↕ Vertical", "↔ Horiz", "↘ Diag 1", "↗ Diag 2" };
                for (int d = 0; d < 4; d++)
                {
                    int dIdx = d;
                    bool act = (numberEffectStyle == 3 && gradDirection == dIdx);
                    AddFixedBtn("gdir_" + d, dirNames[d], "Change gradient direction",
                        new Rectangle(334 + d * 74, sy, 68, 27), false, act, act, () =>
                        {
                            numberEffectStyle = 3;
                            gradDirection = dIdx;
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }
            }
            else if (settingsTab == 2) // Tab 2: 18 Preset Themes + Custom Color Picker
            {
                int swStartX = 24;
                int swStartY = bodyY + 30;
                for (int i = 0; i < themes.Count; i++)
                {
                    int col = i % 10;
                    int r = i / 10;
                    swatchRects.Add(new Rectangle(swStartX + col * 58, swStartY + r * 42, 32, 32));
                }

                int custY = bodyY + 135;
                AddFixedBtn("cust_bg", "🎨 Pick Background Color", "Choose custom background color", new Rectangle(24, custY, 192, 32), false, false, false, () => PickCustomColor(0));
                AddFixedBtn("cust_acc", "✨ Pick Accent Color", "Choose custom neon/accent color", new Rectangle(228, custY, 192, 32), false, true, false, () => PickCustomColor(1));
                AddFixedBtn("cust_txt", "🔤 Pick Text Color", "Choose custom number/text color", new Rectangle(432, custY, 192, 32), false, false, false, () => PickCustomColor(2));

                int opY = custY + 56;
                int[] ops = new int[] { 40, 60, 75, 85, 95, 100 };
                int ox = 125;
                foreach (int op in ops)
                {
                    int targetOp = op;
                    bool activeOp = Math.Abs((int)Math.Round(userOpacity * 100) - targetOp) <= 4;
                    AddFixedBtn("op_" + op, op + "%", "Set window opacity", new Rectangle(ox, opY, 68, 28), false, activeOp, activeOp, () =>
                    {
                        userOpacity = targetOp / 100.0;
                        UpdateDwmAndRegion();
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });
                    ox += 76;
                }
            }
            else if (settingsTab == 3) // Tab 3: Fonts, Number Size & UI Panels
            {
                int fw = (Width - 48 - 24) / 4;
                for (int f = 0; f < fontFamilies.Length; f++)
                {
                    int fi = f;
                    int col = f % 4;
                    int row = f / 4;
                    bool act = (fontFamilyIdx == fi);
                    AddFixedBtn("font_" + f, fontFamilies[f], "Use " + fontFamilies[f] + " font",
                        new Rectangle(24 + col * (fw + 8), bodyY + 26 + row * 36, fw, 29), false, act, act, () =>
                        {
                            fontFamilyIdx = fi;
                            RebuildFonts();
                            EnsureTextFitsInWindow();
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                int szY = bodyY + 118;
                int[] sizes = new int[] { 28, 38, 48, 60, 74, 92 };
                string[] szNames = new string[] { "XS (28)", "S (38)", "M (48)", "L (60)", "XL (74)", "XXL (92)" };
                for (int s = 0; s < sizes.Length; s++)
                {
                    int sz = sizes[s];
                    bool act = (Math.Abs(heroFontSizePt - sz) <= 3);
                    AddFixedBtn("sz_" + sz, szNames[s], "Set main number font size (or Hold Left-Click + Scroll Wheel anywhere!)",
                        new Rectangle(125 + s * 82, szY, 76, 28), false, act, act, () =>
                        {
                            heroFontSizePt = sz;
                            RebuildFonts();
                            EnsureTextFitsInWindow();
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                int outY = szY + 46;
                int[] outs = new int[] { 0, 2, 3, 5 };
                string[] outLbls = new string[] { "No Outline", "Thin (2px)", "Medium (3px)", "Heavy (5px)" };
                for (int o = 0; o < outs.Length; o++)
                {
                    int oVal = outs[o];
                    bool act = (outlineThickness == oVal);
                    AddFixedBtn("out_" + o, outLbls[o], "Set text outline thickness for floating overlay readability",
                        new Rectangle(125 + o * 115, outY, 108, 28), false, act, act, () =>
                        {
                            outlineThickness = oVal;
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                int togY = outY + 42;
                AddFixedBtn("f_grad_c1", "🌈 Grad Start Color", "Pick custom gradient start color",
                    new Rectangle(24, togY, 145, 27), false, true, numberEffectStyle == 3 && useCustomGradColors, () => PickGradientColor(1));
                AddFixedBtn("f_grad_c2", "🌈 Grad End Color", "Pick custom gradient end color",
                    new Rectangle(177, togY, 145, 27), false, true, numberEffectStyle == 3 && useCustomGradColors, () => PickGradientColor(2));

                string[] gPresets = new string[] { "🔥 Fire", "⚡ Cyber", "👑 Gold", "❄ Ice" };
                string[][] gHex = new string[][] {
                    new string[] { "#FACC15", "#EF4444" },
                    new string[] { "#00F5D4", "#A855F7" },
                    new string[] { "#FFFBEB", "#D97706" },
                    new string[] { "#E0F2FE", "#2563EB" }
                };
                for (int gp = 0; gp < gPresets.Length; gp++)
                {
                    int gpIdx = gp;
                    AddFixedBtn("gpre_" + gp, gPresets[gp], "Apply gradient preset",
                        new Rectangle(330 + gp * 74, togY, 68, 27), false, false, false, () =>
                        {
                            gradColor1 = Theme.C(gHex[gpIdx][0]);
                            gradColor2 = Theme.C(gHex[gpIdx][1]);
                            useCustomGradColors = true;
                            numberEffectStyle = 3;
                            SaveSettings();
                            RebuildButtons();
                            Invalidate();
                        });
                }

                togY += 36;
                AddFixedBtn("tog_badges", showBadges ? "✓ Top Badges: ON" : "Top Badges: OFF", "Show or hide Section/Session badges",
                    new Rectangle(24, togY, 142, 27), false, showBadges, showBadges, () => { showBadges = !showBadges; SaveSettings(); RebuildButtons(); Invalidate(); });
                AddFixedBtn("tog_goal", showGoalBar ? "✓ Goal Bar: ON" : "Goal Bar: OFF", "Show or hide target milestone progress bar",
                    new Rectangle(174, togY, 142, 27), false, showGoalBar, showGoalBar, () => { showGoalBar = !showGoalBar; SaveSettings(); RebuildButtons(); Invalidate(); });
                AddFixedBtn("tog_spark", showSparkline ? "✓ Sparkline: ON" : "Sparkline: OFF", "Show or hide live section trend sparkline graph",
                    new Rectangle(324, togY, 142, 27), false, showSparkline, showSparkline, () => { showSparkline = !showSparkline; SaveSettings(); RebuildButtons(); Invalidate(); });
                AddFixedBtn("tog_snd", soundEnabled ? "🔊 Sound: ON" : "🔇 Sound: OFF", "Mute or unmute click tick sound",
                    new Rectangle(474, togY, 150, 27), false, soundEnabled, soundEnabled, () => { soundEnabled = !soundEnabled; SaveSettings(); RebuildButtons(); Invalidate(); });
            }
            else if (settingsTab == 4) // Tab 4: Maximized Game Overlay, Step Size, Target Goal & Exact Value
            {
                int gy = bodyY + 12;
                AddFixedBtn("game_noact", gameOverlayNoActivate ? "✓ Maximized Game Overlay Mode: ON (Never Steals Game Focus)" : "Maximized Game Overlay Mode: OFF",
                    "Keeps counter above borderless/maximized games without touching game processes",
                    new Rectangle(24, gy, Width - 48, 32), false, gameOverlayNoActivate, gameOverlayNoActivate, () =>
                    {
                        gameOverlayNoActivate = !gameOverlayNoActivate;
                        int ex = GetWindowLong(this.Handle, GWL_EXSTYLE);
                        if (gameOverlayNoActivate) SetWindowLong(this.Handle, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE);
                        else SetWindowLong(this.Handle, GWL_EXSTYLE, ex & ~WS_EX_NOACTIVATE);
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });

                gy += 44;
                AddFixedBtn("game_ghost", isGhostMode ? "👻 Click-Through Ghost Mode: ON" : "👻 Enable Click-Through Ghost Mode (" + bindGhost.ShortName() + ")",
                    "Clicks pass straight through the counter into your game",
                    new Rectangle(24, gy, 290, 30), false, isGhostMode, isGhostMode, () => ToggleGhostMode());

                AddFixedBtn("mode_numonly", "🔢 Switch to 'Just Number' Floating Mode",
                    "Hides card background and borders so ONLY the floating number (+ stickers) stays on screen",
                    new Rectangle(326, gy, 300, 30), false, false, false, () => SetViewMode(3));

                gy += 48;
                int[] steps = new int[] { 1, 2, 5, 10, 25, 50 };
                int sx = 125;
                foreach (int st in steps)
                {
                    int stepVal = st;
                    bool act = (stepAmount == stepVal);
                    AddFixedBtn("step_" + st, "+" + st, "Increment/Decrement by " + st, new Rectangle(sx, gy, 64, 28), false, act, act, () =>
                    {
                        stepAmount = stepVal;
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });
                    sx += 72;
                }

                gy += 44;
                long[] goals = new long[] { 500, 1000, 2500, 5000, 10000, 25000 };
                int gx = 125;
                foreach (long gl in goals)
                {
                    long gVal = gl;
                    bool act = (targetGoal == gVal);
                    AddFixedBtn("goal_" + gl, gl.ToString("N0"), "Set milestone goal to " + gl, new Rectangle(gx, gy, 74, 28), false, act, act, () =>
                    {
                        targetGoal = gVal;
                        showGoalBar = true;
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });
                    gx += 80;
                }

                gy += 48;
                AddFixedBtn("set_exact", "✏️ Set Exact Value...", "Type a specific number to set the counter to", new Rectangle(24, gy, 180, 32), false, true, false, () => PromptSetExactNumber());
                AddFixedBtn("reset_sess", "↺ Reset Session (+0)", "Reset the session gain counter to +0", new Rectangle(212, gy, 175, 32), false, false, false, () =>
                {
                    sessionStartValue = sections[sections.Count - 1];
                    Invalidate();
                });
                bool isStretched = (Screen.PrimaryScreen.Bounds.Width == 1720);
                AddFixedBtn("fn_stretch", isStretched ? "✓ 1720×1080 Res: ON" : "🎮 1720×1080 Res Fix",
                    "Toggles Windows desktop between 1720x1080 and 1920x1080 so overlays work over Stretched Res Fortnite!",
                    new Rectangle(395, gy, 231, 32), false, isStretched, isStretched, () => ToggleStretchedDesktopResolution());

                // --- Fortnite Automation ---
                gy += 44;
                AddFixedBtn("auto_fn", autoLaunchRegistered ? "✓ Auto Open/Close with Fortnite: ON" : "🚀 Auto Open/Close with Fortnite: OFF",
                    "Counter opens automatically when Fortnite starts and closes when Fortnite exits (tiny idle watcher, starts with Windows)",
                    new Rectangle(24, gy, 300, 30), false, autoLaunchRegistered, autoLaunchRegistered, () =>
                    {
                        autoLaunchRegistered = !autoLaunchRegistered;
                        if (autoLaunchRegistered) AutoStart.Enable(); else AutoStart.Disable();
                        RebuildButtons();
                        Invalidate();
                    });
                AddFixedBtn("auto_res", autoStretchRes ? "✓ Auto Stretched Res with Fortnite: ON" : "Auto Stretched Res with Fortnite: OFF",
                    "Switches your desktop to Fortnite's stretched resolution when it launches and restores it when it closes (keeps your refresh rate)",
                    new Rectangle(332, gy, 294, 30), false, autoStretchRes, autoStretchRes, () =>
                    {
                        autoStretchRes = !autoStretchRes;
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });

                gy += 40;
                AddFixedBtn("auto_sec", autoSessionSection ? "✓ New Section Each Fortnite Session: ON" : "New Section Each Fortnite Session: OFF",
                    "Automatically adds a section divider each time Fortnite launches (skipped if the last section had no gains)",
                    new Rectangle(24, gy, 300, 30), false, autoSessionSection, autoSessionSection, () =>
                    {
                        autoSessionSection = !autoSessionSection;
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                    });
                AddFixedBtn("open_backups", "📁 Open Daily Backups (30 days)",
                    "counter.txt is backed up once per day automatically - open the folder",
                    new Rectangle(332, gy, 294, 30), false, false, false, () =>
                    {
                        try { Directory.CreateDirectory(backupDir); Process.Start("explorer.exe", "\"" + backupDir + "\""); } catch { }
                    });
            }
        }

        void PickGradientColor(int which)
        {
            using (var cd = new ColorDialog { FullOpen = true })
            {
                cd.Color = (which == 1) ? gradColor1 : gradColor2;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    if (which == 1) gradColor1 = cd.Color;
                    else gradColor2 = cd.Color;
                    useCustomGradColors = true;
                    numberEffectStyle = 3; // Activate Custom Gradient style automatically
                    SaveSettings();
                    RebuildButtons();
                    Invalidate();
                }
            }
        }

        void PickCustomColor(int which)
        {
            using (var cd = new ColorDialog { FullOpen = true })
            {
                cd.Color = (which == 0) ? customBg : (which == 1) ? customAccent : customText;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    if (which == 0) customBg = cd.Color;
                    else if (which == 1) customAccent = cd.Color;
                    else customText = cd.Color;

                    themes[themes.Count - 1] = Theme.FromCustomColors(customBg, customAccent, customText);
                    themeIdx = themes.Count - 1;
                    SaveSettings();
                    Invalidate();
                }
            }
        }

        void AddBtn(string id, string label, string tip, ref int rightX, int y, int w, int h, bool isClose, bool isAccent, bool isActive, Action onClick)
        {
            rightX -= w;
            buttons.Add(new UiButton { Id = id, Label = label, Tooltip = tip, Bounds = new Rectangle(rightX, y, w, h), IsClose = isClose, IsAccent = isAccent, IsActive = isActive, OnClick = onClick });
            rightX -= 5;
        }

        void AddFixedBtn(string id, string label, string tip, Rectangle bounds, bool isClose, bool isAccent, bool isActive, Action onClick)
        {
            buttons.Add(new UiButton { Id = id, Label = label, Tooltip = tip, Bounds = bounds, IsClose = isClose, IsAccent = isAccent, IsActive = isActive, OnClick = onClick });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme t = themes[themeIdx % themes.Count];

            long current;
            int secCount;
            long startVal;
            lock (fileLock)
            {
                current = sections[sections.Count - 1];
                secCount = sections.Count;
                startVal = (sectionStarts.Count >= secCount) ? sectionStarts[secCount - 1] : current;
            }
            long secDelta = current - startVal;
            long sessDelta = current - sessionStartValue;
            string displayNum = numberPrefix + current.ToString("N0") + numberSuffix;

            // --- View Mode 3: "Just Number" Transparent Floating Overlay (when Settings is closed) ---
            if (viewMode == 3 && !isSettingsOpen)
            {
                g.Clear(magicTransKey);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                Rectangle heroZone = new Rectangle(8, 8, Width - 16, Height - 16);
                DrawHeroNumberWithStickers(g, t, displayNum, fontHeroNumOnly, heroZone, true);

                // When mouse is inside the floating overlay (and Pass-Through is OFF), draw subtle corner handles + top-right buttons!
                if (isMouseInsideWindow && !isGhostMode)
                {
                    DrawButtons(g, t);
                    DrawCornerHandles(g, t, true);
                }
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var bgBrush = new LinearGradientBrush(ClientRectangle, t.BgTop, t.BgBottom, LinearGradientMode.Vertical))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // --- View Mode 2: Slim Gaming HUD Pill (when Settings is closed) ---
            if (viewMode == 2 && !isSettingsOpen)
            {
                DrawHudPillMode(g, t, displayNum, secCount, secDelta);
                DrawButtons(g, t);
                DrawOuterBorder(g, t);
                if (isMouseInsideWindow) DrawCornerHandles(g, t, false);
                return;
            }

            // --- Top Drag Bar ---
            using (var gripBrush = new SolidBrush(t.TextMuted))
            {
                for (int gy = 17; gy <= 25; gy += 4)
                {
                    g.FillEllipse(gripBrush, 14, gy, 3, 3);
                    g.FillEllipse(gripBrush, 20, gy, 3, 3);
                }
            }
            using (var dotBrush = new SolidBrush(isGhostMode ? Color.FromArgb(251, 191, 36) : t.Accent))
            {
                g.FillEllipse(dotBrush, 29, 19, 7, 7);
            }
            string hdrText = isGhostMode
                ? ("GHOST ON (" + bindGhost.ShortName() + ")")
                : (bindIncrement.ShortName() + " +" + stepAmount + "  •  " + bindSection.ShortName() + " SPLIT");
            using (var hdrBrush = new SolidBrush(t.TextSecondary))
            {
                g.DrawString(hdrText, fontHeader, hdrBrush, 40, 15);
            }

            // --- If Settings Studio is Open: Live Preview at Top + Spacious Tab Panel Below ---
            if (isSettingsOpen)
            {
                Rectangle previewRect = new Rectangle(20, 46, Width - 40, 104);
                using (GraphicsPath pp = RoundedRect(previewRect, 10))
                using (var pb = new SolidBrush(t.PanelBg))
                using (var ppen = new Pen(t.PanelBorder, 1f))
                {
                    g.FillPath(pb, pp);
                    g.DrawPath(ppen, pp);
                }
                using (var hintBrush = new SolidBrush(t.TextMuted))
                {
                    g.DrawString("LIVE PREVIEW  (Drag stickers directly • Hold-Click + Mouse Wheel scales size)", fontKeyCap, hintBrush, 28, 51);
                }
                DrawHeroNumberWithStickers(g, t, displayNum, fontHeroMini, new Rectangle(24, 62, Width - 48, 84), false);

                DrawSpaciousSettingsBody(g, t, 198);
                DrawButtons(g, t);
                DrawOuterBorder(g, t);
                DrawCornerHandles(g, t, false);
                return;
            }

            int curY = 46;
            int heroBoxHeight = (viewMode == 1) ? (Height - 54) : 138;

            if (showBadges)
            {
                DateTime now = DateTime.UtcNow;
                recentClicks.RemoveAll(dt => (now - dt).TotalSeconds > 60.0);
                int cpm = recentClicks.Count;

                string b1 = "SECTION " + secCount;
                string b2 = (secDelta >= 0 ? "+" : "") + secDelta.ToString("N0") + " in sec";
                string b3 = "Session " + (sessDelta >= 0 ? "+" : "") + sessDelta.ToString("N0") + (cpm > 0 ? (" • " + cpm + "/m") : "");

                SizeF s1 = g.MeasureString(b1, fontBadge);
                SizeF s2 = g.MeasureString(b2, fontBadge);
                SizeF s3 = g.MeasureString(b3, fontBadge);
                int w1 = (int)s1.Width + 14, w2 = (int)s2.Width + 14, w3 = (int)s3.Width + 14;
                int totalBadgeW = w1 + 6 + w2 + 6 + w3;
                int bx = (Width - totalBadgeW) / 2;
                int by = curY + 4;

                DrawPillBadge(g, new Rectangle(bx, by, w1, 20), b1, t.AccentSoftBg, t.AccentSoftBorder, t.Accent);
                bx += w1 + 6;
                DrawPillBadge(g, new Rectangle(bx, by, w2, 20), b2, t.PanelBg, t.PanelBorder, t.TextPrimary);
                bx += w2 + 6;
                DrawPillBadge(g, new Rectangle(bx, by, w3, 20), b3, t.PanelBg, t.PanelBorder, t.TextSecondary);
            }

            int topOff = showBadges ? 26 : 4;
            Rectangle heroRect = new Rectangle(16, curY + topOff, Width - 32, heroBoxHeight - topOff - (showGoalBar ? 14 : 2));
            DrawHeroNumberWithStickers(g, t, displayNum, (viewMode == 1) ? fontHeroMini : fontHeroFull, heroRect, false);

            // Milestone Goal Progress Bar
            if (showGoalBar && targetGoal > 0)
            {
                int barW = Math.Min(280, Width - 80);
                int barX = (Width - barW) / 2;
                int barY = curY + heroBoxHeight - 10;
                float pct = Math.Max(0f, Math.Min(1f, (float)current / targetGoal));
                using (var trackBrush = new SolidBrush(t.PanelBg))
                using (var fillBrush = new SolidBrush(t.Accent))
                {
                    g.FillRectangle(trackBrush, barX, barY, barW, 4);
                    g.FillRectangle(fillBrush, barX, barY, (int)(barW * pct), 4);
                }
            }

            if (viewMode == 0)
            {
                int listTop = curY + 184;
                Rectangle listRect = new Rectangle(16, listTop, Width - 32, Height - listTop - 16);
                using (GraphicsPath lp = RoundedRect(listRect, 10))
                using (var lb = new SolidBrush(t.PanelBg))
                using (var lpen = new Pen(t.PanelBorder, 1f))
                {
                    g.FillPath(lb, lp);
                    g.DrawPath(lpen, lp);
                }

                int innerTop = listRect.Y + 6;
                int innerH = listRect.Height - 12;
                if (showSparkline && secCount > 1)
                {
                    DrawSectionSparkline(g, t, new Rectangle(listRect.X + 10, listRect.Y + 5, listRect.Width - 20, 22));
                    innerTop += 24;
                    innerH -= 24;
                }

                DrawSectionsList(g, t, new Rectangle(listRect.X + 6, innerTop, listRect.Width - 12, Math.Max(20, innerH)));
            }

            DrawButtons(g, t);
            DrawOuterBorder(g, t);
            if (isMouseInsideWindow) DrawCornerHandles(g, t, false);

            if (!trimmedAfterFirstPaint)
            {
                trimmedAfterFirstPaint = true;
                BeginInvoke(new Action(TrimMemory));
            }
        }

        // Draws Corner Move & Resize Grip Handles so the user can easily drag or resize from the corners!
        void DrawCornerHandles(Graphics g, Theme t, bool isFloatingMode)
        {
            Color hc = isFloatingMode ? Color.FromArgb(230, t.Accent) : Color.FromArgb(180, t.Accent);
            using (var pen = new Pen(hc, 2.0f))
            {
                // Top-Left Move Grip (4 corner bracket lines)
                g.DrawLine(pen, 4, 4, 16, 4);
                g.DrawLine(pen, 4, 4, 4, 16);
                g.DrawLine(pen, 8, 8, 14, 8);
                g.DrawLine(pen, 8, 8, 8, 14);

                // Bottom-Right Resize Grip (diagonal resize chevrons)
                int rx = Width - 5, ry = Height - 5;
                g.DrawLine(pen, rx - 12, ry, rx, ry - 12);
                g.DrawLine(pen, rx - 8, ry, rx, ry - 8);
                g.DrawLine(pen, rx - 4, ry, rx, ry - 4);

                // Bottom-Left Resize Grip
                int lx = 5, ly = Height - 5;
                g.DrawLine(pen, lx + 12, ly, lx, ly - 12);
                g.DrawLine(pen, lx + 8, ly, lx, ly - 8);
            }
        }

        void DrawHeroNumberWithStickers(Graphics g, Theme t, string displayNum, Font f, Rectangle zone, bool isTransparentOverlay)
        {
            int cx = zone.X + zone.Width / 2;
            int cy = zone.Y + zone.Height / 2;

            // 1. Built-in Vector Frame / Emblem
            DrawVectorDecoration(g, t, zone, vectorEmblem);

            // 2. Render Centered Counter Number using Vector GraphicsPath so Fonts & Gradients work identically in Floating AND Card modes!
            SmoothingMode prevSmooth = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var textPath = new GraphicsPath())
            {
                FontFamily ff;
                try { ff = f.FontFamily; } catch { ff = FontFamily.GenericSansSerif; }
                int style = (int)FontStyle.Bold;
                if (!ff.IsStyleAvailable(FontStyle.Bold)) style = (int)FontStyle.Regular;

                float emSize = g.DpiY * f.SizeInPoints / 72f;
                textPath.AddString(displayNum, ff, style, emSize, zone, sf);

                // 2a. 3D Drop Shadow
                if (numberEffectStyle == 2)
                {
                    using (var shPath = (GraphicsPath)textPath.Clone())
                    using (var mat = new Matrix())
                    using (var shBrush = new SolidBrush(Color.FromArgb(220, 4, 6, 12)))
                    {
                        mat.Translate(4f, 4f);
                        shPath.Transform(mat);
                        g.FillPath(shBrush, shPath);
                    }
                }

                // 2b. Neon Glow Aura (in Card modes when style == 0)
                if (!isTransparentOverlay && numberEffectStyle == 0)
                {
                    Color gc = (useCustomGradColors && numberEffectStyle == 3) ? gradColor1 : t.Accent;
                    using (var glowPen = new Pen(Color.FromArgb(45, gc), Math.Max(4, outlineThickness * 2 + 4)) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(glowPen, textPath);
                    }
                }

                // 2c. Crisp Vector Outline (prevents any color-key fringing in Floating Mode and gives sharp stroke!)
                int outPx = outlineThickness;
                if (isTransparentOverlay && outPx < 2) outPx = 2; // Minimum 2px dark backing in transparent mode so font & gradient edges are razor-sharp
                if (outPx > 0 && numberEffectStyle != 4)
                {
                    using (var outPen = new Pen(Color.FromArgb(255, 8, 12, 20), outPx * 2) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(outPen, textPath);
                    }
                }
                else if (isTransparentOverlay)
                {
                    using (var guardPen = new Pen(Color.FromArgb(255, 8, 12, 20), 2.2f) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(guardPen, textPath);
                    }
                }

                // 2d. Fill Font Interior (Custom Gradient or Solid Theme Color — works in ALL modes including Floating!)
                if (numberEffectStyle == 3) // Custom / 2-Tone Gradient
                {
                    Color c1 = useCustomGradColors ? gradColor1 : t.TextPrimary;
                    Color c2 = useCustomGradColors ? gradColor2 : t.Accent;
                    LinearGradientMode gMode = (gradDirection == 1) ? LinearGradientMode.Horizontal
                                             : (gradDirection == 2) ? LinearGradientMode.ForwardDiagonal
                                             : (gradDirection == 3) ? LinearGradientMode.BackwardDiagonal
                                             : LinearGradientMode.Vertical;
                    RectangleF bounds = textPath.GetBounds();
                    Rectangle gradRect = new Rectangle(
                        (int)bounds.X - 2,
                        (int)bounds.Y - 2,
                        Math.Max(10, (int)bounds.Width + 4),
                        Math.Max(10, (int)bounds.Height + 4));
                    using (var gradBrush = new LinearGradientBrush(gradRect, c1, c2, gMode))
                    {
                        g.FillPath(gradBrush, textPath);
                    }
                }
                else
                {
                    Color mainColor = isTransparentOverlay ? t.Accent : t.TextPrimary;
                    using (var numBrush = new SolidBrush(mainColor))
                    {
                        g.FillPath(numBrush, textPath);
                    }
                }
            }
            g.SmoothingMode = prevSmooth;

            // 3. Render Free-Draggable Custom Images & Built-in Game Icon Stickers!
            for (int i = 0; i < stickers.Count; i++)
            {
                OverlaySticker st = stickers[i];
                int sz = Math.Max(16, Math.Min(320, st.SizePx));
                int sx = cx + st.OffsetX - sz / 2;
                int sy = cy + st.OffsetY - sz / 2;
                Rectangle sRect = new Rectangle(sx, sy, sz, sz);

                if (st.Source.StartsWith("builtin:"))
                {
                    string code = st.Source.Substring(8);
                    DrawBuiltInStickerIcon(g, t, code, sRect);
                }
                else
                {
                    st.EnsureCache();
                    if (st.CachedBmp != null)
                    {
                        sRect = new Rectangle(cx + st.OffsetX - st.CachedBmp.Width / 2, cy + st.OffsetY - st.CachedBmp.Height / 2, st.CachedBmp.Width, st.CachedBmp.Height);
                        g.DrawImage(st.CachedBmp, sRect);
                    }
                }

                st.LastDrawnBounds = sRect;

                // Highlight sticker border when hovered or dragging so user knows it's draggable & wheel-scalable!
                if (i == hoveredStickerIndex || i == draggingStickerIndex)
                {
                    using (var hPen = new Pen(t.Accent, 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawRectangle(hPen, sRect);
                    }
                }
            }
        }

        // Renders crisp built-in game stickers (Treasure Chest, Key, Crown, Gem, Skull, Flame, Trophy, Star)
        void DrawBuiltInStickerIcon(Graphics g, Theme t, string code, Rectangle r)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int x = r.X, y = r.Y, w = r.Width, h = r.Height;

            if (code == "chest") // Treasure Chest 🧰
            {
                Rectangle baseBox = new Rectangle(x + w / 10, y + h / 3, w * 8 / 10, h * 6 / 10);
                Rectangle lidBox = new Rectangle(x + w / 10, y + h / 6, w * 8 / 10, h / 4);
                using (var woodBrush = new SolidBrush(Color.FromArgb(180, 95, 35)))
                using (var lidBrush = new SolidBrush(Color.FromArgb(205, 115, 45)))
                using (var goldPen = new Pen(Color.FromArgb(250, 204, 21), Math.Max(2f, w / 18f)))
                using (var darkPen = new Pen(Color.FromArgb(20, 15, 10), Math.Max(2f, w / 22f)))
                using (var lockBrush = new SolidBrush(Color.FromArgb(250, 204, 21)))
                {
                    g.FillRectangle(woodBrush, baseBox);
                    g.FillRectangle(lidBrush, lidBox);
                    g.DrawRectangle(darkPen, baseBox);
                    g.DrawRectangle(darkPen, lidBox);
                    g.DrawRectangle(goldPen, baseBox.X + 2, baseBox.Y + 2, baseBox.Width - 4, baseBox.Height - 4);
                    g.DrawLine(goldPen, baseBox.X, lidBox.Bottom, baseBox.Right, lidBox.Bottom);
                    // Gold center lock plate
                    g.FillRectangle(lockBrush, x + w / 2 - w / 9, lidBox.Bottom - h / 10, w * 2 / 9, h / 4);
                    g.DrawRectangle(darkPen, x + w / 2 - w / 9, lidBox.Bottom - h / 10, w * 2 / 9, h / 4);
                }
            }
            else if (code == "crown") // Golden Crown 👑
            {
                Point[] pts = new Point[] {
                    new Point(x + w / 8, y + h * 4 / 5),
                    new Point(x + w / 12, y + h / 4),
                    new Point(x + w * 3 / 8, y + h * 3 / 5),
                    new Point(x + w / 2, y + h / 6),
                    new Point(x + w * 5 / 8, y + h * 3 / 5),
                    new Point(x + w * 11 / 12, y + h / 4),
                    new Point(x + w * 7 / 8, y + h * 4 / 5)
                };
                using (var b = new SolidBrush(Color.FromArgb(250, 204, 21)))
                using (var p = new Pen(Color.FromArgb(20, 15, 10), 2f))
                {
                    g.FillPolygon(b, pts);
                    g.DrawPolygon(p, pts);
                }
            }
            else if (code == "gem") // Diamond Gem 💎
            {
                Point[] pts = new Point[] {
                    new Point(x + w / 4, y + h / 5),
                    new Point(x + w * 3 / 4, y + h / 5),
                    new Point(x + w * 9 / 10, y + h * 2 / 5),
                    new Point(x + w / 2, y + h * 9 / 10),
                    new Point(x + w / 10, y + h * 2 / 5)
                };
                using (var b = new SolidBrush(Color.FromArgb(56, 189, 248)))
                using (var p = new Pen(Color.White, 2f))
                {
                    g.FillPolygon(b, pts);
                    g.DrawPolygon(p, pts);
                }
            }
            else if (code == "skull") // Skull 💀
            {
                using (var b = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (var p = new Pen(Color.FromArgb(15, 23, 42), 2.2f))
                using (var eye = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    g.FillEllipse(b, x + w / 6, y + h / 8, w * 2 / 3, h * 3 / 5);
                    g.FillRectangle(b, x + w * 3 / 10, y + h * 3 / 5, w * 2 / 5, h / 4);
                    g.DrawEllipse(p, x + w / 6, y + h / 8, w * 2 / 3, h * 3 / 5);
                    g.FillEllipse(eye, x + w * 3 / 10, y + h * 3 / 8, w / 7, h / 7);
                    g.FillEllipse(eye, x + w * 11 / 20, y + h * 3 / 8, w / 7, h / 7);
                }
            }
            else if (code == "flame") // Flame 🔥
            {
                Point[] pts = new Point[] {
                    new Point(x + w / 2, y + h / 10),
                    new Point(x + w * 4 / 5, y + h / 2),
                    new Point(x + w * 3 / 4, y + h * 4 / 5),
                    new Point(x + w / 2, y + h * 9 / 10),
                    new Point(x + w / 4, y + h * 4 / 5),
                    new Point(x + w / 5, y + h / 2)
                };
                using (var b = new LinearGradientBrush(r, Color.FromArgb(250, 204, 21), Color.FromArgb(239, 68, 68), LinearGradientMode.Vertical))
                using (var p = new Pen(Color.FromArgb(254, 240, 138), 1.8f))
                {
                    g.FillPolygon(b, pts);
                    g.DrawPolygon(p, pts);
                }
            }
            else if (code == "key") // Golden Key 🗝
            {
                using (var pen = new Pen(Color.FromArgb(250, 204, 21), Math.Max(3f, w / 11f)))
                {
                    g.DrawEllipse(pen, x + w / 8, y + h / 4, w * 2 / 5, h / 2);
                    g.DrawLine(pen, x + w * 11 / 20, y + h / 2, x + w * 9 / 10, y + h / 2);
                    g.DrawLine(pen, x + w * 3 / 4, y + h / 2, x + w * 3 / 4, y + h * 3 / 4);
                    g.DrawLine(pen, x + w * 9 / 10, y + h / 2, x + w * 9 / 10, y + h * 3 / 4);
                }
            }
            else if (code == "trophy") // Trophy 🏆
            {
                using (var b = new SolidBrush(Color.FromArgb(250, 204, 21)))
                using (var p = new Pen(Color.FromArgb(161, 98, 7), 2f))
                {
                    g.FillPie(b, x + w / 5, y + h / 6, w * 3 / 5, h * 3 / 5, 0, 180);
                    g.FillRectangle(b, x + w / 5, y + h / 5, w * 3 / 5, h / 4);
                    g.FillRectangle(b, x + w * 9 / 20, y + h / 2, w / 10, h / 4);
                    g.FillRectangle(b, x + w * 3 / 10, y + h * 3 / 4, w * 2 / 5, h / 8);
                    g.DrawRectangle(p, x + w * 3 / 10, y + h * 3 / 4, w * 2 / 5, h / 8);
                }
            }
            else // Star ⭐
            {
                using (var b = new SolidBrush(Color.FromArgb(250, 204, 21)))
                using (var p = new Pen(Color.White, 1.8f))
                {
                    DrawStar(g, p, b, x + w / 2, y + h / 2, Math.Min(w, h) * 2 / 5);
                }
            }
            g.SmoothingMode = old;
        }

        void DrawVectorDecoration(Graphics g, Theme t, Rectangle zone, int emblem)
        {
            if (emblem <= 0) return;
            int cx = zone.X + zone.Width / 2;
            int cy = zone.Y + zone.Height / 2;

            using (var pen = new Pen(t.Accent, 2.0f))
            using (var fill = new SolidBrush(Color.FromArgb(55, t.Accent)))
            {
                if (emblem == 1)
                {
                    int cw = 36, ch = 14;
                    int x0 = cx - cw / 2, y0 = zone.Y + 2;
                    Point[] pts = new Point[] {
                        new Point(x0, y0 + ch), new Point(x0 - 3, y0 + 2),
                        new Point(x0 + cw / 4, y0 + ch - 4), new Point(cx, y0 - 2),
                        new Point(x0 + (cw * 3) / 4, y0 + ch - 4), new Point(x0 + cw + 3, y0 + 2),
                        new Point(x0 + cw, y0 + ch)
                    };
                    g.FillPolygon(fill, pts); g.DrawPolygon(pen, pts);
                }
                else if (emblem == 2)
                {
                    int bw = Math.Min(zone.Width - 20, 280);
                    int bh = Math.Min(zone.Height - 16, 72);
                    int lx = cx - bw / 2, rx = cx + bw / 2, ty = cy - bh / 2, by = cy + bh / 2, arm = 16;
                    g.DrawLines(pen, new Point[] { new Point(lx + arm, ty), new Point(lx, ty), new Point(lx, by), new Point(lx + arm, by) });
                    g.DrawLines(pen, new Point[] { new Point(rx - arm, ty), new Point(rx, ty), new Point(rx, by), new Point(rx - arm, by) });
                }
                else if (emblem == 3)
                {
                    int bw = Math.Min(zone.Width - 16, 290);
                    int bh = Math.Min(zone.Height - 14, 74);
                    int lx = cx - bw / 2, rx = cx + bw / 2, ty = cy - bh / 2, by = cy + bh / 2, cut = 12;
                    Point[] hex = new Point[] {
                        new Point(lx + cut, ty), new Point(rx - cut, ty),
                        new Point(rx, ty + cut), new Point(rx, by - cut),
                        new Point(rx - cut, by), new Point(lx + cut, by),
                        new Point(lx, by - cut), new Point(lx, ty + cut)
                    };
                    g.DrawPolygon(pen, hex);
                }
                else if (emblem == 4)
                {
                    int span = 125;
                    for (int k = 0; k < 3; k++)
                    {
                        int dy = (k - 1) * 12;
                        int len = 26 - k * 5;
                        g.DrawLine(pen, cx - span, cy + dy, cx - span - len, cy + dy - 6);
                        g.DrawLine(pen, cx + span, cy + dy, cx + span + len, cy + dy - 6);
                    }
                }
                else if (emblem == 5)
                {
                    int sx = cx - 135, sy = cy - 12;
                    g.FillEllipse(fill, sx, sy, 22, 20); g.DrawEllipse(pen, sx, sy, 22, 20);
                    g.DrawRectangle(pen, sx + 5, sy + 16, 12, 7);
                }
                else if (emblem == 6)
                {
                    DrawStar(g, pen, fill, cx - 130, cy, 12);
                    DrawStar(g, pen, fill, cx + 130, cy, 12);
                }
                else if (emblem == 7)
                {
                    Point[] dL = new Point[] { new Point(cx - 130, cy - 12), new Point(cx - 120, cy), new Point(cx - 130, cy + 12), new Point(cx - 140, cy) };
                    Point[] dR = new Point[] { new Point(cx + 130, cy - 12), new Point(cx + 140, cy), new Point(cx + 130, cy + 12), new Point(cx + 120, cy) };
                    g.FillPolygon(fill, dL); g.DrawPolygon(pen, dL);
                    g.FillPolygon(fill, dR); g.DrawPolygon(pen, dR);
                }
            }
        }

        static void DrawStar(Graphics g, Pen pen, Brush fill, int cx, int cy, int r)
        {
            PointF[] pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double ang = -Math.PI / 2 + i * Math.PI / 5;
                double rad = (i % 2 == 0) ? r : r * 0.45;
                pts[i] = new PointF((float)(cx + Math.Cos(ang) * rad), (float)(cy + Math.Sin(ang) * rad));
            }
            g.FillPolygon(fill, pts);
            g.DrawPolygon(pen, pts);
        }

        void DrawSectionSparkline(Graphics g, Theme t, Rectangle r)
        {
            long[] deltas;
            lock (fileLock)
            {
                deltas = new long[sections.Count];
                for (int i = 0; i < sections.Count; i++)
                {
                    long st = (i < sectionStarts.Count) ? sectionStarts[i] : sections[i];
                    deltas[i] = Math.Max(0, sections[i] - st);
                }
            }
            if (deltas.Length < 2) return;

            long maxD = 1;
            for (int i = 0; i < deltas.Length; i++) if (deltas[i] > maxD) maxD = deltas[i];

            PointF[] pts = new PointF[deltas.Length];
            for (int i = 0; i < deltas.Length; i++)
            {
                float x = r.X + 60 + (r.Width - 66) * ((float)i / (deltas.Length - 1));
                float y = r.Bottom - 3 - (r.Height - 6) * ((float)deltas[i] / maxD);
                pts[i] = new PointF(x, y);
            }

            using (var lblBrush = new SolidBrush(t.TextMuted))
            using (var linePen = new Pen(t.Accent, 1.6f))
            {
                g.DrawString("PACE TREND", fontKeyCap, lblBrush, r.X, r.Y + 4);
                g.DrawLines(linePen, pts);
            }
        }

        void DrawSpaciousSettingsBody(Graphics g, Theme t, int bodyY)
        {
            Rectangle setRect = new Rectangle(16, bodyY - 8, Width - 32, Height - bodyY - 8);
            using (GraphicsPath sp = RoundedRect(setRect, 12))
            using (var sb = new SolidBrush(t.PanelBg))
            using (var spen = new Pen(t.AccentSoftBorder, 1f))
            {
                g.FillPath(sb, sp);
                g.DrawPath(spen, sp);
            }

            using (var lblBrush = new SolidBrush(t.TextSecondary))
            using (var accBrush = new SolidBrush(t.Accent))
            {
                if (settingsTab == 0)
                {
                    g.DrawString("Keyboard Size (" + detectedKbSummary + "):", fontSetTitle, accBrush, 24, bodyY + 76);
                    DrawVisualKeyboardAndActualMouse(g, t, bodyY + 108);
                }
                else if (settingsTab == 1)
                {
                    g.DrawString("Tip: Drag any sticker directly in the preview above! Hold Left-Click + Mouse Wheel on a sticker (or number) to resize it!", fontSetTitle, accBrush, 24, bodyY + 41);
                    g.DrawString("Centered Vector Frame / Emblem:", fontSetTitle, accBrush, 24, bodyY + 134);
                    g.DrawString("Number Text Style:", fontSetTitle, accBrush, 24, bodyY + 226);
                }
                else if (settingsTab == 2)
                {
                    g.DrawString("Select a Preset Theme (" + themes[themeIdx].Name + ") or build your own Custom Theme:", fontSetTitle, accBrush, 24, bodyY + 6);
                    for (int i = 0; i < swatchRects.Count && i < themes.Count; i++)
                    {
                        Rectangle sr = swatchRects[i];
                        using (var swBrush = new SolidBrush(themes[i].Accent))
                        using (var swPen = new Pen(i == themeIdx ? Color.White : themes[i].BgTop, i == themeIdx ? 2.5f : 1.2f))
                        {
                            g.FillEllipse(swBrush, sr);
                            g.DrawEllipse(swPen, sr);
                        }
                    }
                    g.DrawString("Opacity:", fontSetLabel, lblBrush, 24, bodyY + 196);
                }
                else if (settingsTab == 3)
                {
                    g.DrawString("Choose Counter Font Family:", fontSetTitle, accBrush, 24, bodyY + 4);
                    g.DrawString("Number Size:", fontSetLabel, lblBrush, 24, bodyY + 123);
                    g.DrawString("Outline:", fontSetLabel, lblBrush, 24, bodyY + 169);
                }
                else if (settingsTab == 4)
                {
                    g.DrawString("Step Size:", fontSetLabel, lblBrush, 24, bodyY + 110);
                    g.DrawString("Target Goal:", fontSetLabel, lblBrush, 24, bodyY + 154);
                }
            }
        }

        void DrawVisualKeyboardAndActualMouse(Graphics g, Theme t, int mapTopY)
        {
            int kbW = (int)((Width - 40) * 0.72);
            int mx = 20 + kbW + 22;
            int mw = (Width - 40) - kbW - 22;

            int bodyX = mx + 30;
            int bodyW = Math.Min(110, mw - 32);
            int bodyY = mapTopY + 4;
            int bodyH = 160;

            using (var shellPath = new GraphicsPath())
            {
                shellPath.AddArc(bodyX + 4, bodyY, bodyW - 8, 42, 180, 180);
                shellPath.AddBezier(
                    new Point(bodyX + bodyW - 4, bodyY + 21),
                    new Point(bodyX + bodyW + 3, bodyY + 68),
                    new Point(bodyX + bodyW + 5, bodyY + 110),
                    new Point(bodyX + bodyW - 4, bodyY + bodyH - 26));
                shellPath.AddArc(bodyX + 2, bodyY + bodyH - 52, bodyW - 4, 50, 0, 180);
                shellPath.AddBezier(
                    new Point(bodyX + 2, bodyY + bodyH - 26),
                    new Point(bodyX - 3, bodyY + 110),
                    new Point(bodyX + 4, bodyY + 68),
                    new Point(bodyX + 4, bodyY + 21));
                shellPath.CloseFigure();

                using (var shellBrush = new LinearGradientBrush(new Rectangle(bodyX, bodyY, bodyW, bodyH), t.BtnBg, t.RowIdleBg, LinearGradientMode.Vertical))
                using (var shellPen = new Pen(t.Border, 1.8f))
                {
                    g.FillPath(shellBrush, shellPath);
                    g.DrawPath(shellPen, shellPath);
                }
            }

            using (var seamPen = new Pen(t.Border, 1.2f))
            using (var glowPen = new Pen(Color.FromArgb(150, t.Accent), 2.2f))
            {
                int midX = bodyX + bodyW / 2;
                g.DrawLine(seamPen, midX, bodyY + 2, midX, bodyY + 64);
                g.DrawArc(seamPen, bodyX + 4, bodyY + 54, bodyW - 8, 18, 180, 180);
                g.DrawArc(glowPen, bodyX + 14, bodyY + bodyH - 34, bodyW - 28, 22, 15, 150);
            }

            using (var lblBrush = new SolidBrush(t.TextMuted))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("LMB", fontKeyCap, lblBrush, bodyX + 8, bodyY + 22);
                g.DrawString("RMB", fontKeyCap, lblBrush, bodyX + bodyW - 32, bodyY + 22);
                g.DrawString("GAMING MOUSE", fontKeyCap, lblBrush, new RectangleF(bodyX, bodyY + 98, bodyW, 18), sf);
            }

            TriggerBind activeSlotBind = GetBindSlot(selectedBindSlot);

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < visualKeys.Count; i++)
                {
                    VisualKeyNode k = visualKeys[i];
                    bool isHovered = (i == hoveredVisualKeyIndex);
                    bool isSelectedSlotMatch = (k.Kind == "Key" && activeSlotBind.MatchesKey(k.Code)) || (k.Kind == "Mouse" && activeSlotBind.MatchesMouse(k.Code));
                    bool isPhysicallyPressed = (k.Kind == "Key" && lastPhysicallyPressedVk == k.Code) || (k.Kind == "Mouse" && lastPhysicallyPressedMouse == k.Code);
                    bool isAnyBindMatch =
                        (k.Kind == "Key" && (bindIncrement.MatchesKey(k.Code) || bindSection.MatchesKey(k.Code) || bindDecrement.MatchesKey(k.Code) || bindUndo.MatchesKey(k.Code) || bindGhost.MatchesKey(k.Code) || bindMode.MatchesKey(k.Code))) ||
                        (k.Kind == "Mouse" && (bindIncrement.MatchesMouse(k.Code) || bindSection.MatchesMouse(k.Code) || bindDecrement.MatchesMouse(k.Code) || bindUndo.MatchesMouse(k.Code) || bindGhost.MatchesMouse(k.Code) || bindMode.MatchesMouse(k.Code)));

                    Color bg = t.BtnBg;
                    Color border = t.BtnBorder;
                    Color fg = t.TextSecondary;

                    if (isSelectedSlotMatch || isPhysicallyPressed)
                    {
                        bg = t.Accent;
                        border = Color.White;
                        fg = t.BgTop;
                    }
                    else if (isAnyBindMatch)
                    {
                        bg = t.AccentSoftBg;
                        border = t.Accent;
                        fg = t.Accent;
                    }
                    else if (isHovered)
                    {
                        bg = t.BtnHoverBg;
                        border = t.Accent;
                        fg = t.TextPrimary;
                    }

                    int radius = (k.Kind == "Mouse") ? 7 : 4;
                    using (GraphicsPath kp = RoundedRect(k.Bounds, radius))
                    using (var kb = new SolidBrush(bg))
                    using (var kpen = new Pen(border, isSelectedSlotMatch ? 1.8f : 1f))
                    using (var kfg = new SolidBrush(fg))
                    {
                        g.FillPath(kb, kp);
                        g.DrawPath(kpen, kp);
                        g.DrawString(k.Label, fontKeyCap, kfg, k.Bounds, sf);
                    }
                }
            }
        }

        void DrawHudPillMode(Graphics g, Theme t, string displayNum, int secCount, long secDelta)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var numBrush = new SolidBrush(t.TextPrimary))
            {
                g.DrawString(displayNum, fontHeroHud, numBrush, new RectangleF(0, 14, Width, Height - 14), sf);
            }
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            string sub = "SEC " + secCount + " (" + (secDelta >= 0 ? "+" : "") + secDelta.ToString("N0") + ")";
            using (var subBrush = new SolidBrush(t.Accent))
            {
                g.DrawString(sub, fontHeader, subBrush, 14, 10);
            }
        }

        void DrawPillBadge(Graphics g, Rectangle r, string text, Color bg, Color border, Color fg)
        {
            using (GraphicsPath p = RoundedRect(r, 6))
            using (var b = new SolidBrush(bg))
            using (var pen = new Pen(border, 1f))
            using (var tb = new SolidBrush(fg))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillPath(b, p);
                g.DrawPath(pen, p);
                g.DrawString(text, fontBadge, tb, r, sf);
            }
        }

        void DrawSectionsList(Graphics g, Theme t, Rectangle clipRect)
        {
            int count;
            long bestDelta = 0;
            lock (fileLock)
            {
                count = sections.Count;
                for (int i = 0; i < count; i++)
                {
                    long st = (i < sectionStarts.Count) ? sectionStarts[i] : sections[i];
                    long d = sections[i] - st;
                    if (d > bestDelta) bestDelta = d;
                }
            }

            int contentH = count * 32 + Math.Max(0, count - 1) * 20;
            maxScroll = Math.Max(0, contentH - clipRect.Height);
            if (scrollOffset > maxScroll) scrollOffset = maxScroll;
            if (scrollOffset < 0) scrollOffset = 0;

            Region oldClip = g.Clip;
            g.SetClip(clipRect);

            int y = clipRect.Y - scrollOffset;
            int rowW = clipRect.Width - (maxScroll > 0 ? 8 : 0);

            using (var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            using (var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < count; i++)
                {
                    long val, secStart;
                    lock (fileLock)
                    {
                        val = sections[i];
                        secStart = (i < sectionStarts.Count) ? sectionStarts[i] : val;
                    }

                    if (i > 0)
                    {
                        if (y + 20 >= clipRect.Top && y <= clipRect.Bottom)
                        {
                            string sepText = "SECTION " + (i + 1);
                            SizeF st = g.MeasureString(sepText, fontSecDivider);
                            int midX = clipRect.X + rowW / 2;
                            int halfTxt = (int)(st.Width / 2) + 8;
                            int lineY = y + 10;

                            using (var linePen = new Pen(t.Border, 1f))
                            using (var sepBrush = new SolidBrush(t.TextMuted))
                            {
                                if (midX - halfTxt > clipRect.X + 6)
                                    g.DrawLine(linePen, clipRect.X + 6, lineY, midX - halfTxt, lineY);
                                if (midX + halfTxt < clipRect.X + rowW - 6)
                                    g.DrawLine(linePen, midX + halfTxt, lineY, clipRect.X + rowW - 6, lineY);
                                g.DrawString(sepText, fontSecDivider, sepBrush, new RectangleF(clipRect.X, y, rowW, 20), sfCenter);
                            }
                        }
                        y += 20;
                    }

                    if (y + 30 >= clipRect.Top && y <= clipRect.Bottom)
                    {
                        bool isLatest = (i == count - 1);
                        long sDelta = val - secStart;
                        bool isPb = (count > 1 && bestDelta > 0 && sDelta == bestDelta);

                        Rectangle rowRect = new Rectangle(clipRect.X + 2, y, rowW - 4, 28);
                        using (GraphicsPath rp = RoundedRect(rowRect, 7))
                        using (var rb = new SolidBrush(isLatest ? t.RowActiveBg : t.RowIdleBg))
                        using (var rpen = new Pen(isLatest ? t.RowActiveBorder : t.RowIdleBorder, 1f))
                        {
                            g.FillPath(rb, rp);
                            g.DrawPath(rpen, rp);
                        }

                        Rectangle textPad = new Rectangle(rowRect.X + 10, rowRect.Y, rowRect.Width - 20, rowRect.Height);
                        using (var tagBrush = new SolidBrush(isLatest ? t.Accent : t.TextMuted))
                        using (var valBrush = new SolidBrush(isLatest ? t.TextPrimary : t.TextSecondary))
                        {
                            string tagStr = "#" + (i + 1) + (isPb ? " ★PB" : "");
                            g.DrawString(tagStr, fontRowTag, tagBrush, textPad, sfLeft);
                            g.DrawString(val.ToString("N0"), isLatest ? fontRowActive : fontRowIdle, valBrush, rowRect, sfCenter);
                            string dStr = (i == 0 && sDelta == 0) ? "start" : ((sDelta >= 0 ? "+" : "") + sDelta.ToString("N0"));
                            g.DrawString(dStr, fontRowTag, tagBrush, textPad, sfRight);
                        }
                    }
                    y += 32;
                }
            }

            if (maxScroll > 0)
            {
                float ratio = (float)clipRect.Height / contentH;
                int thumbH = Math.Max(18, (int)(clipRect.Height * ratio));
                int thumbY = clipRect.Y + (int)((clipRect.Height - thumbH) * ((float)scrollOffset / maxScroll));
                using (var sbBrush = new SolidBrush(t.Border))
                {
                    g.FillRectangle(sbBrush, clipRect.Right - 4, thumbY, 3, thumbH);
                }
            }

            g.Clip = oldClip;
        }

        void DrawButtons(Graphics g, Theme t)
        {
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    UiButton b = buttons[i];
                    bool hov = (i == hoveredBtnIndex);

                    Color bg, border, fg;
                    if (b.IsClose)
                    {
                        bg = hov ? Color.FromArgb(239, 68, 68) : Color.FromArgb(65, 22, 32);
                        border = hov ? Color.FromArgb(248, 113, 113) : Color.FromArgb(115, 36, 52);
                        fg = hov ? Color.White : Color.FromArgb(252, 165, 165);
                    }
                    else if (b.IsActive)
                    {
                        bg = hov ? t.BtnHoverBg : t.AccentSoftBg;
                        border = t.Accent;
                        fg = t.Accent;
                    }
                    else
                    {
                        bg = hov ? t.BtnHoverBg : t.BtnBg;
                        border = hov ? t.AccentSoftBorder : t.BtnBorder;
                        fg = b.IsAccent ? t.Accent : t.TextSecondary;
                    }

                    using (GraphicsPath bp = RoundedRect(b.Bounds, 6))
                    using (var bBrush = new SolidBrush(bg))
                    using (var bPen = new Pen(border, 1f))
                    using (var fBrush = new SolidBrush(fg))
                    {
                        g.FillPath(bBrush, bp);
                        g.DrawPath(bPen, bp);
                        g.DrawString(b.Label, fontBtn, fBrush, b.Bounds, sf);
                    }
                }
            }
        }

        void DrawOuterBorder(Graphics g, Theme t)
        {
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = RoundedRect(r, 14))
            using (var pen = new Pen(isGhostMode ? Color.FromArgb(150, 251, 191, 36) : t.Border, 1.5f))
            {
                g.DrawPath(pen, p);
            }
        }

        static GraphicsPath RoundedRect(Rectangle b, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0) { path.AddRectangle(b); return path; }
            path.AddArc(b.X, b.Y, d, d, 180, 90);
            path.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            path.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            path.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Detects if cursor is in bottom-right or bottom-left corner resize zone
        int HitTestCornerResize(Point pt)
        {
            int grip = 18;
            if (pt.X >= Width - grip && pt.Y >= Height - grip) return 1; // Bottom-Right
            if (pt.X <= grip && pt.Y >= Height - grip) return 2;        // Bottom-Left
            return 0;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isMouseInsideWindow = true;
            if (viewMode == 3) RebuildButtons();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Right-clicking a sticker deletes that sticker!
            if (e.Button == MouseButtons.Right)
            {
                for (int i = stickers.Count - 1; i >= 0; i--)
                {
                    if (stickers[i].LastDrawnBounds.Contains(e.Location))
                    {
                        stickers[i].DisposeCache();
                        stickers.RemoveAt(i);
                        hoveredStickerIndex = -1;
                        SaveSettings();
                        RebuildButtons();
                        Invalidate();
                        return;
                    }
                }
                return;
            }

            if (e.Button != MouseButtons.Left) return;

            // 1. Check UI buttons
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].Bounds.Contains(e.Location))
                {
                    buttons[i].OnClick();
                    return;
                }
            }

            // 2. Check Corner Resize Grips
            int rDir = HitTestCornerResize(e.Location);
            if (rDir != 0)
            {
                isResizingWindow = true;
                resizeCornerDir = rDir;
                dragCursorStart = Cursor.Position;
                dragWindowStartBounds = this.Bounds;
                return;
            }

            // 3. Check Draggable Stickers (click & drag to reposition, or hold-click + wheel to scale!)
            for (int i = stickers.Count - 1; i >= 0; i--)
            {
                if (stickers[i].LastDrawnBounds.Contains(e.Location))
                {
                    draggingStickerIndex = i;
                    stickerDragStartMouse = e.Location;
                    stickerDragStartOffset = new Point(stickers[i].OffsetX, stickers[i].OffsetY);
                    Invalidate();
                    return;
                }
            }

            // 4. Visual Keyboard & Mouse Picker nodes
            if (isSettingsOpen && settingsTab == 0)
            {
                for (int i = 0; i < visualKeys.Count; i++)
                {
                    if (visualKeys[i].Bounds.Contains(e.Location))
                    {
                        AssignBindToSlot(selectedBindSlot, new TriggerBind(visualKeys[i].Kind, visualKeys[i].Code));
                        isListeningForKey = false;
                        RebuildButtons();
                        Invalidate();
                        return;
                    }
                }
            }

            // 5. Theme Swatches
            if (isSettingsOpen && settingsTab == 2)
            {
                for (int i = 0; i < swatchRects.Count && i < themes.Count; i++)
                {
                    if (swatchRects[i].Contains(e.Location))
                    {
                        themeIdx = i;
                        SaveSettings();
                        Invalidate();
                        return;
                    }
                }
            }

            // 6. Start Window Drag (or Hold Left-Click + Mouse Wheel to scale Number Size!)
            isMouseDownOnWindow = true;
            isDraggingWindow = false;
            dragCursorStart = Cursor.Position;
            dragWindowStartBounds = this.Bounds;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Dragging a Sticker / Custom Image
            if (draggingStickerIndex >= 0 && draggingStickerIndex < stickers.Count)
            {
                OverlaySticker st = stickers[draggingStickerIndex];
                st.OffsetX = stickerDragStartOffset.X + (e.X - stickerDragStartMouse.X);
                st.OffsetY = stickerDragStartOffset.Y + (e.Y - stickerDragStartMouse.Y);
                Cursor = Cursors.SizeAll;
                Invalidate();
                return;
            }

            // Resizing Window from Corner Handle
            if (isResizingWindow)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - dragCursorStart.X;
                int dy = cur.Y - dragCursorStart.Y;
                if (resizeCornerDir == 1) // Bottom-Right
                {
                    int nw = Math.Max(MinimumSize.Width, dragWindowStartBounds.Width + dx);
                    int nh = Math.Max(MinimumSize.Height, dragWindowStartBounds.Height + dy);
                    this.Size = new Size(nw, nh);
                }
                else if (resizeCornerDir == 2) // Bottom-Left
                {
                    int nw = Math.Max(MinimumSize.Width, dragWindowStartBounds.Width - dx);
                    int nh = Math.Max(MinimumSize.Height, dragWindowStartBounds.Height + dy);
                    int nx = dragWindowStartBounds.Right - nw;
                    this.Bounds = new Rectangle(nx, dragWindowStartBounds.Y, nw, nh);
                }
                if (!isSettingsOpen)
                {
                    if (viewMode == 3) customFloatSize = this.Size;
                    else if (viewMode == 0) customFullSize = this.Size;
                }
                return;
            }

            // Dragging Window from Anywhere
            if (isMouseDownOnWindow)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - dragCursorStart.X;
                int dy = cur.Y - dragCursorStart.Y;
                if (isDraggingWindow || Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                {
                    isDraggingWindow = true;
                    Cursor = Cursors.SizeAll;
                    SetWindowPos(this.Handle, IntPtr.Zero, dragWindowStartBounds.X + dx, dragWindowStartBounds.Y + dy, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | 0x0004);
                }
                return;
            }

            // Hover detection
            int rCorner = HitTestCornerResize(e.Location);
            if (rCorner == 1) { Cursor = Cursors.SizeNWSE; return; }
            if (rCorner == 2) { Cursor = Cursors.SizeNESW; return; }

            int foundBtn = -1;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].Bounds.Contains(e.Location)) { foundBtn = i; break; }
            }

            int foundSticker = -1;
            if (foundBtn == -1)
            {
                for (int i = stickers.Count - 1; i >= 0; i--)
                {
                    if (stickers[i].LastDrawnBounds.Contains(e.Location)) { foundSticker = i; break; }
                }
            }

            int foundKey = -1;
            if (foundBtn == -1 && foundSticker == -1 && isSettingsOpen && settingsTab == 0)
            {
                for (int i = 0; i < visualKeys.Count; i++)
                {
                    if (visualKeys[i].Bounds.Contains(e.Location)) { foundKey = i; break; }
                }
            }

            if (foundBtn != hoveredBtnIndex || foundKey != hoveredVisualKeyIndex || foundSticker != hoveredStickerIndex)
            {
                hoveredBtnIndex = foundBtn;
                hoveredVisualKeyIndex = foundKey;
                hoveredStickerIndex = foundSticker;
                if (foundBtn >= 0 || foundKey >= 0) Cursor = Cursors.Hand;
                else if (foundSticker >= 0) Cursor = Cursors.SizeAll;
                else Cursor = Cursors.Default;

                if (foundBtn >= 0) toolTip.SetToolTip(this, buttons[foundBtn].Tooltip);
                else if (foundSticker >= 0) toolTip.SetToolTip(this, "Drag to move • Hold Left-Click + Scroll Wheel to resize • Right-Click to remove");
                else if (foundKey >= 0) toolTip.SetToolTip(this, "Click to bind " + visualKeys[foundKey].Label);
                else toolTip.SetToolTip(this, null);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (draggingStickerIndex != -1)
            {
                draggingStickerIndex = -1;
                Cursor = Cursors.Default;
                SaveSettings();
                Invalidate();
            }
            if (isResizingWindow)
            {
                isResizingWindow = false;
                Cursor = Cursors.Default;
                SaveSettings();
                Invalidate();
            }
            if (isMouseDownOnWindow)
            {
                bool wasDrag = isDraggingWindow;
                isMouseDownOnWindow = false;
                isDraggingWindow = false;
                Cursor = Cursors.Default;
                if (wasDrag) SaveSettings();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (viewMode == 3 || viewMode == 2)
            {
                SetViewMode(0);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                isMouseInsideWindow = false;
                if (viewMode == 3) RebuildButtons();
            }
            hoveredBtnIndex = -1;
            hoveredVisualKeyIndex = -1;
            hoveredStickerIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        // Hold Left-Click + Mouse Wheel adjusts Sticker Size or Number Font Size on the fly!
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // 1. If holding Left-Click on a Sticker (or hovering a sticker while holding click), scale that sticker!
            int targetSticker = (draggingStickerIndex >= 0) ? draggingStickerIndex : hoveredStickerIndex;
            if (targetSticker >= 0 && targetSticker < stickers.Count && ((MouseButtons & MouseButtons.Left) != 0 || draggingStickerIndex >= 0))
            {
                OverlaySticker st = stickers[targetSticker];
                st.SizePx = Math.Max(18, Math.Min(280, st.SizePx + (e.Delta > 0 ? 6 : -6)));
                st.EnsureCache();
                SaveSettings();
                Invalidate();
                return;
            }

            // 2. If holding Left-Click anywhere on the window/number, scale the Number Font Size live!
            if ((MouseButtons & MouseButtons.Left) != 0 || isMouseDownOnWindow)
            {
                heroFontSizePt = Math.Max(16, Math.Min(130, heroFontSizePt + (e.Delta > 0 ? 3 : -3)));
                RebuildFonts();
                EnsureTextFitsInWindow();
                SaveSettings();
                RebuildButtons();
                Invalidate();
                return;
            }

            // 3. Scrolling over a hovered sticker even without holding click scales the sticker!
            if (hoveredStickerIndex >= 0 && hoveredStickerIndex < stickers.Count)
            {
                OverlaySticker st = stickers[hoveredStickerIndex];
                st.SizePx = Math.Max(18, Math.Min(280, st.SizePx + (e.Delta > 0 ? 6 : -6)));
                st.EnsureCache();
                SaveSettings();
                Invalidate();
                return;
            }

            // 4. Scrolling over the top header bar adjusts window opacity
            if (e.Y <= 42 && viewMode != 3)
            {
                double step = (e.Delta > 0) ? 0.05 : -0.05;
                userOpacity = Math.Max(0.25, Math.Min(1.0, userOpacity + step));
                UpdateDwmAndRegion();
                SaveSettings();
                RebuildButtons();
                Invalidate();
                return;
            }

            // 5. Otherwise scroll sections list
            if (viewMode == 0 && !isSettingsOpen && maxScroll > 0)
            {
                scrollOffset -= (e.Delta > 0 ? 30 : -30);
                if (scrollOffset < 0) scrollOffset = 0;
                if (scrollOffset > maxScroll) scrollOffset = maxScroll;
                Invalidate();
            }
        }

        void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return;
                string json = File.ReadAllText(settingsPath);
                int t = ExtractInt(json, "themeIndex", 0);
                if (t >= 0 && t < themes.Count) themeIdx = t;

                viewMode = ExtractInt(json, "viewMode", 0);
                if (viewMode < 0 || viewMode > 3) viewMode = 0;

                int opPct = ExtractInt(json, "opacityPercent", 96);
                if (opPct >= 25 && opPct <= 100) userOpacity = opPct / 100.0;

                soundEnabled = ExtractInt(json, "soundEnabled", 0) == 1;
                isGhostMode = ExtractInt(json, "ghostMode", viewMode == 3 ? 1 : 0) == 1;
                autoStretchRes = ExtractInt(json, "autoStretchRes", 0) == 1;
                autoSessionSection = ExtractInt(json, "autoSessionSection", 0) == 1;
                gameOverlayNoActivate = ExtractInt(json, "gameOverlayNoActivate", 1) == 1;
                stepAmount = Math.Max(1, ExtractInt(json, "stepAmount", 1));
                targetGoal = Math.Max(10, ExtractInt(json, "targetGoal", 2500));
                showGoalBar = ExtractInt(json, "showGoalBar", 1) == 1;
                showBadges = ExtractInt(json, "showBadges", 1) == 1;
                showSparkline = ExtractInt(json, "showSparkline", 1) == 1;
                fontFamilyIdx = Math.Max(0, ExtractInt(json, "fontFamilyIdx", 0)) % fontFamilies.Length;
                heroFontSizePt = Math.Max(16, Math.Min(130, ExtractInt(json, "heroFontSizePt", 48)));
                outlineThickness = Math.Max(0, Math.Min(8, ExtractInt(json, "outlineThickness", 3)));

                int savedKb = ExtractInt(json, "kbSizeMode", -1);
                if (savedKb >= 0 && savedKb <= 4) kbSizeMode = savedKb;

                vectorEmblem = Math.Max(0, Math.Min(7, ExtractInt(json, "vectorEmblem", 0)));
                numberEffectStyle = Math.Max(0, Math.Min(4, ExtractInt(json, "numberEffectStyle", 3)));
                gradColor1 = Theme.C(ExtractString(json, "gradColor1", "#00F5D4"));
                gradColor2 = Theme.C(ExtractString(json, "gradColor2", "#7B2CBF"));
                gradDirection = Math.Max(0, Math.Min(3, ExtractInt(json, "gradDirection", 0)));
                useCustomGradColors = ExtractInt(json, "useCustomGradColors", 0) == 1;
                numberPrefix = ExtractString(json, "numberPrefix", "");
                numberSuffix = ExtractString(json, "numberSuffix", "");

                int fw = ExtractInt(json, "floatW", 420);
                int fh = ExtractInt(json, "floatH", 140);
                customFloatSize = new Size(Math.Max(160, fw), Math.Max(56, fh));

                // Parse serialized stickers ("source|ox|oy|sz;...")
                string stickerData = ExtractString(json, "stickersData", "");
                stickers.Clear();
                if (!string.IsNullOrEmpty(stickerData))
                {
                    string[] items = stickerData.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string it in items)
                    {
                        string[] p = it.Split('|');
                        if (p.Length >= 4)
                        {
                            int ox, oy, sz;
                            if (int.TryParse(p[1], out ox) && int.TryParse(p[2], out oy) && int.TryParse(p[3], out sz))
                                stickers.Add(new OverlaySticker { Source = p[0], OffsetX = ox, OffsetY = oy, SizePx = sz });
                        }
                    }
                }
                else
                {
                    string legacyImg = ExtractString(json, "customImagePath", "");
                    if (!string.IsNullOrEmpty(legacyImg))
                        stickers.Add(new OverlaySticker { Source = legacyImg, OffsetX = -110, OffsetY = 0, SizePx = ExtractInt(json, "imageSizePx", 56) });
                }

                customBg = Theme.C(ExtractString(json, "customBg", "#02072D"));
                customAccent = Theme.C(ExtractString(json, "customAccent", "#F40000"));
                customText = Theme.C(ExtractString(json, "customText", "#FFFFFF"));
                themes[themes.Count - 1] = Theme.FromCustomColors(customBg, customAccent, customText);

                bindIncrement = new TriggerBind(ExtractString(json, "incKind", "Mouse"), ExtractInt(json, "incCode", 2));
                bindSection = new TriggerBind(ExtractString(json, "secKind", "Key"), ExtractInt(json, "secCode", 0x78));
                bindDecrement = new TriggerBind(ExtractString(json, "decKind", "None"), ExtractInt(json, "decCode", 0));
                bindUndo = new TriggerBind(ExtractString(json, "undoKind", "None"), ExtractInt(json, "undoCode", 0));
                bindGhost = new TriggerBind(ExtractString(json, "ghostKind", "Key"), ExtractInt(json, "ghostCode", 0x77));
                bindMode = new TriggerBind(ExtractString(json, "modeKind", "Key"), ExtractInt(json, "modeCode", 0x76));

                int savedX = ExtractInt(json, "winX", -99999);
                int savedY = ExtractInt(json, "winY", -99999);
                if (savedX != -99999 && savedY != -99999)
                {
                    Rectangle screen = Screen.FromPoint(new Point(savedX, savedY)).WorkingArea;
                    if (savedX >= screen.Left - 100 && savedX < screen.Right - 60 && savedY >= screen.Top - 20 && savedY < screen.Bottom - 40)
                    {
                        StartPosition = FormStartPosition.Manual;
                        Location = new Point(savedX, savedY);
                    }
                }
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                var stSb = new StringBuilder();
                for (int i = 0; i < stickers.Count; i++)
                {
                    if (i > 0) stSb.Append(";");
                    stSb.Append(stickers[i].Source.Replace("|", "").Replace(";", "")).Append("|")
                        .Append(stickers[i].OffsetX).Append("|")
                        .Append(stickers[i].OffsetY).Append("|")
                        .Append(stickers[i].SizePx);
                }

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"themeIndex\": " + themeIdx + ",");
                sb.AppendLine("  \"themeName\": \"" + themes[themeIdx % themes.Count].Name + "\",");
                sb.AppendLine("  \"viewMode\": " + viewMode + ",");
                sb.AppendLine("  \"opacityPercent\": " + (int)Math.Round(userOpacity * 100) + ",");
                sb.AppendLine("  \"soundEnabled\": " + (soundEnabled ? 1 : 0) + ",");
                sb.AppendLine("  \"ghostMode\": " + (isGhostMode ? 1 : 0) + ",");
                sb.AppendLine("  \"autoStretchRes\": " + (autoStretchRes ? 1 : 0) + ",");
                sb.AppendLine("  \"autoSessionSection\": " + (autoSessionSection ? 1 : 0) + ",");
                sb.AppendLine("  \"gameOverlayNoActivate\": " + (gameOverlayNoActivate ? 1 : 0) + ",");
                sb.AppendLine("  \"stepAmount\": " + stepAmount + ",");
                sb.AppendLine("  \"targetGoal\": " + targetGoal + ",");
                sb.AppendLine("  \"showGoalBar\": " + (showGoalBar ? 1 : 0) + ",");
                sb.AppendLine("  \"showBadges\": " + (showBadges ? 1 : 0) + ",");
                sb.AppendLine("  \"showSparkline\": " + (showSparkline ? 1 : 0) + ",");
                sb.AppendLine("  \"fontFamilyIdx\": " + fontFamilyIdx + ",");
                sb.AppendLine("  \"heroFontSizePt\": " + heroFontSizePt + ",");
                sb.AppendLine("  \"outlineThickness\": " + outlineThickness + ",");
                sb.AppendLine("  \"kbSizeMode\": " + kbSizeMode + ",");
                sb.AppendLine("  \"stickersData\": \"" + stSb.ToString().Replace("\\", "\\\\") + "\",");
                sb.AppendLine("  \"vectorEmblem\": " + vectorEmblem + ",");
                sb.AppendLine("  \"numberEffectStyle\": " + numberEffectStyle + ",");
                sb.AppendLine("  \"gradColor1\": \"" + Theme.ToHex(gradColor1) + "\",");
                sb.AppendLine("  \"gradColor2\": \"" + Theme.ToHex(gradColor2) + "\",");
                sb.AppendLine("  \"gradDirection\": " + gradDirection + ",");
                sb.AppendLine("  \"useCustomGradColors\": " + (useCustomGradColors ? 1 : 0) + ",");
                sb.AppendLine("  \"numberPrefix\": \"" + (numberPrefix ?? "").Replace("\"", "") + "\",");
                sb.AppendLine("  \"numberSuffix\": \"" + (numberSuffix ?? "").Replace("\"", "") + "\",");
                sb.AppendLine("  \"floatW\": " + customFloatSize.Width + ",");
                sb.AppendLine("  \"floatH\": " + customFloatSize.Height + ",");
                sb.AppendLine("  \"customBg\": \"" + Theme.ToHex(customBg) + "\",");
                sb.AppendLine("  \"customAccent\": \"" + Theme.ToHex(customAccent) + "\",");
                sb.AppendLine("  \"customText\": \"" + Theme.ToHex(customText) + "\",");
                sb.AppendLine("  \"incKind\": \"" + bindIncrement.Kind + "\",");
                sb.AppendLine("  \"incCode\": " + bindIncrement.Code + ",");
                sb.AppendLine("  \"secKind\": \"" + bindSection.Kind + "\",");
                sb.AppendLine("  \"secCode\": " + bindSection.Code + ",");
                sb.AppendLine("  \"decKind\": \"" + bindDecrement.Kind + "\",");
                sb.AppendLine("  \"decCode\": " + bindDecrement.Code + ",");
                sb.AppendLine("  \"undoKind\": \"" + bindUndo.Kind + "\",");
                sb.AppendLine("  \"undoCode\": " + bindUndo.Code + ",");
                sb.AppendLine("  \"ghostKind\": \"" + bindGhost.Kind + "\",");
                sb.AppendLine("  \"ghostCode\": " + bindGhost.Code + ",");
                sb.AppendLine("  \"modeKind\": \"" + bindMode.Kind + "\",");
                sb.AppendLine("  \"modeCode\": " + bindMode.Code + ",");
                sb.AppendLine("  \"winX\": " + Location.X + ",");
                sb.AppendLine("  \"winY\": " + Location.Y);
                sb.AppendLine("}");
                File.WriteAllText(settingsPath, sb.ToString());
            }
            catch { }
        }

        internal static int ExtractInt(string json, string key, int fallback)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            int val;
            return (m.Success && int.TryParse(m.Groups[1].Value, out val)) ? val : fallback;
        }

        static string ExtractString(string json, string key, string fallback)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value.Replace("\\\\", "\\") : fallback;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            SaveToFileSync(); // guaranteed flush of the latest count before exit
            if (gameExitTimer != null) { gameExitTimer.Stop(); gameExitTimer.Dispose(); gameExitTimer = null; }
            try { Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch { }
            if (topmostTimer != null) { topmostTimer.Stop(); topmostTimer.Dispose(); topmostTimer = null; }
            if (fgEventHook != IntPtr.Zero) { UnhookWinEvent(fgEventHook); fgEventHook = IntPtr.Zero; }
            if (minEventHook != IntPtr.Zero) { UnhookWinEvent(minEventHook); minEventHook = IntPtr.Zero; }
            if (watcher != null) { watcher.EnableRaisingEvents = false; watcher.Dispose(); watcher = null; }
            StopHookThread();
            if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); trayIcon = null; }
            if (tickPlayer != null) { try { tickPlayer.Dispose(); } catch { } tickPlayer = null; }
            for (int i = 0; i < stickers.Count; i++) stickers[i].DisposeCache();
            base.OnFormClosing(e);
        }

        static IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            uint msg = (uint)wParam;
            if ((msg == WM_XBUTTONDOWN || msg == WM_MBUTTONDOWN) && nCode >= 0)
            {
                int code = (msg == WM_MBUTTONDOWN) ? 3 : ((Marshal.ReadInt32(lParam, 8) >> 16) & 0xFFFF);
                if (code >= 1 && code <= 3)
                    PostMessage(SharedHwnd, WM_APP_MOUSE, (IntPtr)code, IntPtr.Zero);
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        static IntPtr KeyCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                uint msg = (uint)wParam;
                int vk = Marshal.ReadInt32(lParam) & 0xFF;
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (!keysDown[vk])
                    {
                        keysDown[vk] = true;
                        PostMessage(SharedHwnd, WM_APP_KEY, (IntPtr)vk, IntPtr.Zero);
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    keysDown[vk] = false;
                }
            }
            return CallNextHookEx(keyHook, nCode, wParam, lParam);
        }

        static List<Theme> BuildThemes()
        {
            return new List<Theme>
            {
                new Theme("Obsidian Cyan",   "#0C111D","#131C2E","#273652","#080C14","#1B263B","#F8FAFC","#94A3B8","#64748B","#38BDF8","#13283E","#23496E","#162236","#2C466E","#0E1422","#192236","#172133","#273754","#22314C"),
                new Theme("Emerald Matrix",  "#091410","#0F221B","#1E4235","#060E0B","#163026","#ECFDF5","#86EFAC","#4ADE80","#10B981","#0E2E23","#1B5943","#113025","#235E48","#0A1A14","#142E24","#112920","#1E4637","#193B2E"),
                new Theme("Crimson Ember",   "#160B0F","#241118","#4C2231","#0F070A","#361823","#FFF1F2","#FDA4AF","#FB7185","#F43F5E","#3B1522","#6E253D","#33141F","#66263C","#1A0B10","#2E141D","#2B121B","#4A202F","#3D1A27"),
                new Theme("Violet Nebula",   "#110C1D","#1B1330","#3B2866","#0B0714","#281B47","#FAF5FF","#D8B4FE","#A855F7","#C084FC","#2A184A","#4E2D87","#261742","#4C2E85","#140C24","#241640","#211438","#3A2361","#2E1C4F"),
                new Theme("Amber Gold",      "#161209","#241D0E","#4D3D1E","#0F0C05","#362A14","#FFFBEB","#FDE68A","#F59E0B","#FBBF24","#382B10","#664E1D","#30250E","#614A1C","#1A1408","#2E2410","#29200D","#473818","#3B2E13"),
                new Theme("Sakura Neon",     "#170B16","#261224","#52244D","#10070F","#381835","#FDF2F8","#F9A8D4","#EC4899","#F472B6","#3D1739","#702968","#361532","#6B2963","#1C0B1A","#30142D","#2E122B","#4F214A","#421B3D"),
                new Theme("OLED Pitch Black","#000000","#040406","#222630","#000000","#161920","#FFFFFF","#A1A1AA","#71717A","#2DD4BF","#0D2624","#194D48","#0D171A","#1F3A40","#050507","#14161C","#0F1117","#242936","#1A1E29"),
                new Theme("Cyberpunk 2077",  "#0E1017","#181A24","#4D4414","#090A0F","#332E10","#FEF9C3","#FDE047","#EAB308","#FACC15","#36300E","#665B19","#2B270E","#595019","#12141C","#242633","#212014","#423D1B","#332F16"),
                new Theme("Blood Moon",      "#120404","#1F0707","#4A1212","#0A0202","#300A0A","#FEF2F2","#FCA5A5","#EF4444","#DC2626","#380D0D","#661717","#2E0B0B","#591414","#140505","#260A0A","#240909","#421111","#360E0E"),
                new Theme("Nordic Frost",    "#2E3440","#3B4252","#4C566A","#242933","#434C5E","#ECEFF4","#D8DEE9","#81A1C1","#88C0D0","#334152","#4C637A","#354052","#52647D","#292E39","#3B4252","#353C4A","#4C566A","#434C5E"),
                new Theme("Dracula",         "#282A36","#1E1F29","#44475A","#191A22","#343746","#F8F8F2","#BD93F9","#6272A4","#FF79C6","#38243C","#633B69","#312942","#564575","#21222C","#343746","#2C2E3B","#44475A","#383A4A"),
                new Theme("Monokai Pro",     "#2D2A2E","#221F22","#49454B","#19181A","#383539","#FCFCFA","#FFD866","#939293","#A9DC76","#2C3626","#4B5E3E","#33302A","#57513D","#221F22","#363337","#333034","#49454B","#403C41"),
                new Theme("Synthwave Sunset","#1A1025","#29163B","#542B75","#110A19","#3B1E54","#FFF5F5","#FEB2B2","#F687B3","#FF6B6B","#421C2E","#752F4E","#381936","#692B63","#1D1128","#331C47","#2B173D","#4A2869","#3B2054"),
                new Theme("Stealth Tactical","#111315","#181B1E","#2E3438","#0B0C0E","#222629","#F4F4F5","#A1A1AA","#71717A","#84CC16","#1F2E12","#38541F","#1C2416","#354726","#131618","#222629","#1A1D20","#2E3438","#24292D"),
                new Theme("Catppuccin Mocha","#1E1E2E","#181825","#313244","#11111B","#313244","#CDD6F4","#BAC2DE","#7F849C","#B4BEFE","#282A40","#45476B","#26273B","#45476B","#181825","#29293D","#262639","#3B3C54","#313244"),
                new Theme("Tokyo Night",     "#1A1B26","#16161E","#292E42","#13141C","#24283B","#C0CAF5","#A9B1D6","#565F89","#7AA2F7","#1F2A48","#364A82","#202840","#3B4C7E","#161722","#232738","#1F2335","#292E42","#282E44"),
                new Theme("Solarized Dark",  "#002B36","#073642","#2A4F5A","#001E26","#1B3F4A","#FDF6E3","#93A1A1","#657B83","#2AA198","#0A3B3E","#186366","#0B3840","#1D5E69","#042933","#143A45","#093642","#214B57","#13424F"),
                new Theme("Arctic Daylight", "#F8FAFC","#E2E8F0","#CBD5E1","#FFFFFF","#CBD5E1","#0F172A","#475569","#64748B","#2563EB","#DBEAFE","#93C5FD","#EFF6FF","#93C5FD","#F8FAFC","#E2E8F0","#E2E8F0","#CBD5E1","#CBD5E1"),
                Theme.FromCustomColors(ColorTranslator.FromHtml("#02072D"), ColorTranslator.FromHtml("#F40000"), ColorTranslator.FromHtml("#FFFFFF"))
            };
        }
    }

    // Zero-allocation Fortnite process detector. Uses a Toolhelp snapshot only — it NEVER opens a handle
    // to any game process, so EasyAntiCheat sees nothing.
    public static unsafe class GameWatch
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct PROCESSENTRY32W
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            public fixed char szExeFile[260];
        }

        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
        [DllImport("kernel32.dll", EntryPoint = "Process32FirstW")] static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32W lppe);
        [DllImport("kernel32.dll", EntryPoint = "Process32NextW")] static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32W lppe);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr hObject);

        static readonly string[] Names = new string[] {
            "FortniteClient-Win64-Shipping.exe",
            "FortniteClient-Win64-Shipping_EAC_EOS.exe",
            "FortniteClient-Win64-Shipping_EAC.exe",
            "FortniteClient-Win64-Shipping_BE.exe",
            "FortniteLauncher.exe"
        };

        static bool EqualsIgnoreCase(char* p, string s)
        {
            int i = 0;
            for (; i < s.Length; i++)
            {
                char a = p[i];
                if (a == '\0') return false;
                char b = s[i];
                if (a >= 'A' && a <= 'Z') a = (char)(a + 32);
                if (b >= 'A' && b <= 'Z') b = (char)(b + 32);
                if (a != b) return false;
            }
            return p[i] == '\0';
        }

        public static bool IsFortniteRunning()
        {
            IntPtr snap = CreateToolhelp32Snapshot(0x00000002 /* TH32CS_SNAPPROCESS */, 0);
            if (snap == IntPtr.Zero || snap == new IntPtr(-1)) return false;
            try
            {
                PROCESSENTRY32W pe = new PROCESSENTRY32W();
                pe.dwSize = (uint)sizeof(PROCESSENTRY32W);
                if (!Process32First(snap, ref pe)) return false;
                do
                {
                    char* n = pe.szExeFile;
                    // Fast reject: first char must be 'F'/'f'
                    if (n[0] == 'F' || n[0] == 'f')
                    {
                        for (int i = 0; i < Names.Length; i++)
                            if (EqualsIgnoreCase(n, Names[i])) return true;
                    }
                } while (Process32Next(snap, ref pe));
            }
            finally { CloseHandle(snap); }
            return false;
        }
    }

    // Desktop resolution switching (dynamic, never written to the registry -> a reboot always restores your normal res)
    public static class DisplayRes
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DEVMODEW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
            public int dmFields;
            public int dmPositionX, dmPositionY;
            public int dmDisplayOrientation, dmDisplayFixedOutput;
            public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
            public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsW")] static extern bool EnumDisplaySettings(string dev, int mode, ref DEVMODEW dm);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsW")] static extern int ChangeDisplaySettings(ref DEVMODEW dm, int flags);
        [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsW")] static extern int ChangeDisplaySettingsNull(IntPtr dm, int flags);

        const int DM_PELSWIDTH = 0x00080000, DM_PELSHEIGHT = 0x00100000, DM_DISPLAYFREQUENCY = 0x00400000;

        static string MarkerPath(string dir) { return Path.Combine(dir, "display_restore.flag"); }

        public static bool GetCurrent(out int w, out int h, out int hz)
        {
            w = h = hz = 0;
            DEVMODEW dm = new DEVMODEW();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODEW));
            if (!EnumDisplaySettings(null, -1, ref dm)) return false;
            w = dm.dmPelsWidth; h = dm.dmPelsHeight; hz = dm.dmDisplayFrequency;
            return true;
        }

        public static bool Apply(int w, int h, string dir)
        {
            DEVMODEW dm = new DEVMODEW();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODEW));
            if (!EnumDisplaySettings(null, -1, ref dm)) return false;
            if (dm.dmPelsWidth == w && dm.dmPelsHeight == h) return false;
            int hz = dm.dmDisplayFrequency;
            dm.dmPelsWidth = w;
            dm.dmPelsHeight = h;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY; // keep your refresh rate (e.g. 240Hz)
            dm.dmDisplayFrequency = hz;
            int r = ChangeDisplaySettings(ref dm, 0);
            if (r != 0)
            {
                dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
                r = ChangeDisplaySettings(ref dm, 0);
            }
            if (r == 0)
            {
                try { File.WriteAllText(MarkerPath(dir), DateTime.Now.ToString("o")); } catch { }
                return true;
            }
            return false;
        }

        public static void Restore(string dir)
        {
            ChangeDisplaySettingsNull(IntPtr.Zero, 0); // back to the saved Windows default mode
            try { File.Delete(MarkerPath(dir)); } catch { }
        }

        // If the PC/app crashed while stretched, put the desktop back the next time the watcher starts
        public static void RecoverIfNeeded(string dir)
        {
            try
            {
                if (File.Exists(MarkerPath(dir)) && !GameWatch.IsFortniteRunning()) Restore(dir);
            }
            catch { }
        }

        // Reads Fortnite's own config: only returns true when Fortnite is in exclusive Fullscreen at a custom (stretched) res
        public static bool TryReadFortniteStretch(out int w, out int h)
        {
            w = h = 0;
            try
            {
                string ini = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini");
                if (!File.Exists(ini)) return false;
                int mode = -1;
                foreach (string raw in File.ReadLines(ini))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("FullscreenMode=")) int.TryParse(line.Substring(15), out mode);
                    else if (line.StartsWith("ResolutionSizeX=")) int.TryParse(line.Substring(16), out w);
                    else if (line.StartsWith("ResolutionSizeY=")) int.TryParse(line.Substring(16), out h);
                }
                return mode == 0 && w >= 640 && h >= 480;
            }
            catch { return false; }
        }
    }

    // "Start with Windows" watcher registration (HKCU Run key, no admin needed)
    public static class AutoStart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "SplitCounterFortniteWatcher";

        static string Command() { return "\"" + Application.ExecutablePath + "\" -Watch"; }

        public static bool IsRegistered()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                    return k != null && k.GetValue(ValueName) != null;
            }
            catch { return false; }
        }

        public static void Enable()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey))
                    k.SetValue(ValueName, Command());
            }
            catch { }
            // Clear any pending stop request, then start the watcher now (a duplicate exits instantly via its mutex)
            try
            {
                EventWaitHandle ev;
                if (EventWaitHandle.TryOpenExisting(Program.WatcherStopEventName, out ev)) { ev.Reset(); ev.Dispose(); }
            }
            catch { }
            if (!Program.IsWatcherProcess)
            {
                try { Process.Start(new ProcessStartInfo(Application.ExecutablePath, "-Watch") { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath) }); } catch { }
            }
        }

        public static void Disable()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                    if (k != null) k.DeleteValue(ValueName, false);
            }
            catch { }
            try
            {
                EventWaitHandle ev;
                if (EventWaitHandle.TryOpenExisting(Program.WatcherStopEventName, out ev)) { ev.Set(); ev.Dispose(); }
            }
            catch { }
        }
    }

    public static class Program
    {
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public const string UiMutexName = "Local\\SplitCounterPro_Mutex";
        public const string WatcherMutexName = "Local\\SplitCounterPro_Watcher_Mutex";
        public const string WatcherStopEventName = "Local\\SplitCounterPro_Watcher_Stop";
        public static bool IsWatcherProcess = false;
        static bool uiInitialized = false;

        [STAThread]
        public static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string counterFile = Path.Combine(baseDir, "counter.txt");
            string modeArg = null;
            bool testExit = false;
            bool watch = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-CounterFile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    counterFile = args[++i];
                else if (string.Equals(args[i], "-Button", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    modeArg = args[++i];
                else if (string.Equals(args[i], "-TestExit", StringComparison.OrdinalIgnoreCase))
                    testExit = true;
                else if (string.Equals(args[i], "-Watch", StringComparison.OrdinalIgnoreCase))
                    watch = true;
            }

            if (watch) { RunWatcher(counterFile); return; }
            RunCounterUI(counterFile, modeArg, false, testExit);
        }

        public static bool RunCounterUI(string counterFile, string modeArg, bool launchedWithGame, bool testExit)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, UiMutexName, out createdNew))
            {
                if (!createdNew && !testExit)
                {
                    if (!launchedWithGame)
                    {
                        IntPtr existing = FindWindow(null, "Split Counter Pro");
                        if (existing != IntPtr.Zero)
                            CounterForm.PostMessage(existing, CounterForm.WM_APP_SHOW_EXISTING, IntPtr.Zero, IntPtr.Zero);
                    }
                    return false;
                }

                if (!uiInitialized)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    uiInitialized = true;
                }

                var form = new CounterForm(counterFile, modeArg, launchedWithGame);
                CounterForm.ProgramFormInstance = form;
                if (testExit)
                {
                    form.Shown += (s, e) => form.Close();
                }
                Application.Run(form);
                try { form.Dispose(); } catch { }
                CounterForm.ProgramFormInstance = null;
                if (createdNew) { try { mutex.ReleaseMutex(); } catch { } }
            }
            return true;
        }

        // Idle watcher: sleeps in the kernel, wakes every 2s for a ~0.1ms process snapshot. No window, no hooks, no rendering.
        static void RunWatcher(string counterFile)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, WatcherMutexName, out createdNew))
            {
                if (!createdNew) return;
                IsWatcherProcess = true;
                string dir = Path.GetDirectoryName(Path.GetFullPath(counterFile));
                string settingsPath = Path.Combine(dir, "settings.json");

                using (var stopEvt = new EventWaitHandle(false, EventResetMode.ManualReset, WatcherStopEventName))
                {
                    stopEvt.Reset();
                    SetIdlePriority();
                    DisplayRes.RecoverIfNeeded(dir);
                    CounterForm.TrimMemory();

                    while (true)
                    {
                        // 1. Dormant: wait for Fortnite to start
                        while (!GameWatch.IsFortniteRunning())
                        {
                            if (stopEvt.WaitOne(2000)) return;
                        }

                        // 2. Fortnite detected -> optional stretched res, then open the counter overlay
                        bool resChanged = false;
                        string json = "";
                        try { if (File.Exists(settingsPath)) json = File.ReadAllText(settingsPath); } catch { }
                        if (CounterForm.ExtractInt(json, "autoStretchRes", 0) == 1)
                        {
                            int fw, fh;
                            if (DisplayRes.TryReadFortniteStretch(out fw, out fh))
                                resChanged = DisplayRes.Apply(fw, fh, dir);
                        }

                        RunCounterUI(counterFile, null, true, false); // blocks until the counter closes (game exit or user close)

                        // 3. Wait until Fortnite is truly gone (2 consecutive misses) so a manual close doesn't re-open it mid-game
                        int misses = 0;
                        bool stopping = false;
                        while (misses < 2)
                        {
                            if (stopEvt.WaitOne(2000)) { stopping = true; break; }
                            misses = GameWatch.IsFortniteRunning() ? 0 : misses + 1;
                        }
                        if (resChanged || File.Exists(Path.Combine(dir, "display_restore.flag")))
                        {
                            if (!GameWatch.IsFortniteRunning()) DisplayRes.Restore(dir);
                        }
                        if (stopping) return;

                        SetIdlePriority();
                        CounterForm.TrimMemory();
                    }
                }
            }
        }

        static void SetIdlePriority()
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; } catch { }
        }
    }
}
