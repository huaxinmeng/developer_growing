namespace t1_frame.core;

public class NfpModuleInfo 
{
    /// <summary>
    /// Type of the module.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Instance of the module.
    /// </summary>
    public NfpModule Instance { get; }

    /// <summary>
    /// All dependent modules of this module.
    /// </summary>
    public List<NfpModuleInfo> Dependencies { get; }

    public NfpModuleInfo(Type type, NfpModule instance)
    {
        Type = type;
        Instance = instance;

        Dependencies = new List<NfpModuleInfo>();
    }
}
