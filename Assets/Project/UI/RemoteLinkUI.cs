using UnityEngine;

//Top right corner of the host's screen: the button that starts hosting, the QR code and the connected remotes.
//A remote has its own screen, see RemoteDeviceUI.
public class RemoteLinkUI
{
    //Same button size as ShapeSwitchUI.
    private const float buttonWidthCm = 3.5f;
    private const float buttonHeightCm = 1f;
    private const float buttonMarginCm = 0.3f;
    private const float qrSizeCm = 4f;

    private const float FALLBACK_DPI = 96f;
    private const float CM_PER_INCH = 2.54f;
    private const float BUTTON_FONT_SCALE = 0.35f;
    private const float LABEL_FONT_SCALE = 0.28f;

    //White border the QR standard asks for, in modules.
    private const int QR_QUIET_ZONE = 4;
    private const char QR_DARK = '1';

    private readonly IRemoteLinkPlatform _platform;

    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;

    private Texture2D _qrTexture;
    private string _qrUrl;
    private bool _showDetails = true;

    public RemoteLinkUI(IRemoteLinkPlatform platform)
    {
        _platform = platform;
    }

    public void Draw(RemoteLink link)
    {
        float dpi = Screen.dpi > 0 ? Screen.dpi : FALLBACK_DPI;
        float pixelsPerCm = dpi / CM_PER_INCH;

        float width = buttonWidthCm * pixelsPerCm;
        float height = buttonHeightCm * pixelsPerCm;
        float margin = buttonMarginCm * pixelsPerCm;

        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;

            //Names and messages come from other devices, so they are never read as rich text.
            _labelStyle = new GUIStyle(GUI.skin.box);
            _labelStyle.alignment = TextAnchor.UpperLeft;
            _labelStyle.wordWrap = true;
            _labelStyle.richText = false;
        }
        _buttonStyle.fontSize = Mathf.RoundToInt(height * BUTTON_FONT_SCALE);
        _labelStyle.fontSize = Mathf.RoundToInt(height * LABEL_FONT_SCALE);

        float panelWidth = Mathf.Max(width, qrSizeCm * pixelsPerCm);
        Rect area = new Rect(Screen.width - panelWidth - margin, margin, panelWidth, Screen.height - 2 * margin);

        GUILayout.BeginArea(area);
        if (link.LoadError != null)
        {
            GUILayout.Label(link.LoadError, _labelStyle);
        }
        else
        {
            DrawHost(link, link.Host, panelWidth, height);
        }
        GUILayout.EndArea();
    }

    private void DrawHost(RemoteLink link, RemoteHostSession host, float panelWidth, float buttonHeight)
    {
        switch (host.State)
        {
            case RemoteHostSession.HostState.NotHosting:
                GUI.enabled = link.IsReady;
                if (GUILayout.Button(link.IsReady ? "Add remotes" : "Loading...", _buttonStyle, GUILayout.Height(buttonHeight)))
                {
                    host.StartHosting();
                }
                GUI.enabled = true;

                if (host.Error != null)
                {
                    GUILayout.Label(host.Error, _labelStyle);
                }
                return;

            case RemoteHostSession.HostState.Starting:
                GUI.enabled = false;
                GUILayout.Button("Starting...", _buttonStyle, GUILayout.Height(buttonHeight));
                GUI.enabled = true;
                return;
        }

        //A click only takes effect in the next GUI event: IMGUI needs the same controls for the whole event.
        bool showDetails = _showDetails;
        string countText = "Remotes: " + host.Remotes.Count + " / " + RemoteProtocol.MAX_REMOTES;
        if (GUILayout.Button(countText, _buttonStyle, GUILayout.Height(buttonHeight)))
        {
            _showDetails = !_showDetails;
        }
        if (!showDetails)
        {
            return;
        }

        Texture2D qr = GetQrTexture(host.JoinUrl);
        if (qr != null)
        {
            Rect qrRect = GUILayoutUtility.GetRect(panelWidth, panelWidth);
            GUI.DrawTexture(qrRect, qr, ScaleMode.ScaleToFit);
        }

        if (GUILayout.Button("Copy link", _buttonStyle, GUILayout.Height(buttonHeight)))
        {
            _platform.CopyText(host.JoinUrl);
        }

        if (host.IsSignalingLost)
        {
            GUILayout.Label("Server lost. Connected remotes keep working, new ones can not join. Retrying...", _labelStyle);
        }

        foreach (RemoteHostSession.ConnectedRemote remote in host.Remotes)
        {
            GUILayout.Label(remote.Name + FormatRtt(remote.RttInMilliseconds), _labelStyle);
        }
    }

    private static string FormatRtt(float rttInMilliseconds)
    {
        return rttInMilliseconds < 0f ? "" : "  (" + Mathf.RoundToInt(rttInMilliseconds) + " ms)";
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
