using KSoftFingerPrintManager.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace KSoftFPMService
{
    public partial class FPMService : ServiceBase
    {
        System.Timers.Timer timeDelay;
        int count;
        public FPMService()
        {
            InitializeComponent();
            timeDelay = new System.Timers.Timer();
            timeDelay.Interval = 1800000;
            timeDelay.Elapsed += new System.Timers.ElapsedEventHandler(WorkProcess);
        }

        private void WorkProcess(object sender, ElapsedEventArgs e)
        {
            //string process = "Timer Tick " + count;
            //LogService(process);
            count++;
            KSoftFingerPrintManager.Entity.FPDevice.Init();
            KSoftFingerPrintManager.Entity.FPDevice.Run();
        }

        protected override void OnStart(string[] args)
        {
            Logger.log("Service is Started");
            timeDelay.Enabled = true;
            KSoftFingerPrintManager.Entity.FPDevice.Init();
            KSoftFingerPrintManager.Entity.FPDevice.Run();
        }

        protected override void OnStop()
        {
            Logger.log("Service Stoped");
            timeDelay.Enabled = false;
        }

       
    }
}
