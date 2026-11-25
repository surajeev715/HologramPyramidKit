using System.Collections.Generic;

public class VariantNameCache
{
    private readonly List<string> _names = new List<string>();
    private IShapeCycler _cycler;

    public IReadOnlyList<string> Get(IShapeCycler cycler)
    {
        int count = cycler != null ? cycler.Count : 0;
        if (cycler != _cycler || _names.Count != count)
        {
            _cycler = cycler;
            _names.Clear();
            for (int i = 0; i < count; i++)
            {
                _names.Add(cycler.NameAt(i));
            }
        }
        return _names;
    }
}
