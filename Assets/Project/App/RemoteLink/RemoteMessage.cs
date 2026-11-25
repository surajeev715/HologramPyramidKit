using System;
using System.Collections.Generic;
using UnityEngine;

//Which fields of a RemoteMessage are used.
public enum RemoteMessageType
{
    Invalid,   // missing or unknown type
    Hello,     // remote -> host: name
    Welcome,   // host -> remote: accepted
    Full,      // host -> remote: refused, too many remotes
    Sensor,    // remote -> host: seq, clap, blow, light
    Ping,      // remote -> host: t, rtt
    Pong,      // host -> remote: t of the ping
    Command,   // remote -> host: command (and spin for SetSpin, shapeIndex for SelectShape, variantIndex for SelectVariant,
               //                 wordPerLine for SetWordPerLine)
    State,     // host -> remote: shape, spin, animation, shapeIndex, variantIndex, features, wordPerLine
    Shapes,    // host -> remote: a part of the names of all shapes (names, first, total), sent when a remote joins
    Variants   // host -> remote: a part of the variants of the shape at shapeIndex (names, first, total), sent when the shape changes
}

//Commands a remote can give, they do the same as the buttons on the host.
public enum RemoteCommand
{
    None,   // missing or unknown command
    SetSpin,
    SelectShape,
    SelectVariant,
    SetWordPerLine
}

//One message between a remote and the host, sent as JSON. Which fields are used depends on the type.
[Serializable]
public class RemoteMessage
{
    //How type and command are written in the JSON. Text instead of numbers, so the messages stay readable.
    private static readonly Dictionary<RemoteMessageType, string> TYPE_NAMES = new Dictionary<RemoteMessageType, string>
    {
        { RemoteMessageType.Hello, "hello" },
        { RemoteMessageType.Welcome, "welcome" },
        { RemoteMessageType.Full, "full" },
        { RemoteMessageType.Sensor, "sensor" },
        { RemoteMessageType.Ping, "ping" },
        { RemoteMessageType.Pong, "pong" },
        { RemoteMessageType.Command, "command" },
        { RemoteMessageType.State, "state" },
        { RemoteMessageType.Shapes, "shapes" },
        { RemoteMessageType.Variants, "variants" }
    };

    private static readonly Dictionary<RemoteCommand, string> COMMAND_NAMES = new Dictionary<RemoteCommand, string>
    {
        { RemoteCommand.SetSpin, "setSpin" },
        { RemoteCommand.SelectShape, "selectShape" },
        { RemoteCommand.SelectVariant, "selectVariant" },
        { RemoteCommand.SetWordPerLine, "setWordPerLine" }
    };

    public const float UNKNOWN_RTT = -1f;

    private const int NAME_OVERHEAD_CHARS = 3;
    private const int ESCAPED_CHARS = 2;
    private const int UNICODE_ESCAPED_CHARS = 6;
    private const char MAX_ASCII = (char)127;

    //Protocol version. Not set by default, so a received message without it is invalid.
    public int v;
    [SerializeField] private string type;
    public string name;
    public int seq;

    //clap and blow: happened since the previous sensor message. light: is on right now.
    public bool clap;
    public bool blow;
    public bool light;

    public string text;

    [SerializeField] private string command;

    //What the host shows right now. animation is the shown variant, empty if the shape has none to switch between.
    public string shape;
    public bool spin;
    public string animation;

    public int shapeIndex = RemoteProtocol.NO_INDEX;
    public int variantIndex = RemoteProtocol.NO_INDEX;
    public int features;
    public bool wordPerLine;

    public string[] names;
    public int first;
    public int total;

    //Send time of a ping in milliseconds, the pong returns it unchanged.
    public double t;

    //Round trip time in milliseconds the remote measured with its previous ping.
    public float rtt = UNKNOWN_RTT;

    public RemoteMessageType Type
    {
        get => FromName(TYPE_NAMES, type, RemoteMessageType.Invalid);
        set => type = TYPE_NAMES.TryGetValue(value, out string text) ? text : null;
    }

    public RemoteCommand Command
    {
        get => FromName(COMMAND_NAMES, command, RemoteCommand.None);
        set => command = COMMAND_NAMES.TryGetValue(value, out string text) ? text : null;
    }

    public static RemoteMessage Create(RemoteMessageType type)
    {
        return new RemoteMessage { v = RemoteProtocol.VERSION, Type = type };
    }

    public string ToJson()
    {
        Debug.Log($"[REMOTE MESSAGE] send message with text: {this.text}");
        return JsonUtility.ToJson(this);
    }

    //Returns a checked message, or null. Never trust what arrives over the network.
    public static RemoteMessage Parse(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > RemoteProtocol.MAX_MESSAGE_CHARS)
        {
            return null;
        }

        RemoteMessage message;
        try
        {
            message = JsonUtility.FromJson<RemoteMessage>(json);
            Debug.Log($"[REMOTE MESSAGE] message received with text: {message.text}");
        }
        catch (Exception)
        {
            Debug.Log($"[REMOTE MESSAGE] Message reading failed");
            //Whatever a stranger sends must not stop the frame, so every parse failure is just an invalid message.
            return null;
        }

        if (message == null || message.v != RemoteProtocol.VERSION)
        {
            return null;
        }

        switch (message.Type)
        {
            case RemoteMessageType.Hello:
                message.name = RemoteProtocol.CleanName(message.name);
                return message;
            case RemoteMessageType.Ping:
            case RemoteMessageType.Pong:
                return double.IsNaN(message.t) || double.IsInfinity(message.t) ? null : message;
            case RemoteMessageType.Command:
                return message.Command != RemoteCommand.None ? message : null;
            case RemoteMessageType.State:
                message.shape = RemoteProtocol.CleanText(message.shape, "");
                message.animation = RemoteProtocol.CleanText(message.animation, "");
                message.features &= (int)ShapeFeatures.All;
                return message;
            case RemoteMessageType.Shapes:
            case RemoteMessageType.Variants:
                message.names = CleanNames(message.names);
                bool fits = message.total >= 0 && message.total <= RemoteProtocol.MAX_LIST_NAMES
                    && message.first >= 0 && message.first + message.names.Length <= message.total;
                return fits ? message : null;
            case RemoteMessageType.Welcome:
            case RemoteMessageType.Full:
            case RemoteMessageType.Sensor:
                return message;
            default:
                return null;
        }
    }

    public static List<string> CreateListJsons(RemoteMessageType type, IReadOnlyList<string> names, int shapeIndex)
    {
        string[] all = CleanNames(names);
        List<string> jsons = new List<string>();

        RemoteMessage part = Create(type);
        part.shapeIndex = shapeIndex;
        part.total = all.Length;
        part.names = new string[0];
        int emptyLength = JsonUtility.ToJson(part).Length;

        int first = 0;
        do
        {
            int count = 0;
            int length = emptyLength;
            while (first + count < all.Length)
            {
                int nameLength = JsonLength(all[first + count]) + NAME_OVERHEAD_CHARS;
                if (count > 0 && length + nameLength > RemoteProtocol.MAX_MESSAGE_CHARS)
                {
                    break;
                }
                length += nameLength;
                count++;
            }

            part.first = first;
            part.names = new string[count];
            Array.Copy(all, first, part.names, 0, count);
            jsons.Add(JsonUtility.ToJson(part));
            first += count;
        }
        while (first < all.Length);

        return jsons;
    }

    public static string[] CleanNames(IReadOnlyList<string> names)
    {
        if (names == null)
        {
            return new string[0];
        }

        int count = Math.Min(names.Count, RemoteProtocol.MAX_LIST_NAMES);
        string[] clean = new string[count];
        for (int i = 0; i < count; i++)
        {
            clean[i] = RemoteProtocol.CleanText(names[i], RemoteProtocol.UNKNOWN_NAME);
        }
        return clean;
    }

    private static int JsonLength(string text)
    {
        int length = 0;
        foreach (char c in text)
        {
            if (c == '"' || c == '\\')
            {
                length += ESCAPED_CHARS;
            }
            else if (c > MAX_ASCII)
            {
                length += UNICODE_ESCAPED_CHARS;
            }
            else
            {
                length++;
            }
        }
        return length;
    }

    private static T FromName<T>(Dictionary<T, string> names, string text, T fallback)
    {
        foreach (KeyValuePair<T, string> entry in names)
        {
            if (entry.Value == text)
            {
                return entry.Key;
            }
        }
        return fallback;
    }
}
