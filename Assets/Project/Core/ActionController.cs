using System;
using System.Collections.Generic;
using System.Text;
using TMPro.EditorUtilities;

class ActionController
{
    SensorContext _context;
    public ActionController()
    {
        _context = new SensorContext();
    }
    public void TriggerClap(bool flag)
    {
        _context.HasClap = flag;
    }

    public void TriggerBlow(bool flag)
    {
        _context.HasBlow = flag;
    }

    public void TriggerLight(bool flag)
    {
        _context.HasLight = flag;
    }

    public SensorContext GetContext()
    {
        return _context.Copy();
    }

};