using System.Collections.Generic;

//Host side of the remote link: accepts remotes, merges what they detect into the host's SensorContext,
//runs their commands and tells them what the hologram shows.
public class RemoteHostSession : IRemoteSession
{
    public enum HostState { NotHosting, Starting, Hosting }

    //A connected remote and what it reported since the host last used it.
    public class ConnectedRemote
    {
        public string Name;
        public float RttInMilliseconds = RemoteMessage.UNKNOWN_RTT;

        internal float LastSeen;
        internal float RateWindowStart;
        internal int RateWindowCount;

        //Clap and blow are single moments: kept until the next frame used them, then cleared.
        internal bool PendingClap;
        internal bool PendingBlow;

        internal bool PendingLight;
        //Light is a state: kept until the remote reports a change.
        internal bool HasLight;

        internal string text;
    }

    private const float RATE_WINDOW_IN_SECONDS = 1f;

    private readonly IRemoteLinkPlatform _platform;
    private readonly string _pageUrl;
    private readonly IHologramControls _controls;
    private readonly Dictionary<string, ConnectedRemote> _remotes = new Dictionary<string, ConnectedRemote>();
    private readonly List<string> _timedOut = new List<string>();
    private bool _isReady;
    private int _nextRemoteNumber = 1;

    //What the remotes know, sent again when it changes. The two lists are sent on their own, only when they change.
    private string _sentShape;
    private bool _sentSpin;
    private string _sentVariant;
    private int _sentShapeIndex = RemoteProtocol.NO_INDEX;
    private int _sentVariantIndex = RemoteProtocol.NO_INDEX;
    private ShapeFeatures _sentFeatures;
    private bool _sentWordPerLine;
    private int _sentShapeCount = -1;
    private int _sentVariantsShapeIndex = RemoteProtocol.NO_INDEX;
    private int _sentVariantCount = -1;

    public HostState State { get; private set; } = HostState.NotHosting;

    //The link the QR code shows. Null while not hosting.
    public string JoinUrl { get; private set; }

    //True while the signaling server is unreachable: connected remotes keep working, new ones can not join.
    public bool IsSignalingLost { get; private set; }

    //Why hosting could not be started, null otherwise.
    public string Error { get; private set; }

    public IReadOnlyCollection<ConnectedRemote> Remotes => _remotes.Values;

    public RemoteHostSession(IRemoteLinkPlatform platform, string pageUrl, IHologramControls controls)
    {
        _platform = platform;
        _pageUrl = pageUrl;
        _controls = controls;
    }

    public void OnReady()
    {
        _isReady = true;
    }

    //Hosting only starts when the user asks for it, a hologram that is used alone never contacts a server.
    public void StartHosting()
    {
        if (!_isReady || State != HostState.NotHosting)
        {
            return;
        }

        byte[] randomBytes = new byte[RemoteProtocol.HOST_ID_RANDOM_CHARS];
        _platform.FillRandom(randomBytes);

        Error = null;
        State = HostState.Starting;
        _platform.StartHost(RemoteProtocol.CreateHostId(randomBytes), RemoteProtocol.MAX_MESSAGE_CHARS);
        _platform.KeepScreenOn();
    }

    public void HandleEvent(PeerEvent peerEvent, float now)
    {
        switch (peerEvent.Kind)
        {
            case PeerEventKind.Open:
                //Also arrives when the signaling server is back after it was lost.
                State = HostState.Hosting;
                IsSignalingLost = false;
                JoinUrl = RemoteProtocol.BuildJoinUrl(_pageUrl, peerEvent.Peer);
                break;

            case PeerEventKind.Connected:
                AcceptOrRefuse(peerEvent.Peer, now);
                break;

            case PeerEventKind.Data:
                HandleMessage(peerEvent.Peer, peerEvent.Data, now);
                break;

            case PeerEventKind.Closed:
                _remotes.Remove(peerEvent.Peer);
                break;

            case PeerEventKind.Signaling:
                IsSignalingLost = true;
                break;

            case PeerEventKind.Error:
                HandleError(peerEvent.Peer, peerEvent.Data);
                break;
        }
    }

    public void Tick(float now)
    {
        _timedOut.Clear();
        foreach (KeyValuePair<string, ConnectedRemote> entry in _remotes)
        {
            if (now - entry.Value.LastSeen > RemoteProtocol.PEER_TIMEOUT_IN_SECONDS)
            {
                _timedOut.Add(entry.Key);
            }
        }

        foreach (string peerId in _timedOut)
        {
            _remotes.Remove(peerId);
            _platform.Close(peerId, 0);
        }

        SendStateIfChanged();
    }

    //Remote input goes into the same context as local input: a clap on any device is a clap.
    public void Apply(SensorContext sensorContext, TextContext textContext, float now)
    {
        foreach (ConnectedRemote remote in _remotes.Values)
        {
            //While the host calibrates, the context stays empty. Claps and blows from that time are dropped.
            if (!sensorContext.IsCalibrating)
            {
                sensorContext.HasClap |= remote.PendingClap;
                sensorContext.HasBlow |= remote.PendingBlow;
                sensorContext.HasLight |= remote.PendingLight;
            }
            textContext.Text = remote.text;
            remote.PendingClap = false;
            remote.PendingBlow = false;
            remote.PendingLight = false;
        }
    }

    private void AcceptOrRefuse(string peerId, float now)
    {
        if (_remotes.Count >= RemoteProtocol.MAX_REMOTES)
        {
            _platform.Send(peerId, RemoteMessage.Create(RemoteMessageType.Full).ToJson());
            _platform.Close(peerId, RemoteProtocol.FULL_CLOSE_DELAY_IN_MILLISECONDS);
            return;
        }

        _remotes[peerId] = new ConnectedRemote
        {
            Name = "Remote " + _nextRemoteNumber++,
            LastSeen = now,
            RateWindowStart = now
        };
        _platform.Send(peerId, RemoteMessage.Create(RemoteMessageType.Welcome).ToJson());
        SendAll(peerId, CreateShapesJsons());
        SendAll(peerId, CreateVariantsJsons());
        _platform.Send(peerId, CreateState().ToJson());
    }

    //The remotes' controls show what the host shows, whoever changed it.
    private void SendStateIfChanged()
    {
        if (_remotes.Count == 0)
        {
            return;
        }

        int shapeCount = _controls.ShapeNames.Count;
        if (shapeCount != _sentShapeCount)
        {
            _sentShapeCount = shapeCount;
            BroadcastAll(CreateShapesJsons());
        }

        int shapeIndex = _controls.CurrentShapeIndex;
        int variantCount = _controls.VariantNames.Count;
        if (shapeIndex != _sentVariantsShapeIndex || variantCount != _sentVariantCount)
        {
            _sentVariantsShapeIndex = shapeIndex;
            _sentVariantCount = variantCount;
            BroadcastAll(CreateVariantsJsons());
        }

        string shape = _controls.CurrentShapeName;
        bool spin = _controls.IsSpinning;
        string variant = _controls.CurrentVariantName;
        int variantIndex = _controls.CurrentVariantIndex;
        ShapeFeatures features = _controls.CurrentFeatures;
        bool wordPerLine = _controls.IsWordPerLine;
        if (shape == _sentShape && spin == _sentSpin && variant == _sentVariant && shapeIndex == _sentShapeIndex
            && variantIndex == _sentVariantIndex && features == _sentFeatures && wordPerLine == _sentWordPerLine)
        {
            return;
        }
        _sentWordPerLine = wordPerLine;
        _sentShape = shape;
        _sentSpin = spin;
        _sentVariant = variant;
        _sentShapeIndex = shapeIndex;
        _sentVariantIndex = variantIndex;
        _sentFeatures = features;
        Broadcast(CreateState().ToJson());
    }

    private void Broadcast(string json)
    {
        foreach (string peerId in _remotes.Keys)
        {
            _platform.Send(peerId, json);
        }
    }

    private void BroadcastAll(List<string> jsons)
    {
        foreach (string json in jsons)
        {
            Broadcast(json);
        }
    }

    private void SendAll(string peerId, List<string> jsons)
    {
        foreach (string json in jsons)
        {
            _platform.Send(peerId, json);
        }
    }

    private RemoteMessage CreateState()
    {
        RemoteMessage state = RemoteMessage.Create(RemoteMessageType.State);
        state.shape = _controls.CurrentShapeName;
        state.spin = _controls.IsSpinning;
        state.animation = _controls.CurrentVariantName;
        state.shapeIndex = _controls.CurrentShapeIndex;
        state.variantIndex = _controls.CurrentVariantIndex;
        state.features = (int)_controls.CurrentFeatures;
        state.wordPerLine = _controls.IsWordPerLine;
        return state;
    }

    private List<string> CreateShapesJsons()
    {
        return RemoteMessage.CreateListJsons(RemoteMessageType.Shapes, _controls.ShapeNames, RemoteProtocol.NO_INDEX);
    }

    private List<string> CreateVariantsJsons()
    {
        return RemoteMessage.CreateListJsons(RemoteMessageType.Variants, _controls.VariantNames, _controls.CurrentShapeIndex);
    }

    private void RunCommand(RemoteMessage message)
    {
        switch (message.Command)
        {
            case RemoteCommand.SetSpin:
                _controls.SetSpinning(message.spin);
                break;
            case RemoteCommand.SelectShape:
                _controls.ShowShape(message.shapeIndex);
                break;
            case RemoteCommand.SelectVariant:
                _controls.SelectVariant(message.variantIndex);
                break;
            case RemoteCommand.SetWordPerLine:
                _controls.SetWordPerLine(message.wordPerLine);
                break;
        }
    }

    private void HandleMessage(string peerId, string json, float now)
    {
        if (!_remotes.TryGetValue(peerId, out ConnectedRemote remote) || IsRateLimited(remote, now))
        {
            return;
        }

        RemoteMessage message = RemoteMessage.Parse(json);
        if (message == null)
        {
            return;
        }

        remote.LastSeen = now;
        switch (message.Type)
        {
            case RemoteMessageType.Hello:
                remote.Name = message.name;
                break;

            case RemoteMessageType.Ping:
                remote.RttInMilliseconds = message.rtt;
                RemoteMessage pong = RemoteMessage.Create(RemoteMessageType.Pong);
                pong.t = message.t;
                _platform.Send(peerId, pong.ToJson());
                break;

            case RemoteMessageType.Sensor:
                remote.PendingClap |= message.clap;
                remote.PendingBlow |= message.blow;
                remote.PendingLight |= message.light;
                remote.text = message.text;
                break;

            case RemoteMessageType.Command:
                RunCommand(message);
                break;
        }
    }

    private static bool IsRateLimited(ConnectedRemote remote, float now)
    {
        if (now - remote.RateWindowStart >= RATE_WINDOW_IN_SECONDS)
        {
            remote.RateWindowStart = now;
            remote.RateWindowCount = 0;
        }
        remote.RateWindowCount++;
        return remote.RateWindowCount > RemoteProtocol.MAX_MESSAGES_PER_SECOND;
    }

    private void HandleError(string errorType, string message)
    {
        if (State == HostState.Starting)
        {
            //Hosting never started. Everything is closed, so the user can try again.
            Error = RemoteProtocol.DescribeError(errorType, message);
            State = HostState.NotHosting;
            _platform.Stop();
        }
        else if (RemoteProtocol.IsSignalingError(errorType))
        {
            IsSignalingLost = true;
        }
    }
}
