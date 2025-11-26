using System.Runtime.InteropServices;

namespace BatteryManagerService.Services
{
    /// <summary>
    /// Service that provides global keyboard shortcuts.
    /// </summary>
    public class KeyboardHookService : IDisposable
    {
        private readonly ILogger<KeyboardHookService> _logger;
        private readonly Action _exitAction;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_X = 0x58;
        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public KeyboardHookService(ILogger<KeyboardHookService> logger, Action exitAction)
        {
            _logger = logger;
            _exitAction = exitAction;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            _logger.LogInformation("Keyboard hook installed (Ctrl+Shift+X to exit)");
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // Check for Ctrl+Shift+X
                if (vkCode == VK_X)
                {
                    bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;  // VK_CONTROL
                    bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
                    
                    if (ctrlPressed && shiftPressed)
                    {
                        _logger.LogInformation("Ctrl+Shift+X detected - exiting application");
                        Task.Run(() => _exitAction());
                        return (IntPtr)1; // Suppress the key
                    }
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _logger.LogInformation("Keyboard hook uninstalled");
            }
        }
    }
}
