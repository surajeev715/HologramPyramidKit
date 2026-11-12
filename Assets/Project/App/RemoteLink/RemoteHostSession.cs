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
        //Light is a state: kept until the remote reports a change.
        internal bool HasLight;
    }

    private const float RATE_WINDOW_IN_SECONDS = 1f;

    private readonly IRemoteLinkPlatform _platform;
    private readonly string _pageUrl;
    private readonly IHologramControls _controls;
    private readonly Dictionary<string, ConnectedRemote> _remotes = new Dictionary<string, ConnectedRemote>();
    private readonly List<string> _timedOut = new List<string>();
    private bool _isReady;
    private int _nextRemoteNumber = 1;

    //The state the remotes know, sent again when it changes.
    private string _sentShape;
    private bool _sentSpin;
    private string _sentAnimation;

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
    public void Apply(SensorContext context, float now)
    {
        foreach (ConnectedRemote remote in _remotes.Values)
        {
            //While the host calibrates, the context stays empty. Claps and blows from that time are dropped.
            if (!context.IsCalibrating)
            {
                context.HasClap |= remote.PendingClap;
                context.HasBlow |= remote.PendingBlow;
                context.HasLight |= remote.HasLight;
            }
            remote.PendingClap = false;
            remote.PendingBlow = false;
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
        _platform.Send(peerId, CreateState().ToJson());
    }

    //The remotes' buttons show what the host shows, whoever changed it.
    private void SendStateIfChanged()
    {
        if (_remotes.Count == 0)
        {
            return;
        }

        RemoteMessage state = CreateState();
        if (state.shape == _sentShape && state.spin == _sentSpin && state.animation == _sentAnimation)
        {
            return;
        }
        _sentShape = state.shape;
        _sentSpin = state.spin;
        _sentAnimation = state.animation;

        string json = state.ToJson();
        foreach (string peerId in _remotes.Keys)
        {
            _platform.Send(peerId, json);
        }
    }

    private RemoteMessage CreateState()
    {
        RemoteMessage state = RemoteMessage.Create(RemoteMessageType.State);
        state.shape = _controls.CurrentShapeName;
        state.spin = _controls.IsSpinning;
        state.animation = _controls.CurrentAnimationName;
        return state;
    }

    private void RunCommand(RemoteMessage message)
    {
        switch (message.Command)
        {
            case RemoteCommand.NextShape:
                _controls.NextShape();
                break;
            case RemoteCommand.SetSpin:
                _controls.SetSpinning(message.spin);
                break;
            case RemoteCommand.NextAnimation:
                _controls.NextAnimation();
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
                remote.HasLight = message.light;
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
