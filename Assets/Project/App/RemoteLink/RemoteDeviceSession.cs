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

    private bool _connectRequested;
    private bool _isChannelOpen;
    private float _connectStartTime;
    private float _lastPongTime;
    private float _nextPingTime;
    private float _rttInMilliseconds = RemoteMessage.UNKNOWN_RTT;

    private int _sequence;
    private bool _pendingClap;
    private bool _pendingBlow;
    private bool _lastSentLight;
    private float _lastSensorSendTime;

    private bool _manualClap;
    private bool _manualBlow;
    private bool _manualLight;
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
    public string HostAnimationName { get; private set; } = "";

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

    //The buttons only note what was tapped. It is sent in the next Apply, like everything else.
    public void RequestNextShape()
    {
        QueueCommand(RemoteCommand.NextShape, false);
    }

    public void RequestSetSpinning(bool value)
    {
        QueueCommand(RemoteCommand.SetSpin, value);
    }

    public void RequestNextAnimation()
    {
        QueueCommand(RemoteCommand.NextAnimation, false);
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

    public void Apply(SensorContext context, float now)
    {
        //Manual taps count exactly like the real sensors.
        if (_manualLight)
        {
            _manualLightEndTime = now + MANUAL_LIGHT_IN_SECONDS;
        }
        bool hasClap = context.HasClap || _manualClap;
        bool hasBlow = context.HasBlow || _manualBlow;
        bool hasLight = context.HasLight || now < _manualLightEndTime;
        _manualClap = false;
        _manualBlow = false;
        _manualLight = false;

        if (hasClap) LastClapTime = now;
        if (hasBlow) LastBlowTime = now;
        if (hasLight) LastLightTime = now;

        if (State != DeviceState.Connected)
        {
            _pendingClap = false;
            _pendingBlow = false;
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

        //Light is a state, not a moment: the host keeps the last value it got, so it is only sent when it changes.
        bool hasNews = _pendingClap || _pendingBlow || hasLight != _lastSentLight;
        bool maySend = now - _lastSensorSendTime >= RemoteProtocol.MIN_SENSOR_SEND_INTERVAL_IN_SECONDS;
        if (!hasNews || !maySend)
        {
            return;
        }

        RemoteMessage message = RemoteMessage.Create(RemoteMessageType.Sensor);
        message.seq = ++_sequence;
        message.clap = _pendingClap;
        message.blow = _pendingBlow;
        message.light = hasLight;
        Send(message);

        _pendingClap = false;
        _pendingBlow = false;
        _lastSentLight = hasLight;
        _lastSensorSendTime = now;
    }

    private void QueueCommand(RemoteCommand command, bool spin)
    {
        if (State != DeviceState.Connected || _queuedCommands.Count >= MAX_QUEUED_COMMANDS)
        {
            return;
        }

        RemoteMessage message = RemoteMessage.Create(RemoteMessageType.Command);
        message.Command = command;
        message.spin = spin;
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
                HostAnimationName = message.animation;
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
