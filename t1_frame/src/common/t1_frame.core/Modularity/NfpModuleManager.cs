using System.Reflection;

namespace t1_frame.core;

public class NfpModuleManager
{
    public NfpModuleInfo StartupModule { get; private set; }

    private NfpModuleCollection _modules;

    public void Initialize(Type startupModule)
    {
        _modules = new NfpModuleCollection(startupModule);

        LoadAllModules();
    }

    public void ShutdownModules()
    {
        throw new NotImplementedException();
    }

    public void StartModules()
    {
        _modules.ForEach(module => module.Instance.Initialize());
    }

    private void LoadAllModules()
    {
        TypeInfo typeInfo = _modules.StartupModuleType.GetTypeInfo();
        var attrs = typeInfo.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in attrs)
        {
            foreach (var item in attr.DependedModuleTypes)
            {
                _modules.Add(new NfpModuleInfo(item, (NfpModule)Activator.CreateInstance(item)));
            }

        }
    }
}
