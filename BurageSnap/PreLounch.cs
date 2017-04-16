using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace BurageSnap
{
    public class PreLounch
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc enumProc, IntPtr lParam);

        public static bool ProcessAlreadyExists()
        {
            try
            {
                var cur = Process.GetCurrentProcess();
                var process = Process.GetProcessesByName(cur.ProcessName)
                    .FirstOrDefault(p => cur.Id != p.Id && p.MainModule.FileName == cur.MainModule.FileName);
                if (process != null)
                {
                    ActivateProcessWindow(process);
                    return true;
                }
            }
            catch (Win32Exception)
            {
                /*
                 * MainModule.FileName can fail by AV software with Win32Exception.
                */
            }
            return false;
        }

        private static void ActivateProcessWindow(Process process)
        {
            var hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero) // stored in the system tray
            {
                EnumThreadWindows(process.Threads[0].Id, (hWnd, lParam) =>
                {
                    hwnd = hWnd;
                    return false;
                }, IntPtr.Zero);
            }
            ShowWindowAsync(hwnd, 9); // SW_RESTORE
            SetForegroundWindow(hwnd);
        }
    }
}