using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using WinGraphicsController.view;

namespace SelfDestruct
{

    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    public partial class DestructCountdown : ServiceBase
    {

        private int eventId = 1;

        private string sourceName = "DestructSource";
        private string logName = "DestructLog";
        private string audioFilePath = "ALIEN - Nostromo destruct sequence.mp3";

        private int timeToDetonation = 600000;
        private int timeForManualOverride = 300000;
        private bool debugMode = true;

        private CountdownTimer pollTimer;
        private DestructionAudioPlayer audioPlayer;
        private WinBackgroundOverride bkgOverride;

        public DestructCountdown(string[] args)
        {
            InitializeComponent();

            //Set runtime parameters for user control.
            this.CanPauseAndContinue = true;
            this.CanStop = true;

            //Load parameters
            if (args.Length > 0)
            {
                if (args[0].Any(Char.IsDigit))
                {
                    timeToDetonation = int.Parse(args[0]);
                }
            }
            if (args.Length > 1)
            {
                if (args[1].Any(Char.IsDigit))
                {
                    timeForManualOverride = int.Parse(args[1]);
                }
            }
            if (args.Length > 2)
            {
                if (args[2].Equals("true"))
                {
                    debugMode = true;
                }
            }

            //Initialise eventlog
            eventLog1 = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists(sourceName))
            {
                System.Diagnostics.EventLog.CreateEventSource(sourceName, logName);
            }
            eventLog1.Source = sourceName;
            eventLog1.Log = logName;
        }

        protected override void OnStart(string[] args)
        {

            //If debug mode is requested attempt to launch the systems debugger
            if (debugMode)
            {
                System.Diagnostics.Debugger.Launch();
            }

            // Update the service state to Start Pending.  
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("OnStart: Initialising destruct service.");

            //Initialise and open audio player
            string filePath = GetAppAbsolutePath(audioFilePath);
            audioPlayer = new DestructionAudioPlayer(filePath);
            eventLog1.WriteEntry("OnStart: destruct sequence audio player initialised.");

            //TODO: Initialise the background override controller and draw a rectangle
            bkgOverride = new WinBackgroundOverride();
            bkgOverride.DemoDrawRect(500, 500, 200, 200);

            //Initialise the countdown timer
            pollTimer = new CountdownTimer
            {
                Interval = audioPlayer.FileLength
            };

            //Assign a new event handler to the timer
            pollTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);

            pollTimer.Start();


            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            audioPlayer.Play();

        }

        protected override void OnStop()
        {

            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("OnStop: Stopping of destruct service.");

            bkgOverride.ClearBackground();

            //Close and dispose of the audio player
            audioPlayer.Stop();
            audioPlayer.Close();
            eventLog1.WriteEntry("OnStop: Closed audio player.");

            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnPause()
        {

            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSE_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            EventLog.WriteEntry("OnPause: Destruct sequence has been paused");
            audioPlayer.Stop();

            pollTimer.Stop();

            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_PAUSED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

        }

        protected override void OnContinue()
        {
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_CONTINUE_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("OnContinue: Continuing service.");

            audioPlayer.Play();

            pollTimer.Continue();

            // Update the service state to Running.  
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //TODO: Insert monitoring activities here
            eventLog1.WriteEntry("Scheduled monitoring in progress.", EventLogEntryType.Information, eventId++);

            if (this.CanPauseAndContinue) {
                eventLog1.WriteEntry("Pause and continue overrides have been disabled, for your safety.", EventLogEntryType.Warning, eventId++);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="serviceStatus"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        /// <summary>
        /// This method seems a bit complicated for fetching a file's path,
        /// but it's flexible enough to fetch a path for both console 
        /// applications and service applications.
        /// </summary>
        /// <param name="relativePath">Relative path to a resource.</param>
        /// <returns>Absolute path.</returns>
        private string GetAppAbsolutePath(string relativePath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), relativePath);
        }

    }

}
