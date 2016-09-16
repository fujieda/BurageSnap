// Copyright (C) 2016 Kazuhiro Fujieda <fujieda@users.osdn.me>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BurageSnap
{
    public static class WindowPicker
    {
        private static IntPtr _hHook;
        private static string _origCursor;
        private static NativeMethods.LowLevelMouseProc _hookProc;

        public static event Action<string> Picked;

        public static void Start()
        {
            var hMod = Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]);
            _hookProc = HookProc;
            _hHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _hookProc, hMod, 0);
            ChangeCursor();
        }

        public static void Stop()
        {
            if (_hHook != IntPtr.Zero)
                NativeMethods.UnhookWindowsHookEx(_hHook);
            _hHook = IntPtr.Zero;
            RestoreCursor();
        }

        private static IntPtr HookProc(int nCode, NativeMethods.Message wParam, ref NativeMethods.MSLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0 && wParam != NativeMethods.Message.WM_MOUSEMOVE)
            {
                Stop();
                if (wParam == NativeMethods.Message.WM_LBUTTONDOWN)
                {
                    Picked?.Invoke(PickWindowTitle(lParam.pt));
                    return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(_hHook, nCode, wParam, ref lParam);
        }

        private static void ChangeCursor()
        {
            if (_origCursor == null)
                _origCursor = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Arrow", null);
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Arrow", @"%SystemRoot%\cursors\cross_r.cur");
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero,
                NativeMethods.SPI_UPDATEINIFILE | NativeMethods.SPI_SENDCHANGE);
        }

        private static void RestoreCursor()
        {
            if (_origCursor == null)
                return;
            Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Arrow", _origCursor);
            NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETCURSORS, 0, IntPtr.Zero,
                NativeMethods.SPI_UPDATEINIFILE | NativeMethods.SPI_SENDCHANGE);
        }

        private static string PickWindowTitle(NativeMethods.POINT point)
        {
            var window = NativeMethods.WindowFromPoint(point);
            while (true)
            {
                var parent = NativeMethods.GetParent(window);
                if (parent == IntPtr.Zero)
                    return Capture.GetWindowText(window);
                window = parent;
            }
        }

        // ReSharper disable All
        private static class NativeMethods
        {
            public const int WH_MOUSE_LL = 14;

            public enum Message
            {
                WM_MOUSEMOVE = 0x0200,
                WM_LBUTTONDOWN = 0x0201,
                WM_LBUTTONDBLCLK = 0x0203,
                WM_RBUTTONDOWN = 0x0204,
                WM_RBUTTONUP = 0x0205,
                WM_MBUTTONDOWN = 0x0207,
                WM_MBUTTONUP = 0x0208,
                WM_MOUSEWHEEL = 0x020A,
                WM_XBUTTONDOWN = 0x20B,
                WM_XBUTTONUP = 0x20C
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            public delegate IntPtr LowLevelMouseProc(int nCode, Message wParam, ref MSLLHOOKSTRUCT lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod,
                uint dwThreadId);

            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, Message wParam, ref MSLLHOOKSTRUCT lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(POINT point);

            [DllImport("user32.dll")]
            public static extern IntPtr GetParent(IntPtr hWnd);

            public const int SPI_SETCURSORS = 0x0057;
            public const int SPI_UPDATEINIFILE = 0x01;
            public const int SPI_SENDCHANGE = 0x02;

            [DllImport("user32.dll")]
            public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
        }
    }
}