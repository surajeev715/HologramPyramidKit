using System;
using System.Collections.Generic;
using System.Text;

public class SensorContextService
{
    private readonly SensorContext _context = new SensorContext();

    public void SetClap(bool flag) => _context.HasClap = flag;
    public void SetBlow(bool flag) => _context.HasBlow = flag;
    public void SetLight(bool flag) => _context.HasLight = flag;

    // for code that needs to modify the context directly (SensorHandler)
    public void Apply(System.Action<SensorContext> change) => change(_context);

    public SensorContext Snapshot() => _context.Copy();
}