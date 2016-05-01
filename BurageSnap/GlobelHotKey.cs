using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BurageSnap
{
    public class GlobelHotKey
    {
        private static readonly OrderedDictionary KeyDictionary = new OrderedDictionary
        {
            {"", default(Key)},
            {"PrtSc", Key.PrintScreen},
            {"F1", Key.F1},
            {"F2", Key.F2},
            {"F3", Key.F3},
            {"F4", Key.F4},
            {"F5", Key.F5},
            {"F6", Key.F6},
            {"F7", Key.F7},
            {"F8", Key.F8},
            {"F9", Key.F9},
            {"F10", Key.F10},
            {"F11", Key.F11},
            {"F12", Key.F12},
            {"ScrLk", Key.Scroll},
            {"Pause", Key.Pause},
            {"Insert", Key.Insert},
            {"Delete", Key.Delete},
            {"Home", Key.Home},
            {"End", Key.End},
            {"PgDn", Key.PageDown},
            {"PgUp", Key.PageUp},
            {"Esc", Key.Escape},
            {"Tab", Key.Tab},
            {"Caps", Key.CapsLock},
            {"BS", Key.Back},
            {"Enter", Key.Enter},
            {"Space", Key.Space},
            {"0", Key.D0},
            {"1", Key.D1},
            {"2", Key.D2},
            {"3", Key.D3},
            {"4", Key.D4},
            {"5", Key.D5},
            {"6", Key.D6},
            {"7", Key.D7},
            {"8", Key.D8},
            {"9", Key.D9},
            {"A", Key.A},
            {"B", Key.B},
            {"C", Key.C},
            {"D", Key.D},
            {"E", Key.E},
            {"F", Key.F},
            {"G", Key.G},
            {"H", Key.H},
            {"I", Key.I},
            {"J", Key.J},
            {"K", Key.K},
            {"L", Key.L},
            {"M", Key.M},
            {"N", Key.N},
            {"O", Key.O},
            {"P", Key.P},
            {"Q", Key.Q},
            {"R", Key.R},
            {"S", Key.S},
            {"T", Key.T},
            {"U", Key.U},
            {"V", Key.V},
            {"W", Key.W},
            {"X", Key.X},
            {"Y", Key.Y},
            {"Z", Key.Z},
            {"Left", Key.Left},
            {"Up", Key.Up},
            {"Right", Key.Right},
            {"Down", Key.Down},
            {"Num0", Key.NumPad0},
            {"Num1", Key.NumPad1},
            {"Num2", Key.NumPad2},
            {"Num3", Key.NumPad3},
            {"Num4", Key.NumPad4},
            {"Num5", Key.NumPad5},
            {"Num6", Key.NumPad6},
            {"Num7", Key.NumPad7},
            {"Num8", Key.NumPad8},
            {"Num9", Key.NumPad9}
        };

        public static IEnumerable<string> KeyList => KeyDictionary.Keys.OfType<string>();

        public event Action HotKeyPressed;

        private const int HotKeyId = 0x4000;
        private HwndSource _source;
        private int _modifiers;
        private string _key;

        public void Register(Window window, int modifiers, string key)
        {
            if (_modifiers == modifiers && _key == key)
                return;
            if (_source != null)
                Unregister();
            if (key == "")
                return;
            int vkey;
            try
            {
                vkey = KeyInterop.VirtualKeyFromKey((Key)KeyDictionary[key]);
            }
            catch (KeyNotFoundException)
            {
                return;
            }
            var hWnd = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(hWnd);
            if (_source == null)
                return;
            _source.AddHook(HwndHook);
            RegisterHotKey(hWnd, HotKeyId, modifiers, vkey);
            _modifiers = modifiers;
            _key = key;
        }

        private IntPtr HwndHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int wmHotkey = 0x0312;
            if (msg == wmHotkey && wParam.ToInt32() == HotKeyId)
            {
                HotKeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Unregister()
        {
            if (_source == null)
                return;
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_source.Handle, HotKeyId);
            _source = null;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}