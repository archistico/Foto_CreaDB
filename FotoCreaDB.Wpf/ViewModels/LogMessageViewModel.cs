using System;
using Foto_CreaDB2;

namespace FotoCreaDB.Wpf.ViewModels
{
    public class LogMessageViewModel
    {
        public DateTime Timestamp { get; set; }

        public ServiceLogLevel Level { get; set; }

        public string Message { get; set; } = string.Empty;

        public string ExceptionMessage { get; set; } = string.Empty;

        public string FullText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExceptionMessage))
                {
                    return "[" + Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                        + "[" + Level.ToString().ToUpper() + "] "
                        + Message;
                }

                return "[" + Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                    + "[" + Level.ToString().ToUpper() + "] "
                    + Message
                    + " - "
                    + ExceptionMessage;
            }
        }
    }
}