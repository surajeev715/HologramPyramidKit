using System.Collections.Generic;

//Remote side of the remote link: this device joined a host through its QR code. It sends the host what its own
//sensors detect and the commands from its buttons. It never changes its own SensorContext.
public class RemoteDeviceSession : IRemoteSession
{
    public enum DeviceState { Loading, Connecting, Connected, Failed }

    private const float SECONDS_TO_MILLISECONDS = 1000f;
    private const int NAME_NUMBER_MIN = 10;
    private const int NAME_NUMBER_COUNT = 90;

    //A tap on the manual light button counts as light for this long, enough for the host to see it.
    private const float MANUAL_LIGHT_IN_SECONDS = 0.5f;

    //Taps that are waiting to be sent. More than a few can only pile up while the connection is stuck.
    private const int MAX_QUEUED_COMMANDS = 8;

    private readonly IRemoteLinkPlatform _platform;
    private readonly string _hostId;
    private readonly bool _isHostIdValid;
    private readonly List<RemoteMessage> _queuedCommands = new List<RemoteMessage>();

    private static readonly string[] NO_NAMES = new string[0];

    private readonly RemoteListParts _shapeNames = new RemoteListParts();
    private readonly RemoteListParts _variantNames = new RemoteListParts();

    private bool _connectRequested;
    private bool _isChannelOpen;
    private float _connectStartTime;
    private float _lastPongTime;
    private float _nextPingTime;
    private float _rttInMilliseconds = RemoteMessage.UNKNOWN_RTT;

    private int _sequence;
    private bool _pendingClap;
    private bool _pendingBlow;

    private bool _pendingLight;

    private bool _previousLocalLight;
    private bool _lastSentLight;
    private float _lastSensorSendTime;

    private bool _manualClap;
    private bool _manualBlow;
    private bool _manualLight;
    private string _text;
    private float _manualLightEndTime = float.NegativeInfinity;

    public DeviceState State { get; private set; } = DeviceState.Loading;

    public string Name { get; private set; }

    //Short text for the state, and a longer hint what to do about it.
    public string Status { get; private set; } = "Loading...";
    public string Hint { get; private set; } = "";

    //False if trying again can not help (broken join link).
    public bool CanReconnect => State == DeviceState.Failed && _isHostIdValid;

    public float RttInMilliseconds => _rttInMilliseconds;

    //What the host's hologram shows, as last reported by the host.
    public string HostShapeName { get; private set; } = "";
    public bool IsHostSpinning { get; private set; }
    public string HostVariantName { get; private set; } = "";
    public IReadOnlyList<string> HostShapeNames => _shapeNames.Names;
    public bool IsHostWordPerLine { get; private set; }
    public int HostShapeIndex { get; private set; } = RemoteProtocol.NO_INDEX;
    public int HostVariantIndex { get; private set; } = RemoteProtocol.NO_INDEX;
    public ShapeFeatures HostFeatures { get; private set; } = ShapeFeatures.None;

    public IReadOnlyList<string> HostVariantNames => _variantNames.ShapeIndex == HostShapeIndex ? _variantNames.Names : NO_NAMES;

    //When this device last detected (or manually triggered) each input, on the clock passed to Apply.
    public float LastClapTime { get; private set; } = float.NegativeInfinity;
    public float LastBlowTime { get; private set; } = float.NegativeInfinity;
    public float LastLightTime { get; private set; } = float.NegativeInfinity;

    public RemoteDeviceSession(IRemoteLinkPlatform platform, string hostId)
    {
        _platform = platform;
        _hostId = hostId;
        _isHostIdValid = RemoteProtocol.IsValidHostId(hostId);

        if (!_isHostIdValid)
        {
            Fail("Invalid join link", "Scan the QR code on the host again.");
        }
    }

    public void OnReady()
    {
        byte[] random = new byte[1];
        _platform.FillRandom(random);
        Name = "Phone " + (NAME_NUMBER_MIN + random[0] % NAME_NUMBER_COUNT);

        Reconnect();
    }

    //The connection is opened in the next Tick, so that all timers start from the same clock.
    public void Reconnect()
    {
        if (_isHostIdValid && Name != null && State != DeviceState.Connecting && State != DeviceState.Connected)
        {
            _connectRequested = true;
        }
    }

    //The controls only note what was tapped. It is sent in the next Apply, like everything else.
    public void RequestSelectShape(int index)
    {
        RemoteMessage message = CreateCommand(RemoteCommand.SelectShape);
        message.shapeIndex = index;
        QueueCommand(message);
    }

    public void RequestSetSpinning(bool value)
    {
        RemoteMessage message = CreateCommand(RemoteCommand.SetSpin);
        message.spin = value;
        QueueCommand(message);
    }

    public void RequestSetWordPerLine(bool value)
    {
        RemoteMessage message = CreateCommand(RemoteCommand.SetWordPerLine);
        message.wordPerLine = value;
        QueueCommand(message);
    }

    public void RequestSelectVariant(int index)
    {
        RemoteMessage message = CreateCommand(RemoteCommand.SelectVariant);
        message.variantIndex = index;
        QueueCommand(message);
    }

    public void TriggerClap()
    {
        _manualClap = true;
    }

    public void TriggerBlow()
    {
        _manualBlow = true;
    }

    public void TriggerLight()
    {
        _manualLight = true;
    }

    public void SetText(string text)
    {
        _text = text;
    }


    public void HandleEvent(PeerEvent peerEvent, float now)
    {
        //Loading: nothing was opened yet. Failed: the connection was already stopped. The platform drops old events
        //on Stop, this only makes sure a late one can not replace the reason it failed with.
        if (State != DeviceState.Connecting && State != DeviceState.Connected)
        {
            return;
        }

        switch (peerEvent.Kind)
        {
            case PeerEventKind.Connected:
                _isChannelOpen = true;
                _lastPongTime = now;
                _nextPingTime = now + RemoteProtocol.PING_INTERVAL_IN_SECONDS;
                RemoteMessage hello = RemoteMessage.Create(RemoteMessageType.Hello);
                hello.name = Name;
                Send(hello);
                break;

            case PeerEventKind.Data:
                HandleMessage(peerEvent.Data, now);
                break;

            case PeerEventKind.Closed:
                Fail("Disconnected", "The host closed the connection.");
                break;

            case PeerEventKind.Error:
                //Once connected, the signaling server is no longer needed.
                bool canIgnore = State == DeviceState.Connected && RemoteProtocol.IsSignalingError(peerEvent.Peer);
                if (!canIgnore)
                {
                    Fail("Error", RemoteProtocol.DescribeError(peerEvent.Peer, peerEvent.Data));
                }
                break;
        }
    }

    public void Tick(float now)
    {
        if (_connectRequested)
        {
            _connectRequested = false;
            StartConnecting(now);
            return;
        }

        if (State == DeviceState.Connecting && (now - _connectStartTime) > RemoteProtocol.CONNECT_TIMEOUT_IN_SECONDS)
        {
            Fail("Could not connect",
                "The devices could not reach each other directly. Guest Wi-Fi, eduroam and some routers block " +
                "device-to-device traffic. Try a phone hotspot.");
            return;
        }

        //Not an error: still loading, still waiting for the channel (the timeout above covers that), or already failed.
        if (!_isChannelOpen)
        {
            return;
        }

        if (now - _lastPongTime > RemoteProtocol.PEER_TIMEOUT_IN_SECONDS)
        {
            Fail("Host lost", "The host did not answer for " + RemoteProtocol.PEER_TIMEOUT_IN_SECONDS + " s.");
            return;
        }

        if (now >= _nextPingTime)
        {
            _nextPingTime = now + RemoteProtocol.PING_INTERVAL_IN_SECONDS;
            RemoteMessage ping = RemoteMessage.Create(RemoteMessageType.Ping);
            ping.t = (double)now * SECONDS_TO_MILLISECONDS;
            ping.rtt = _rttInMilliseconds;
            Send(ping);
        }
    }

    public void Apply(SensorContext sensorContext, TextContext textContext, float now)
    {
        //Manual taps count exactly like the real sensors.
        bool lightTriggered = (sensorContext.HasLight && !_previousLocalLight) || _manualLight;
        _previousLocalLight = sensorContext.HasLight;
        bool hasClap = sensorContext.HasClap || _manualClap;
        bool hasBlow = sensorContext.HasBlow || _manualBlow;
        _manualClap = false;
        _manualBlow = false;
        _manualLight = false;


        if (hasClap) LastClapTime = now;
        if (hasBlow) LastBlowTime = now;
        if (lightTriggered) LastLightTime = now;

        if (State != DeviceState.Connected)
        {
            _pendingClap = false;
            _pendingBlow = false;
            _pendingLight = false;
            _queuedCommands.Clear();
            return;
        }

        foreach (RemoteMessage command in _queuedCommands)
        {
            Send(command);
        }
        _queuedCommands.Clear();

        //A clap lasts one frame but messages are sent less often, so it is remembered until the next one goes out.
        _pendingClap |= hasClap;
        _pendingBlow |= hasBlow;
        _pendingLight |= lightTriggered;

        //Light is a state, not a moment: the host keeps the last value it got, so it is only sent when it changes.
        bool hasNews = _pendingClap || _pendingBlow || _pendingLight || (_text != textContext.Text);
        bool maySend = now - _lastSensorSendTime >= RemoteProtocol.MIN_SENSOR_SEND_INTERVAL_IN_SECONDS;
        if (!hasNews || !maySend)
        {
            return;
        }

        //update text
        if (_text == null || (_text != textContext.Text))
        {
            _text = textContext.Text;
        }

        RemoteMessage message = RemoteMessage.Create(RemoteMessageType.Sensor);
        message.seq = ++_sequence;
        message.clap = _pendingClap;
        message.blow = _pendingBlow;
        message.light = _pendingLight;
        message.text = _text;
        Send(message);

        _pendingClap = false;
        _pendingBlow = false;
        _pendingLight = false;
        _lastSensorSendTime = now;
    }

    private static RemoteMessage CreateCommand(RemoteCommand command)
    {
        RemoteMessage message = RemoteMessage.Create(RemoteMessageType.Command);
        message.Command = command;
        return message;
    }

    private void QueueCommand(RemoteMessage message)
    {
        if (State != DeviceState.Connected || _queuedCommands.Count >= MAX_QUEUED_COMMANDS)
        {
            return;
        }
        _queuedCommands.Add(message);
    }

    private void StartConnecting(float now)
    {
        _isChannelOpen = false;
        _lastSentLight = false;
        _rttInMilliseconds = RemoteMessage.UNKNOWN_RTT;
        _connectStartTime = now;
        State = DeviceState.Connecting;
        Status = "Connecting...";
        Hint = "";
        _platform.Connect(_hostId, RemoteProtocol.MAX_MESSAGE_CHARS);
    }

    private void HandleMessage(string json, float now)
    {
        RemoteMessage message = RemoteMessage.Parse(json);
        if (message == null)
        {
            return;
        }

        switch (message.Type)
        {
            case RemoteMessageType.Welcome:
                State = DeviceState.Connected;
                Status = "Connected as " + Name;
                Hint = "";
                _platform.KeepScreenOn();
                break;

            case RemoteMessageType.Full:
                Fail("Host is full", "The host already has " + RemoteProtocol.MAX_REMOTES + " remotes.");
                break;

            case RemoteMessageType.Pong:
                _lastPongTime = now;
                _rttInMilliseconds = (float)((double)now * SECONDS_TO_MILLISECONDS - message.t);
                break;

            case RemoteMessageType.State:
                HostShapeName = message.shape;
                IsHostSpinning = message.spin;
                HostVariantName = message.animation;
                HostShapeIndex = message.shapeIndex;
                HostVariantIndex = message.variantIndex;
                HostFeatures = (ShapeFeatures)message.features;
                IsHostWordPerLine = message.wordPerLine;
                break;

            case RemoteMessageType.Shapes:
                _shapeNames.Add(message);
                break;

            case RemoteMessageType.Variants:
                _variantNames.Add(message);
                break;
        }
    }

    private void Send(RemoteMessage message)
    {
        _platform.Send(_hostId, message.ToJson());
    }

    private void Fail(string status, string hint)
    {
        State = DeviceState.Failed;
        Status = status;
        Hint = hint;
        _isChannelOpen = false;
        _platform.Stop();
    }
}
