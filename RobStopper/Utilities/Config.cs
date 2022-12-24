using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace RobStopper.Utilities
{
    [Serializable]
    public class Config
    {
        [NonSerialized]
        public const string CFG_FILE_NAME = "Config.cfg";

        public List<RobotInfo> RobotsInfo;

        public int  RobotRequestTimeoutMs;
        public int  ConfigReloadTimeSec;
        public bool DebugMode;

        public Config()
        {
            RobotsInfo = new List<RobotInfo>();

            RobotRequestTimeoutMs = 3000;
            ConfigReloadTimeSec   = 300;
            DebugMode             = false;
        }

        public void Load(Logger logger = null)
        {
            Config config = Deserialize(logger);

            if (config == null)
            {
                config = new Config();

                if (config.Serialize(logger))
                {
                    if (logger != null)
                    {
                        string thisMethodFullName = typeof(Config) + "." + MethodBase.GetCurrentMethod().Name;

                        logger.Trace(thisMethodFullName + ": cannot find a config. A new one has been created with default setup.");
                    }
                }
                else
                {
                    if (logger != null)
                    {
                        string thisMethodFullName = typeof(Config) + "." + MethodBase.GetCurrentMethod().Name;

                        logger.Fatal(thisMethodFullName + "Can't even to create the default config!");
                    }
                    throw new Exception("Can't even to create the default config! Look at the logs.");
                }
            }
            RobotsInfo            = config.RobotsInfo;
            RobotRequestTimeoutMs = config.RobotRequestTimeoutMs;
            ConfigReloadTimeSec   = config.ConfigReloadTimeSec;
            DebugMode             = config.DebugMode;
        }

        public bool Save(Logger logger = null)
        {
            return Serialize(logger);
        }

        private bool Serialize(Logger logger = null)
        {
            bool bResult = false;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());
                using (TextWriter textWriter = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CFG_FILE_NAME)))
                {
                    xmlSerializer.Serialize(textWriter, this);
                }
                bResult = true;
            }
            catch (Exception exception)
            {
                if (logger != null)
                {
                    string thisMethodFullName = typeof(Config) + "." + MethodBase.GetCurrentMethod().Name;
                    string exceptionMessage = String.Empty;

                    if (exception != null && !String.IsNullOrWhiteSpace(exception.Message))
                    {
                        exceptionMessage += "Exception: " + exception.Message.Replace(Environment.NewLine, " ") + ".";

                        if (exception.InnerException != null && !String.IsNullOrWhiteSpace(exception.InnerException.Message))
                        {
                            exceptionMessage += " Inner exception: " + exception.InnerException.Message.Replace(Environment.NewLine, " ") + ".";
                        }
                    }
                    logger.Error(thisMethodFullName + ": " + exceptionMessage);
                }
            }
            return bResult;
        }

        private static Config Deserialize(Logger logger = null)
        {
            Config config = null;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));
                using (FileStream fileStream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CFG_FILE_NAME), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    config = (Config)xmlSerializer.Deserialize(fileStream);
                }
            }
            catch (Exception exception)
            {
                if (logger != null)
                {
                    string thisMethodFullName = typeof(Config) + "." + MethodBase.GetCurrentMethod().Name;
                    string exceptionMessage = String.Empty;

                    if (exception != null && !String.IsNullOrWhiteSpace(exception.Message))
                    {
                        exceptionMessage += "Exception: " + exception.Message.Replace(Environment.NewLine, " ") + ".";

                        if (exception.InnerException != null && !String.IsNullOrWhiteSpace(exception.InnerException.Message))
                        {
                            exceptionMessage += " Inner exception: " + exception.InnerException.Message.Replace(Environment.NewLine, " ") + ".";
                        }
                    }
                    logger.Error(thisMethodFullName + ": " + exceptionMessage);
                }
            }
            return config;
        }

        public class RobotInfo
        {
            public string RobotIp;
            public string RobotAuthorization;
        }
    }
}
