//What kind of thing happened on the remote link (keep in sync with WebRemoteLink.jslib and WebRemoteLinkPlatform).
public enum PeerEventKind
{
    Loaded,      // a library finished loading
    Open,        // registered with the signaling server, Peer is the own ID
    Connected,   // messages can now be sent to Peer
    Data,        // Peer sent Data
    Closed,      // the connection to Peer ended
    Signaling,   // the signaling server was lost
    Error        // Peer is the error type, Data the message
}

//One thing that happened on the remote link since the last frame, as reported by IRemoteLinkPlatform.
public struct PeerEvent
{
    public PeerEventKind Kind;
    public string Peer;
    public string Data;
}
