using UnityEngine;

//The remotes part of the host's menu: the button that starts hosting, the QR code and the connected remotes.
//A remote has its own screen, see RemoteDeviceUI.
public class RemoteLinkUI
{
    private const float QR_MAX_SIZE_CM = 5.5f;
    private const float LIST_ROW_IN_ROWS = 0.7f;

    //White border the QR standard asks for, in modules.
    private const int QR_QUIET_ZONE = 4;
    private const char QR_DARK = '1';

    private readonly IRemoteLinkPlatform _platform;

    private Texture2D _qrTexture;
    private string _qrUrl;
    private bool _showDetails = true;

    public RemoteLinkUI(IRemoteLinkPlatform platform)
    {
        _platform = platform;
    }

    public void Draw(UITheme theme, RemoteLink link, float width)
    {
        theme.SectionLabel("Remotes");
        if (link.LoadError != null)
        {
            GUILayout.Label(link.LoadError, theme.Paragraph);
            return;
        }
        DrawHost(theme, link, link.Host, width);
    }

    private void DrawHost(UITheme theme, RemoteLink link, RemoteHostSession host, float width)
    {
        GUILayoutOption height = GUILayout.Height(theme.Row);
        switch (host.State)
        {
            case RemoteHostSession.HostState.NotHosting:
                //Read before the button: starting clears the error, and the controls must stay the same for this event.
                string error = host.Error;
                GUI.enabled = link.IsReady;
                if (GUILayout.Button(link.IsReady ? "Add remotes" : "Loading...", theme.AccentButton, height))
                {
                    host.StartHosting();
                }
                GUI.enabled = true;

                if (error != null)
                {
                    GUILayout.Space(theme.Gap);
                    theme.StatusLine("Could not start", UITheme.BAD, theme.Caption, theme.SectionLabelHeight);
                    GUILayout.Label(error, theme.Paragraph);
                }
                else
                {
                    GUILayout.Space(theme.Gap / 2f);
                    GUILayout.Label("Phones scan a QR code and become extra sensors.", theme.Paragraph);
                }
                return;

            case RemoteHostSession.HostState.Starting:
                GUI.enabled = false;
                GUILayout.Button("Starting...", theme.Button, height);
                GUI.enabled = true;
                return;
        }

        //A click only takes effect in the next GUI event: IMGUI needs the same controls for the whole event.
        bool showDetails = _showDetails;
        string countText = host.Remotes.Count + " / " + RemoteProtocol.MAX_REMOTES;
        if (theme.ValueRow(showDetails ? "Hide QR code" : "Show QR code", countText))
        {
            _showDetails = !_showDetails;
        }

        if (showDetails)
        {
            Texture2D qr = GetQrTexture(host.JoinUrl);
            if (qr != null)
            {
                GUILayout.Space(theme.Gap);
                float size = Mathf.Min(width, QR_MAX_SIZE_CM * ScreenDpi.PixelsPerCm);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Rect qrRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (Event.current.type == EventType.Repaint)
                {
                    //The QR texture has its own white border, the rounded white card behind it only softens the corners.
                    theme.DrawRounded(qrRect, Color.white);
                    float inset = theme.Gap / 2f;
                    GUI.DrawTexture(new Rect(qrRect.x + inset, qrRect.y + inset, qrRect.width - 2f * inset,
                        qrRect.height - 2f * inset), qr, ScaleMode.ScaleToFit);
                }
            }

            GUILayout.Space(theme.Gap);
            if (GUILayout.Button("Copy link", theme.Button, height))
            {
                _platform.CopyText(host.JoinUrl);
            }
        }

        if (host.IsSignalingLost)
        {
            GUILayout.Space(theme.Gap);
            theme.StatusLine("Server lost", UITheme.BUSY, theme.Caption, theme.SectionLabelHeight);
            GUILayout.Label("Connected remotes keep working, new ones can not join. Retrying...", theme.Paragraph);
        }

        if (host.Remotes.Count > 0)
        {
            GUILayout.Space(theme.Gap / 2f);
        }
        float rowHeight = theme.Row * LIST_ROW_IN_ROWS;
        foreach (RemoteHostSession.ConnectedRemote remote in host.Remotes)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(rowHeight));
            GUILayout.BeginVertical();
            theme.StatusLine(remote.Name, UITheme.GOOD, theme.Body, rowHeight);
            GUILayout.EndVertical();
            GUILayout.Label(FormatRtt(remote.RttInMilliseconds), theme.CaptionRight, GUILayout.Height(rowHeight));
            GUILayout.EndHorizontal();
        }
    }

    private static string FormatRtt(float rttInMilliseconds)
    {
        return rttInMilliseconds < 0f ? "" : Mathf.RoundToInt(rttInMilliseconds) + " ms";
    }

    //The texture is only built again when the link changes.
    private Texture2D GetQrTexture(string url)
    {
        if (url == _qrUrl)
        {
            return _qrTexture;
        }
        _qrUrl = url;

        if (_qrTexture != null)
        {
            Object.Destroy(_qrTexture);
            _qrTexture = null;
        }

        string modules = url == null ? null : _platform.GetQrModules(url);
        if (string.IsNullOrEmpty(modules))
        {
            return null;
        }

        //The code is square: one char per module, row by row.
        int count = Mathf.RoundToInt(Mathf.Sqrt(modules.Length));
        if (count * count != modules.Length)
        {
            return null;
        }

        int size = count + 2 * QR_QUIET_ZONE;
        Color32[] pixels = new Color32[size * size];
        Color32 light = Color.white;
        Color32 dark = Color.black;
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = light;
        }
        for (int row = 0; row < count; row++)
        {
            //Textures start at the bottom, the QR code at the top.
            int y = size - 1 - (row + QR_QUIET_ZONE);
            for (int column = 0; column < count; column++)
            {
                if (modules[row * count + column] == QR_DARK)
                {
                    pixels[y * size + column + QR_QUIET_ZONE] = dark;
                }
            }
        }

        //One pixel per module, drawn larger without smoothing so the edges stay sharp for the camera.
        _qrTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _qrTexture.filterMode = FilterMode.Point;
        _qrTexture.wrapMode = TextureWrapMode.Clamp;
        _qrTexture.SetPixels32(pixels);
        _qrTexture.Apply();
        return _qrTexture;
    }
}
