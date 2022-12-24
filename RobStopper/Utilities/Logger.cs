namespace RobStopper.Utilities
{
    public sealed class Logger
    {
        private const    string _FILE_EXT         = ".log";
        private const    string _DATE_TIME_FORMAT = "dd-MM-yyyy | hh:mm:ss tt";
        private readonly string _LogFileName;
        private readonly string _LogFilePath;
        private readonly object _FileLocker;

        /// <summary>
        /// Initiate an instance of Logger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        public Logger(string logFileName = null)
        {
            if (System.String.IsNullOrEmpty(logFileName))
            {
                logFileName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            }
            _FileLocker  = new object();
            _LogFileName = logFileName + _FILE_EXT;
            _LogFilePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, _LogFileName);

            // Log file header line
            string logHeader = _LogFileName + " is created.";
            if (!System.IO.File.Exists(_LogFilePath))
            {
                WriteLine(System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " " + logHeader);
            }
        }

        /// <summary>
        /// Log a DEBUG message
        /// </summary>
        /// <param name="text">Message</param>
        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.DEBUG, text);
        }

        /// <summary>
        /// Log an ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.ERROR, text);
        }

        /// <summary>
        /// Log a FATAL ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.FATAL, text);
        }

        /// <summary>
        /// Log an INFO message
        /// </summary>
        /// <param name="text">Message</param>
        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.INFO, text);
        }

        /// <summary>
        /// Log a TRACE message
        /// </summary>
        /// <param name="text">Message</param>
        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.TRACE, text);
        }

        /// <summary>
        /// Log a WARNING message
        /// </summary>
        /// <param name="text">Message</param>
        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.WARNING, text);
        }

        private void WriteLine(string text, bool append = false)
        {
            try
            {
                if (System.String.IsNullOrEmpty(text))
                {
                    return;
                }
                lock (_FileLocker)
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(_LogFilePath, append, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine(text);
                    }
                }
            }
            catch (System.Exception exception)
            {
                throw new System.Exception("The Logger threw an exception: " + exception.Message);
            }
        }

        private void WriteFormattedLog(LogLevel level, string text)
        {
            string pretext;
            switch (level)
            {
                case LogLevel.TRACE:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [TRACE]   ";
                    break;
                case LogLevel.INFO:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [INFO]    ";
                    break;
                case LogLevel.DEBUG:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [DEBUG]   ";
                    break;
                case LogLevel.WARNING:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [WARNING] ";
                    break;
                case LogLevel.ERROR:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [ERROR]   ";
                    break;
                case LogLevel.FATAL:
                    pretext = System.DateTime.Now.ToString(_DATE_TIME_FORMAT) + " [FATAL]   ";
                    break;
                default:
                    pretext = "";
                    break;
            }
            WriteLine(pretext + text, true);
        }

        [System.Flags]
        private enum LogLevel
        {
            TRACE,
            INFO,
            DEBUG,
            WARNING,
            ERROR,
            FATAL
        }
    }
}
