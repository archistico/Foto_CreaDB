using System;
using System.Text.RegularExpressions;
using Foto_CreaDB2;

namespace FotoCreaDB.Wpf.ViewModels
{
    public class LogMessageViewModel
    {
        private static readonly Regex WindowsPathRegex =
            new Regex(
                @"[A-Za-z]:\\(?:[^<>:""/\\|?*\r\n]+\\)*[^<>:""/\\|?*\r\n]+",
                RegexOptions.Compiled);

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

        public string PrefixText
        {
            get
            {
                ExtractParts(out string prefix, out _, out _);
                return prefix;
            }
        }

        public string ClickablePath
        {
            get
            {
                ExtractParts(out _, out string path, out _);
                return path;
            }
        }

        public string SuffixText
        {
            get
            {
                ExtractParts(out _, out _, out string suffix);
                return suffix;
            }
        }

        public bool HasClickablePath
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ClickablePath);
            }
        }

        private void ExtractParts(out string prefix, out string path, out string suffix)
        {
            string text = FullText;

            prefix = text;
            path = string.Empty;
            suffix = string.Empty;

            if (TryExtractPathAfterLastColon(text, out prefix, out path, out suffix))
            {
                return;
            }

            Match match = WindowsPathRegex.Match(text);
            if (!match.Success)
            {
                return;
            }

            prefix = text.Substring(0, match.Index);
            path = match.Value;
            suffix = text.Substring(match.Index + match.Value.Length);
        }

        private bool TryExtractPathAfterLastColon(string text, out string prefix, out string path, out string suffix)
        {
            prefix = text;
            path = string.Empty;
            suffix = string.Empty;

            int separatorIndex = text.LastIndexOf(": ", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return false;
            }

            string candidate = text.Substring(separatorIndex + 2).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            if (!WindowsPathRegex.IsMatch(candidate))
            {
                return false;
            }

            Match pathMatch = WindowsPathRegex.Match(candidate);
            if (!pathMatch.Success || pathMatch.Index != 0)
            {
                return false;
            }

            prefix = text.Substring(0, separatorIndex + 2);
            path = pathMatch.Value;
            suffix = candidate.Substring(path.Length);

            return true;
        }
    }
}