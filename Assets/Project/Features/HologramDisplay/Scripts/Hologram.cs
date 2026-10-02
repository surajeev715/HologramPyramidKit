using UnityEngine;
using System;

public class Hologram : MonoBehaviour
{
    public Camera[] cameras;

    public float hologramArea = 1f;

    public enum Direction
    {
        topDown, bottomUp
    };

    public Direction type = new Direction();

    [Header("Pyramid Guide")]
    [Tooltip("Side length of the pyramid's small opening in centimeters.")]
    public float squareSizeCm = 1.8f;

    [Tooltip("Width of the white guide outline in pixels.")]
    public int lineWidth = 2;

    [Tooltip("Calibration: overrides Screen.dpi when greater than 0. Measure the square with a ruler and adjust if it is off.")]
    public float dpiOverride = 0f;

    [Tooltip("Shader used to draw the four views onto the screen.")]
    public Shader viewShader;

    [Header("Shape Size")]
    [Tooltip("Space between the center square and the shape, as a fraction of each side's depth.")]
    [Range(0f, 0.5f)]
    public float shapeGap = 0.29f;

    [Tooltip("Space between the shape and the screen edge, as a fraction of each side's depth.")]
    [Range(0f, 0.5f)]
    public float shapeEdgeMargin = 0.03f;

    [Tooltip("Half the height of the shapes in world units.")]
    public float shapeHalfHeight = 0.5f;

    [Tooltip("Largest distance of the shapes from their vertical axis in world units, e.g. a spinning cube's corners.")]
    public float shapeRadius = 0.71f;

    RenderTexture[] viewTextures;
    Material viewMaterial;

    void OnValidate()
    {
        //These methods only get called when an inspector value for the script has been modified.
        Scale();
        CameraClipping();
        CameraRotation();

        //In play mode the views are rendered to textures and drawn in OnGUI, so the camera rects are only an editor preview.
        if (!Application.isPlaying)
        {
            CameraRect(GetMainGameViewSize());
        }
        else if (viewTextures != null && viewTextures[1] != null)
        {
            CameraProjection(SquareSizePixels(), viewTextures[1].height);
        }
    }

    void Start()
    {
        cameras[0].backgroundColor = Color.black;
        viewTextures = new RenderTexture[cameras.Length];
        viewMaterial = new Material(viewShader != null ? viewShader : Shader.Find("Hidden/Hologram/ViewComposite"));
        UpdateViewTextures();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Update()
    {
        //Recreates the view textures when the screen size changes, e.g. after a rotation.
        UpdateViewTextures();
    }

    void OnDestroy()
    {
        if (viewTextures != null)
        {
            for (int i = 1; i < viewTextures.Length; i++)
            {
                ReleaseViewTexture(i);
            }
        }

        if (viewMaterial != null)
        {
            Destroy(viewMaterial);
        }
    }

    void Scale()
    {
        transform.localScale = new Vector3(hologramArea, hologramArea, hologramArea);
    }

    void CameraClipping()
    {
        float centerToEdge = hologramArea / 2f;
        float EdgeToCamera = centerToEdge / (float)(Math.Tan(30 * (Math.PI / 180)));

        for (int i = 1; i < cameras.Length; i++)
        {
            cameras[i].farClipPlane = EdgeToCamera + hologramArea;
            cameras[i].nearClipPlane = EdgeToCamera;
        }
    }

    void CameraRotation()
    {
        if (type == Direction.topDown)
        {
            cameras[1].transform.eulerAngles = new Vector3(0, 0, 0);
            cameras[2].transform.eulerAngles = new Vector3(0, 180, 180);
        }
        else if (type == Direction.bottomUp)
        {
            cameras[1].transform.eulerAngles = new Vector3(0, 0, 180);
            cameras[2].transform.eulerAngles = new Vector3(0, 180, 0);
        }

        cameras[3].transform.eulerAngles = new Vector3(0, 90, 90);
        cameras[4].transform.eulerAngles = new Vector3(0, 270, 270);
    }

    void CameraRect(Vector2 res)
    {
        //Editor preview only: each view covers the bounding box of its pyramid face.
        //The corners overlap here, which is resolved in play mode by drawing each view as a trapezoid.
        float squarePixels = SquareSizePixels();
        float viewPixels = ViewSizePixels(res, squarePixels);
        float facePixels = squarePixels + 2f * viewPixels;

        CameraProjection(squarePixels, viewPixels);

        float squareHalfX = squarePixels / 2f / res.x;
        float squareHalfY = squarePixels / 2f / res.y;

        Rect left = new Rect(1 / 2f - squareHalfX - viewPixels / res.x, 1 / 2f - facePixels / 2f / res.y, viewPixels / res.x, facePixels / res.y);
        Rect right = new Rect(1 / 2f + squareHalfX, 1 / 2f - facePixels / 2f / res.y, viewPixels / res.x, facePixels / res.y);

        cameras[1].rect = new Rect(1 / 2f - facePixels / 2f / res.x, 1 / 2f - squareHalfY - viewPixels / res.y, facePixels / res.x, viewPixels / res.y);
        cameras[2].rect = new Rect(1 / 2f - facePixels / 2f / res.x, 1 / 2f + squareHalfY, facePixels / res.x, viewPixels / res.y);

        if (type == Direction.topDown)
        {
            cameras[3].rect = left;
            cameras[4].rect = right;
        }
        else if (type == Direction.bottomUp)
        {
            cameras[3].rect = right;
            cameras[4].rect = left;
        }
    }

    float SquareSizePixels()
    {
        //The square has a fixed physical size on every device. Only the views around it grow with the screen.
        ScreenDpi.overrideDpi = dpiOverride;
        return Mathf.Round(squareSizeCm * ScreenDpi.PixelsPerCm);
    }

    float ViewSizePixels(Vector2 res, float squarePixels)
    {
        //Each view fills the space between the center square and the closest screen edge.
        return Mathf.Max(Mathf.Floor((Mathf.Min(res.x, res.y) - squarePixels) / 2f), 0f);
    }

    void CameraProjection(float squarePixels, float viewPixels)
    {
        //The shape keeps shapeGap to the square and grows toward the screen edge until shapeEdgeMargin is left.
        //Every view gets the same pixel size and position, so the shape looks the same on all four faces.
        float facePixels = squarePixels + 2f * viewPixels;
        float gapPixels = shapeGap * viewPixels;
        float shapePixels = viewPixels * (1f - shapeGap - shapeEdgeMargin);

        //The widest part of the shape can be at its near end, where the face is narrowest, so it is kept inside the face's diagonals.
        shapePixels = Mathf.Min(shapePixels, (squarePixels + 2f * gapPixels) * shapeHalfHeight / shapeRadius);
        shapePixels = Mathf.Max(shapePixels, 1f);

        float centerPixels = gapPixels + shapePixels / 2f;

        for (int i = 1; i < cameras.Length; i++)
        {
            Camera viewCamera = cameras[i];
            float distance = Vector3.Distance(viewCamera.transform.position, transform.position);

            //Parts of the shape in front of its center look bigger, so its height is measured at its closest point.
            float closest = Mathf.Max(distance - shapeRadius, viewCamera.nearClipPlane);
            float pixelsPerUnit = shapePixels / (2f * shapeHalfHeight * distance / closest);

            //Extents of the view at the shape's center in world units. The frustum is off-center so the shape can sit closer to the square than to the edge.
            float along = facePixels / 2f / pixelsPerUnit;
            float towardSquare = centerPixels / pixelsPerUnit;
            float towardEdge = (viewPixels - centerPixels) / pixelsPerUnit;

            Vector2 square = SquareDirection(i);
            float left, right, bottom, top;

            if (square.x == 0f)
            {
                left = -along;
                right = along;
                bottom = square.y > 0f ? -towardEdge : -towardSquare;
                top = square.y > 0f ? towardSquare : towardEdge;
            }
            else
            {
                bottom = -along;
                top = along;
                left = square.x > 0f ? -towardEdge : -towardSquare;
                right = square.x > 0f ? towardSquare : towardEdge;
            }

            float nearScale = viewCamera.nearClipPlane / distance;
            viewCamera.projectionMatrix = Matrix4x4.Frustum(left * nearScale, right * nearScale, bottom * nearScale, top * nearScale, viewCamera.nearClipPlane, viewCamera.farClipPlane);
        }
    }

    Vector2 SquareDirection(int i)
    {
        //Direction of the center square in each camera's image, matching how DrawFace and CameraRect place the views.
        switch (i)
        {
            case 1: return Vector2.up;
            case 2: return Vector2.down;
            case 3: return type == Direction.topDown ? Vector2.right : Vector2.left;
            default: return type == Direction.topDown ? Vector2.left : Vector2.right;
        }
    }

    void UpdateViewTextures()
    {
        int square = (int)SquareSizePixels();
        int view = (int)ViewSizePixels(new Vector2(Screen.width, Screen.height), square);
        int face = square + 2 * view;

        if (view < 1 || (viewTextures[1] != null && viewTextures[1].width == face && viewTextures[1].height == view))
        {
            return;
        }

        CameraProjection(square, view);

        for (int i = 1; i < cameras.Length; i++)
        {
            ReleaseViewTexture(i);

            //Top and bottom views are wide, left and right views are tall.
            viewTextures[i] = i <= 2 ? new RenderTexture(face, view, 24) : new RenderTexture(view, face, 24);
            cameras[i].rect = new Rect(0, 0, 1, 1);
            cameras[i].targetTexture = viewTextures[i];
        }
    }

    void ReleaseViewTexture(int i)
    {
        if (viewTextures[i] == null)
        {
            return;
        }

        cameras[i].targetTexture = null;
        viewTextures[i].Release();
        Destroy(viewTextures[i]);
        viewTextures[i] = null;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        float size = SquareSizePixels();
        float x = Mathf.Round((Screen.width - size) / 2f);
        float y = Mathf.Round((Screen.height - size) / 2f);

        //GUI coordinates start top-left, GL pixel coordinates start bottom-left.
        DrawViews(new Vector2(x + size / 2f, Screen.height - y - size / 2f), size / 2f);

        //Draws the white outline of the center square. The outline lies inside the square so its outer edge is exactly squareSizeCm.
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x, y, size, lineWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + size - lineWidth, size, lineWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, lineWidth, size), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + size - lineWidth, y, lineWidth, size), Texture2D.whiteTexture);
    }

    void DrawViews(Vector2 center, float squareHalf)
    {
        if (viewMaterial == null || viewTextures == null || viewTextures[1] == null)
        {
            return;
        }

        float view = viewTextures[1].height;
        Vector2[] face = FacePolygon(squareHalf, view);

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, Screen.width, 0, Screen.height);

        //Each face is described by the direction along the square's edge and the direction pointing away from the center.
        DrawFace(viewTextures[1], face, center, squareHalf, view, Vector2.right, Vector2.down);
        DrawFace(viewTextures[2], face, center, squareHalf, view, Vector2.right, Vector2.up);

        if (type == Direction.topDown)
        {
            DrawFace(viewTextures[3], face, center, squareHalf, view, Vector2.up, Vector2.left);
            DrawFace(viewTextures[4], face, center, squareHalf, view, Vector2.up, Vector2.right);
        }
        else if (type == Direction.bottomUp)
        {
            DrawFace(viewTextures[3], face, center, squareHalf, view, Vector2.up, Vector2.right);
            DrawFace(viewTextures[4], face, center, squareHalf, view, Vector2.up, Vector2.left);
        }

        GL.PopMatrix();
    }

    Vector2[] FacePolygon(float squareHalf, float view)
    {
        //One pyramid face as a trapezoid, in (along edge, away from center) coordinates.
        //It starts at the square's edge and widens along the diagonals, so neighbouring faces meet without overlapping.
        return new Vector2[]
        {
            new Vector2(-squareHalf, squareHalf),
            new Vector2(squareHalf, squareHalf),
            new Vector2(squareHalf + view, squareHalf + view),
            new Vector2(-squareHalf - view, squareHalf + view)
        };
    }

    void DrawFace(RenderTexture texture, Vector2[] face, Vector2 center, float squareHalf, float view, Vector2 along, Vector2 outward)
    {
        //Screen area covered by the view texture, used to map screen positions to texture coordinates.
        Vector2 cornerA = center - (squareHalf + view) * along + squareHalf * outward;
        Vector2 cornerB = center + (squareHalf + view) * along + (squareHalf + view) * outward;
        Vector2 viewMin = Vector2.Min(cornerA, cornerB);
        Vector2 viewSize = Vector2.Max(cornerA, cornerB) - viewMin;

        viewMaterial.mainTexture = texture;
        viewMaterial.SetPass(0);

        GL.Begin(GL.TRIANGLES);

        for (int i = 1; i < face.Length - 1; i++)
        {
            FaceVertex(face[0], center, along, outward, viewMin, viewSize);
            FaceVertex(face[i], center, along, outward, viewMin, viewSize);
            FaceVertex(face[i + 1], center, along, outward, viewMin, viewSize);
        }

        GL.End();
    }

    void FaceVertex(Vector2 point, Vector2 center, Vector2 along, Vector2 outward, Vector2 viewMin, Vector2 viewSize)
    {
        Vector2 position = center + point.x * along + point.y * outward;
        Vector2 uv = new Vector2((position.x - viewMin.x) / viewSize.x, (position.y - viewMin.y) / viewSize.y);

        GL.TexCoord2(uv.x, uv.y);
        GL.Vertex3(position.x, position.y, 0);
    }

    Vector2 GetMainGameViewSize()
    {
        Type T = Type.GetType("UnityEditor.GameView,UnityEditor");
        System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
        return (Vector2)Res;
    }
}
