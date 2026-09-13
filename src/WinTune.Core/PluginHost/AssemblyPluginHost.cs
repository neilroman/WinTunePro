using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using WinTune.Sdk;

namespace WinTune.Core.PluginHost;

public sealed class AssemblyPluginHost : IDisposable
{
    private readonly ILogger<AssemblyPluginHost> _log;
    private readonly List<PluginContext> _loaded = [];

    public AssemblyPluginHost(ILogger<AssemblyPluginHost> log) => _log = log;

    public IReadOnlyList<IOptimizerModule> LoadedModules =>
        _loaded.SelectMany(p => p.Modules).ToList();

    public IReadOnlyList<IOptimizerModule> LoadFromDirectory(string pluginDir)
    {
        var modules = new List<IOptimizerModule>();
        if (!Directory.Exists(pluginDir)) return modules;

        foreach (var dll in Directory.GetFiles(pluginDir, "WinTune.Plugins.*.dll"))
        {
            try
            {
                var ctx = new PluginLoadContext(dll);
                var asm = ctx.LoadFromAssemblyPath(dll);
                var found = asm.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IOptimizerModule).IsAssignableFrom(t))
                    .Select(t => (IOptimizerModule)Activator.CreateInstance(t)!)
                    .ToList();

                _loaded.Add(new PluginContext(ctx, found));
                modules.AddRange(found);
                _log.LogInformation("Loaded {Count} module(s) from {Dll}", found.Count, Path.GetFileName(dll));
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to load plugin from {Dll}", dll);
            }
        }
        return modules;
    }

    public void Dispose()
    {
        foreach (var p in _loaded)
        {
            p.Context.Unload();
        }
        _loaded.Clear();
    }
}

internal sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

    protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName name)
    {
        var path = _resolver.ResolveAssemblyToPath(name);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }
}

internal sealed record PluginContext(AssemblyLoadContext Context, List<IOptimizerModule> Modules);
