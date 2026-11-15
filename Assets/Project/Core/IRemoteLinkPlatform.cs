//Everything the remote link needs from the platform it runs on.
//Implementations only transport and measure, they never decide what a message means.
public interface IRemoteLinkPlatform
{
    //Starts loading a library. A LOADED or ERROR event follows.
    void LoadScript(string url);

    //True once all libraries are loaded and the platform supports direct connections.
    bool IsReady();

    //Fills the buffer with cryptographically secure random bytes.
    void FillRandom(byte[] buffer);

    //Registers as a host under the given ID. An OPEN event follows once remotes can join.
    void StartHost(string hostId, int maxMessageChars);

    //Connects as a remote to a host. A CONNECTED event follows once messages can be sent.
    void Connect(string hostId, int maxMessageChars);

    //Returns false if the peer is not connected.
    bool Send(string peerId, string text);

    //Closes the connection to one peer after the delay. No CLOSED event follows for it.
    void Close(string peerId, int delayInMilliseconds);

    //Closes everything and throws away the events that were not collected yet.
    void Stop();

    //Returns false if nothing happened since the last call.
    bool TryPollEvent(out PeerEvent peerEvent);

    //QR code for a text as rows of '1' (dark) and '0' (light) in one string, or null if it failed.
    string GetQrModules(string text);

    void KeepScreenOn();

    void CopyText(string text);
}
