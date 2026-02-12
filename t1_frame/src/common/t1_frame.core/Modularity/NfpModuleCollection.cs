namespace t1_frame.core;

public class NfpModuleCollection : List<NfpModuleInfo>
{
    public Type StartupModuleType { get; }

    public NfpModuleCollection(Type startupModuleType)
    {
        StartupModuleType = startupModuleType;
    }


}
