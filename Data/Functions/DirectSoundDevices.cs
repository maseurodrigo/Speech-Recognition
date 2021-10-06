using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Speech_Recognition.Data.Functions
{
    public class DirectSoundDevices
    {
        [DllImport("dsound.dll", CharSet = CharSet.Ansi)]
        static extern void DirectSoundCaptureEnumerate(DSEnumCallback callback, IntPtr context);
        delegate bool DSEnumCallback([MarshalAs(UnmanagedType.LPStruct)] Guid guid,
            String description, String module, IntPtr lpContext);
        static bool EnumCallback(Guid guid, String description, String module, IntPtr context) {
            if (guid != Guid.Empty) captureDevices.Add(new DirectSoundDeviceInfo(guid, description, module));
            return true;
        }
        private static List<DirectSoundDeviceInfo> captureDevices;
        public static IEnumerable<DirectSoundDeviceInfo> GetCaptureDevices() {
            captureDevices = new List<DirectSoundDeviceInfo>();
            DirectSoundCaptureEnumerate(new DSEnumCallback(EnumCallback), IntPtr.Zero);
            return captureDevices;
        }
    }

    public class DirectSoundDeviceInfo {
        public DirectSoundDeviceInfo(Guid guid, String description, String module)
        { Guid = guid; Description = description; Module = module; }
        public Guid Guid { get; }
        public String Description { get; }
        public String Module { get; }
    }
}