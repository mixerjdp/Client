using System;
using System.Text;

namespace ReverseAppClient
{
    internal sealed class RatHandshakeMessage
    {
        public string ClientIp { get; set; }
        public string PcName { get; set; }
        public string OsName { get; set; }
        public string Mutex { get; set; }
        public string SocketHash { get; set; }
        public string Raw { get; set; }
    }

    internal sealed class RoutedWindowMessage
    {
        public string WindowTag { get; set; }
        public string Raw { get; set; }
    }

    internal sealed class CaptureMessage
    {
        public string WindowTag { get; set; }
        public byte[] ImageBytes { get; set; }
        public string Raw { get; set; }
    }

    internal sealed class RemoteMessageParser
    {
        public bool TryParseHandshake(StringBuilder buffer, out RatHandshakeMessage handshake)
        {
            handshake = null;

            var raw = buffer.ToString();
            var pipeIndex = raw.IndexOf("|", StringComparison.Ordinal);
            if (pipeIndex <= 0)
            {
                return false;
            }

            var prefix = raw.Substring(0, pipeIndex);
            if (!string.Equals(prefix, "M1X3R", StringComparison.Ordinal))
            {
                return false;
            }

            var parts = raw.Split('|');
            if (parts.Length < 5)
            {
                return false;
            }

            handshake = new RatHandshakeMessage
            {
                Raw = raw,
                ClientIp = parts[1],
                PcName = parts[2],
                OsName = parts[3],
                Mutex = parts[4]
            };

            return true;
        }

        public bool TryParseRoutedWindow(StringBuilder buffer, out RoutedWindowMessage routed)
        {
            routed = null;

            var raw = buffer.ToString();
            var pipeIndex = raw.IndexOf("|", StringComparison.Ordinal);
            if (pipeIndex <= 0)
            {
                return false;
            }

            var prefix = raw.Substring(0, pipeIndex);
            if (string.Equals(prefix, "M1X3R", StringComparison.Ordinal))
            {
                return false;
            }

            var parts = raw.Split('|');
            if (parts.Length < 2)
            {
                return false;
            }

            routed = new RoutedWindowMessage
            {
                Raw = raw,
                WindowTag = parts[1]
            };

            return true;
        }

        public bool TryParseCapture(string raw, out CaptureMessage capture)
        {
            capture = null;

            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            const string delimiter = "<:imagen:>";
            if (raw.IndexOf(delimiter, StringComparison.Ordinal) < 0)
            {
                return false;
            }

            string[] split = raw.Split(new string[] { delimiter }, StringSplitOptions.None);
            if (split.Length < 3)
            {
                return false;
            }

            capture = new CaptureMessage
            {
                Raw = raw,
                ImageBytes = Convert.FromBase64String(split[1]),
                WindowTag = split[2]
            };

            return true;
        }
    }
}
