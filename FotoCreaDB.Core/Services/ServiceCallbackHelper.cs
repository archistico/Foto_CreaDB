using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Metodi di supporto per emissione log e avanzamento dai servizi.
    /// </summary>
    public static class ServiceCallbackHelper
    {
        /// <summary>
        /// Invia un messaggio informativo al callback di log.
        /// </summary>
        /// <param name="log">Callback di log.</param>
        /// <param name="message">Messaggio da inviare.</param>
        public static void Info(Action<ServiceLogMessage> log, string message)
        {
            Write(log, ServiceLogLevel.Info, message, null);
        }

        /// <summary>
        /// Invia un messaggio di avviso al callback di log.
        /// </summary>
        /// <param name="log">Callback di log.</param>
        /// <param name="message">Messaggio da inviare.</param>
        public static void Warning(Action<ServiceLogMessage> log, string message)
        {
            Write(log, ServiceLogLevel.Warning, message, null);
        }

        /// <summary>
        /// Invia un messaggio di errore al callback di log.
        /// </summary>
        /// <param name="log">Callback di log.</param>
        /// <param name="message">Messaggio da inviare.</param>
        /// <param name="ex">Eccezione associata.</param>
        public static void Error(Action<ServiceLogMessage> log, string message, Exception ex = null)
        {
            Write(log, ServiceLogLevel.Error, message, ex);
        }

        /// <summary>
        /// Notifica un avanzamento se il callback di progress è presente.
        /// </summary>
        /// <typeparam name="T">Tipo del payload di progressione.</typeparam>
        /// <param name="progress">Callback di progressione.</param>
        /// <param name="value">Valore di avanzamento da inviare.</param>
        public static void ReportProgress<T>(IProgress<T> progress, T value)
        {
            if (progress != null)
            {
                progress.Report(value);
            }
        }

        /// <summary>
        /// Invia un messaggio strutturato al callback di log.
        /// </summary>
        /// <param name="log">Callback di log.</param>
        /// <param name="level">Livello del messaggio.</param>
        /// <param name="message">Testo del messaggio.</param>
        /// <param name="ex">Eccezione associata.</param>
        private static void Write(Action<ServiceLogMessage> log, ServiceLogLevel level, string message, Exception ex)
        {
            if (log == null)
            {
                return;
            }

            log(new ServiceLogMessage
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Exception = ex
            });
        }
    }
}