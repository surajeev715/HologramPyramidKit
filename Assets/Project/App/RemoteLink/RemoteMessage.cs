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
    Command,   // remote -> host: command (and spin for SetSpin)
    State      // host -> remote: shape, spin, animation
}

//Commands a remote can give, they do the same as the buttons on the host.
public enum RemoteCommand
{
    None,   // missing or unknown command
    NextShape,
    SetSpin,
    NextAnimation
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
        { RemoteMessageType.State, "state" }
    };

    private static readonly Dictionary<RemoteCommand, string> COMMAND_NAMES = new Dictionary<RemoteCommand, string>
    {
        { RemoteCommand.NextShape, "nextShape" },
        { RemoteCommand.SetSpin, "setSpin" },
        { RemoteCommand.NextAnimation, "nextAnimation" }
    };

    public const float UNKNOWN_RTT = -1f;

    //Protocol version. Not set by default, so a received message without it is invalid.
    public int v;
    [SerializeField] private string type;
    public string name;
    public int seq;

    //clap and blow: happened since the previous sensor message. light: is on right now.
    public bool clap;
    public bool blow;
    public bool light;

    [SerializeField] private string command;

    //What the host shows right now. animation is empty if the shape has no animations to switch between.
    public string shape;
    public bool spin;
    public string animation;

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
        }
        catch (Exception)
        {
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
                return message;
            case RemoteMessageType.Welcome:
            case RemoteMessageType.Full:
            case RemoteMessageType.Sensor:
                return message;
            default:
                return null;
        }
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
