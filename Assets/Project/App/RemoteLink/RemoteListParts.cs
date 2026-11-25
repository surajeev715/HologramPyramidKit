using System;

public class RemoteListParts
{
    private static readonly string[] NO_NAMES = new string[0];

    private string[] _building;
    private int _received;
    private int _buildingShapeIndex = RemoteProtocol.NO_INDEX;

    public string[] Names { get; private set; } = NO_NAMES;
    public int ShapeIndex { get; private set; } = RemoteProtocol.NO_INDEX;

    public void Add(RemoteMessage part)
    {
        if (part.first == 0)
        {
            _building = new string[part.total];
            _received = 0;
            _buildingShapeIndex = part.shapeIndex;
        }
        else if (_building == null || part.total != _building.Length || part.shapeIndex != _buildingShapeIndex
            || part.first != _received)
        {
            _building = null;
            return;
        }

        Array.Copy(part.names, 0, _building, part.first, part.names.Length);
        _received += part.names.Length;
        if (_received < _building.Length)
        {
            return;
        }

        Names = _building;
        ShapeIndex = _buildingShapeIndex;
        _building = null;
    }
}
