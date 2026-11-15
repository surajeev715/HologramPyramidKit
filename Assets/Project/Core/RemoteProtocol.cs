using System;
using System.Text;

//Limits, IDs and links of the remote link. Host and remote run the same build, so both always use the same values.
public static class RemoteProtocol
{
    public const int VERSION = 1;
    public const int MAX_REMOTES = 4;
    public const int MAX_MESSAGE_CHARS = 512;
    public const int MAX_MESSAGES_PER_SECOND = 20;
    public const int MAX_NAME_CHARS = 24;

    public const float PING_INTERVAL_IN_SECONDS = 2f;
    public const float PEER_TIMEOUT_IN_SECONDS = 6f;
    public const float CONNECT_TIMEOUT_IN_SECONDS = 10f;

    //A remote sends at most this often, which stays below MAX_MESSAGES_PER_SECOND together with its pings.
    public const float MIN_SENSOR_SEND_INTERVAL_IN_SECONDS = 0.06f;

    //A refused remote gets this long to receive the "full" message before the host closes the connection.
    public const int FULL_CLOSE_DELAY_IN_MILLISECONDS = 500;

    public const int HOST_ID_RANDOM_CHARS = 16;

    public const string ERROR_LIBRARY = "library";

    private const string JOIN_PARAMETER = "join";
    private const string HOST_ID_PREFIX = "hpk-";

    //32 chars, no look-alikes (l, o, 0, 1).
    private const string HOST_ID_ALPHABET = "abcdefghijkmnpqrstuvwxyz23456789";
    private const int HOST_ID_ALPHABET_MASK = 31;

    private const string DEFAULT_NAME = "Remote";

    //The host ID is the only thing that protects a host from strangers, so it is made from secure random bytes.
    public static string CreateHostId(byte[] randomBytes)
    {
        if (randomBytes == null || randomBytes.Length != HOST_ID_RANDOM_CHARS)
        {
            throw new ArgumentException("Expected " + HOST_ID_RANDOM_CHARS + " random bytes.", nameof(randomBytes));
        }

        StringBuilder id = new StringBuilder(HOST_ID_PREFIX);
        foreach (byte value in randomBytes)
        {
            //The alphabet has exactly 32 chars, so masking does not prefer any of them.
            id.Append(HOST_ID_ALPHABET[value & HOST_ID_ALPHABET_MASK]);
        }
        return id.ToString();
    }

    public static bool IsValidHostId(string id)
    {
        if (id == null || id.Length != HOST_ID_PREFIX.Length + HOST_ID_RANDOM_CHARS)
        {
            return false;
        }
        if (!id.StartsWith(HOST_ID_PREFIX, StringComparison.Ordinal))
        {
            return false;
        }
        for (int i = HOST_ID_PREFIX.Length; i < id.Length; i++)
        {
            if (HOST_ID_ALPHABET.IndexOf(id[i]) < 0)
            {
                return false;
            }
        }
        return true;
    }

    //The link a remote opens: the page the host runs on, plus the host ID.
    public static string BuildJoinUrl(string pageUrl, string hostId)
    {
        return StripQueryAndFragment(pageUrl) + "?" + JOIN_PARAMETER + "=" + hostId;
    }

    //Returns the host ID from a join link. Null if the page was opened without one (this device is the host).
    //The ID is returned unchecked, test it with IsValidHostId.
    public static string ReadJoinId(string pageUrl)
    {
        if (string.IsNullOrEmpty(pageUrl))
        {
            return null;
        }

        int queryStart = pageUrl.IndexOf('?');
        if (queryStart < 0)
        {
            return null;
        }

        string query = pageUrl.Substring(queryStart + 1);
        int fragmentStart = query.IndexOf('#');
        if (fragmentStart >= 0)
        {
            query = query.Substring(0, fragmentStart);
        }

        foreach (string pair in query.Split('&'))
        {
            int equals = pair.IndexOf('=');
            string key = equals < 0 ? pair : pair.Substring(0, equals);
            if (key == JOIN_PARAMETER)
            {
                return equals < 0 ? "" : pair.Substring(equals + 1);
            }
        }
        return null;
    }

    public static string CleanName(string name)
    {
        return CleanText(name, DEFAULT_NAME);
    }

    //Texts come from other devices: cut to the allowed length and without control characters.
    public static string CleanText(string name, string fallback)
    {
        if (name == null)
        {
            return fallback;
        }

        StringBuilder clean = new StringBuilder();
        foreach (char c in name.Trim())
        {
            if (clean.Length >= MAX_NAME_CHARS)
            {
                break;
            }
            if (!char.IsControl(c))
            {
                clean.Append(c);
            }
        }
        return clean.Length > 0 ? clean.ToString() : fallback;
    }

    //True for errors that only concern the signaling server. Open connections keep working through them.
    public static bool IsSignalingError(string errorType)
    {
        switch (errorType)
        {
            case "network":
            case "server-error":
            case "disconnected":
            case "socket-error":
            case "socket-closed":
                return true;
            default:
                return false;
        }
    }

    public static string DescribeError(string errorType, string message)
    {
        switch (errorType)
        {
            case "peer-unavailable": return "Host not found. Is the host page still open? Scan the QR code again.";
            case "network": return "No connection to the signaling server. Check the internet connection.";
            case "server-error": return "The signaling server had a problem. Try again in a moment.";
            case "unavailable-id": return "This host ID is taken. Try again.";
            case "browser-incompatible": return "This browser does not support WebRTC.";
            case "disconnected": return "Lost the signaling server.";
            case ERROR_LIBRARY: return "The connection library could not be loaded. Reload the page.";
            default: return string.IsNullOrEmpty(message) ? errorType : message;
        }
    }

    private static string StripQueryAndFragment(string url)
    {
        int end = url.IndexOfAny(new[] { '?', '#' });
        return end < 0 ? url : url.Substring(0, end);
    }
}
