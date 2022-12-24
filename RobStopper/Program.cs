using System;
using System.IO;
using System.Threading;
using System.Reflection;

using System.Device.Gpio;

using RobStopper.Utilities;

namespace RobStopper
{
    internal class Program
    {
        private const string _PROGRAM_ALIAS         = "Rob Stopper";
        private const string _CONSOLE_OUTPUT_PREFIX = "[" + _PROGRAM_ALIAS + "]: ";

        private static Logger _Logger = new Logger();
        private static Config _Config = new Config();

        private static readonly object _ConfigMutex = new object();

        private static Thread _ConfigUpdaterThread;

        private static GpioController _GpioController;

        internal static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
            //{
            //    string resourceName = new AssemblyName(a.Name).Name + Helper.DYNAMIC_LINK_LIBRARY_FILENAME_EXTENSION;
            //    string resource = Array.Find(typeof(Program).Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

            //    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            //    {
            //        byte[] assemblyData = new byte[stream.Length];
            //        stream.Read(assemblyData, 0, assemblyData.Length);
            //        return Assembly.Load(assemblyData);
            //    }
            //};
            try
            {
                lock (_ConfigMutex)
                {
                    _Config.Load(_Logger);
                }
                _ConfigUpdaterThread = new Thread(ConfigUpdaterThread);
                _ConfigUpdaterThread.IsBackground = true;
                _ConfigUpdaterThread.Priority = ThreadPriority.Lowest;
                _ConfigUpdaterThread.Start();
            }
            catch (Exception exception)
            {
                string exceptionMessage = String.Empty;

                if (exception != null && !String.IsNullOrWhiteSpace(exception.Message))
                {
                    exceptionMessage += "Exception: " + exception.Message.Replace(Environment.NewLine, " ") + ".";

                    if (exception.InnerException != null && !String.IsNullOrWhiteSpace(exception.InnerException.Message))
                    {
                        exceptionMessage += " Inner exception: " + exception.InnerException.Message.Replace(Environment.NewLine, " ") + ".";
                    }
                }
                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + "ConfigUpdaterThread has failed to start: " + exceptionMessage);
                _Logger.Fatal("ConfigUpdaterThread has failed to start: " + exceptionMessage);
                Environment.Exit(-1);
            }
            using (_GpioController = new GpioController())
            {
                Logic();
            }
        }

        private static int MiR_ChangeRobotsState(MiR_REST_API.RequestModels.StateValue state)
        {
            int successfulCalls = 0;

            lock (_ConfigMutex)
            {
                foreach (Config.RobotInfo robotInfo in _Config.RobotsInfo)
                {
                    try
                    {
                        MiR_REST_API.RequestHandler.SetState(robotInfo.RobotIp, robotInfo.RobotAuthorization, state.StateId, _Config.RobotRequestTimeoutMs);

                        successfulCalls += 1;
                    }
                    catch (Exception exception)
                    {
                        string exceptionMessage = String.Empty;

                        if (exception != null && !String.IsNullOrWhiteSpace(exception.Message))
                        {
                            exceptionMessage += "Exception: " + exception.Message.Replace(Environment.NewLine, " ") + ".";

                            if (exception.InnerException != null && !String.IsNullOrWhiteSpace(exception.InnerException.Message))
                            {
                                exceptionMessage += " Inner exception: " + exception.InnerException.Message.Replace(Environment.NewLine, " ") + ".";
                            }
                        }
                        Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + "An error occurred while robot state change: " + exceptionMessage);
                        _Logger.Error("An error occurred while robot state change: " + exceptionMessage);
                    }
                }
            }
            return successfulCalls;
        }

        private static void MiR_ChangeRobotsStateWrapper(MiR_REST_API.RequestModels.StateValue state, string messageBefore, string messageAfter)
        {
            bool debugMode;

            lock (_ConfigMutex)
            {
                debugMode = _Config.DebugMode;
            }
            if (debugMode)
            {
                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + "- Debug - " + messageBefore);
                _Logger.Debug(messageBefore);

                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + "- Debug - " + messageAfter);
                _Logger.Debug(messageAfter);
            }
            else
            {
                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + messageBefore);
                _Logger.Trace(messageBefore);

                int successfulCalls = MiR_ChangeRobotsState(state);

                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + successfulCalls.ToString() + " robots have been affected.");
                _Logger.Trace(successfulCalls.ToString() + " robots have been affected.");

                Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + messageAfter);
                _Logger.Trace(messageAfter);
            }
        }

        private static void ConfigUpdaterThread()
        {
            int configReloadTimeSec;

            lock (_ConfigMutex)
            {
                configReloadTimeSec = _Config.ConfigReloadTimeSec;
            }
            while (true)
            {
                Thread.Sleep(configReloadTimeSec * Helper.MS_IN_SEC);
                try
                {
                    lock (_ConfigMutex)
                    {
                        _Config.Load(_Logger);

                        configReloadTimeSec = _Config.ConfigReloadTimeSec;
                    }
                }
                catch (Exception exception)
                {
                    string exceptionMessage = String.Empty;

                    if (exception != null && !String.IsNullOrWhiteSpace(exception.Message))
                    {
                        exceptionMessage += "Exception: " + exception.Message.Replace(Environment.NewLine, " ") + ".";

                        if (exception.InnerException != null && !String.IsNullOrWhiteSpace(exception.InnerException.Message))
                        {
                            exceptionMessage += " Inner exception: " + exception.InnerException.Message.Replace(Environment.NewLine, " ") + ".";
                        }
                    }
                    Console.WriteLine(_CONSOLE_OUTPUT_PREFIX + "An error occurred while config reloading: " + exceptionMessage);
                    _Logger.Error("An error occurred while config reloading: " + exceptionMessage);
                }
            }
        }

        private static void Logic()
        {
            _GpioController.OpenPin(GpioLayout.GPI_EMERGENCY_BUTTON, PinMode.InputPullUp);

            bool lastEmergencyButtonState = false;
            bool currentEmergencyButtonState;

            while (true)
            {
                currentEmergencyButtonState = _GpioController.Read(GpioLayout.GPI_EMERGENCY_BUTTON) == PinValue.High;

                if (currentEmergencyButtonState != lastEmergencyButtonState)
                {
                    if (currentEmergencyButtonState)
                    {
                        MiR_ChangeRobotsStateWrapper(new MiR_REST_API.RequestModels.StateValue(MiR_REST_API.RequestModels.StateValue.PAUSE),
                                                     "Emergency button press has been detected! Stopping robots...",
                                                     "Robots have been stopped.");
                    }
                    else
                    {
                        MiR_ChangeRobotsStateWrapper(new MiR_REST_API.RequestModels.StateValue(MiR_REST_API.RequestModels.StateValue.READY),
                                                    "Emergency button has been released. Setting ready status for robots...",
                                                    "Robots have been returned to ready state.");
                    }
                    lastEmergencyButtonState = currentEmergencyButtonState;
                }
                Thread.Sleep(100);
            }
        }

        private static class GpioLayout
        {
            public static int GPI_EMERGENCY_BUTTON = 2;
        }
    }
}
