using System.Runtime.InteropServices;

//Remote link in the browser, see WebRemoteLink.jslib.
public class WebRemoteLinkPlatform : IRemoteLinkPlatform
{
    [DllImport("__Internal")] private static extern void WebRemoteLink_LoadScript(string url);
    [DllImport("__Internal")] private static extern int WebRemoteLink_IsReady();
    [DllImport("__Internal")] private static extern void WebRemoteLink_FillRandom(byte[] buffer, int length);
    [DllImport("__Internal")] private static extern void WebRemoteLink_StartHost(string hostId, int maxMessageChars);
    [DllImport("__Internal")] private static extern void WebRemoteLink_Connect(string hostId, int maxMessageChars);
    [DllImport("__Internal")] private static extern int WebRemoteLink_Send(string peerId, string text);
    [DllImport("__Internal")] private static extern void WebRemoteLink_Close(string peerId, int delayInMilliseconds);
    [DllImport("__Internal")] private static extern void WebRemoteLink_Stop();
    [DllImport("__Internal")] private static extern string WebRemoteLink_PollEvent();
    [DllImport("__Internal")] private static extern string WebRemoteLink_GetQrModules(string text);
    [DllImport("__Internal")] private static extern void WebRemoteLink_KeepScreenOn();
    [DllImport("__Internal")] private static extern void WebRemoteLink_CopyText(string text);

    //An event arrives as "kind\npeerId\ndata". Only the data can contain further line breaks.
    private const int EVENT_PART_COUNT = 3;
    private static readonly char[] EVENT_SEPARATOR = { '\n' };

    public void LoadScript(string url)
    {
        WebRemoteLink_LoadScript(url);
    }

    public bool IsReady()
    {
        return WebRemoteLink_IsReady() == 1;
    }

    public void FillRandom(byte[] buffer)
    {
        WebRemoteLink_FillRandom(buffer, buffer.Length);
    }

    public void StartHost(string hostId, int maxMessageChars)
    {
        WebRemoteLink_StartHost(hostId, maxMessageChars);
    }

    public void Connect(string hostId, int maxMessageChars)
    {
        WebRemoteLink_Connect(hostId, maxMessageChars);
    }

    public bool Send(string peerId, string text)
    {
        return WebRemoteLink_Send(peerId, text) == 1;
    }

    public void Close(string peerId, int delayInMilliseconds)
    {
        WebRemoteLink_Close(peerId, delayInMilliseconds);
    }

    public void Stop()
    {
        WebRemoteLink_Stop();
    }

    public bool TryPollEvent(out PeerEvent peerEvent)
    {
        peerEvent = default;

        string raw = WebRemoteLink_PollEvent();
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        string[] parts = raw.Split(EVENT_SEPARATOR, EVENT_PART_COUNT);
        if (parts.Length != EVENT_PART_COUNT || !TryParseKind(parts[0], out PeerEventKind kind))
        {
            return false;
        }

        peerEvent.Kind = kind;
        peerEvent.Peer = parts[1];
        peerEvent.Data = parts[2];
        return true;
    }

    private static bool TryParseKind(string text, out PeerEventKind kind)
    {
        switch (text)
        {
            case "loaded": kind = PeerEventKind.Loaded; return true;
            case "open": kind = PeerEventKind.Open; return true;
            case "connected": kind = PeerEventKind.Connected; return true;
            case "data": kind = PeerEventKind.Data; return true;
            case "closed": kind = PeerEventKind.Closed; return true;
            case "signaling": kind = PeerEventKind.Signaling; return true;
            case "error": kind = PeerEventKind.Error; return true;
            default: kind = default; return false;
        }
    }

    public string GetQrModules(string text)
    {
        return WebRemoteLink_GetQrModules(text);
    }

    public void KeepScreenOn()
    {
        WebRemoteLink_KeepScreenOn();
    }

    public void CopyText(string text)
    {
        WebRemoteLink_CopyText(text);
    }
}
