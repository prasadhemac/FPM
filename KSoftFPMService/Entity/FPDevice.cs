using Riss.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace KSoftFingerPrintManager.Entity
{
    static class FPDevice
    {
        static Engine s_Engine = new Engine();

        public static void Init()
        {
            s_Engine.Init(1, "0", "ZDC2911", 5, "192.168.1.106", 8000, CommunicationType.Tcp);
            
        }
        public static void Run()
        {
            s_Engine.Run();
        }

        
    }
}
