using System.Reflection;
using System.Runtime.Loader;

namespace AimlVoice;
internal class PluginLoadContext : AssemblyLoadContext {
	private readonly AssemblyDependencyResolver resolver;

	public PluginLoadContext(string pluginPath) {
		resolver = new AssemblyDependencyResolver(pluginPath);
	}

	protected override Assembly? Load(AssemblyName assemblyName) {
		// Check whether the assembly is already loaded (possibly another plugin assembly).
		foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
			if (a.GetName().FullName == assemblyName.FullName)
				return a;
		}

		var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
		if (assemblyPath != null)
			return this.LoadFromAssemblyPath(assemblyPath);

		// The assembly couldn't be found from deps.json; look for it in the plugins directory.
		if (assemblyName.Name == null)
			return null;
		var path = Path.Combine("plugins", $"{assemblyName.Name}.dll");
		if (File.Exists(path))
			return this.LoadFromAssemblyPath(path);
#if DEBUG
		// Look for a debug build.
		for (path = Environment.CurrentDirectory; path != null; path = Path.GetDirectoryName(path)) {
			var path2 = Path.Combine(path, "Plugins", assemblyName.Name, "bin", "Debug");
			if (Directory.Exists(path2)) {
				foreach (var dir in Directory.GetDirectories(path2)) {
					var path3 = Path.Combine(dir, $"{assemblyName.Name}.dll");
					if (File.Exists(path3))
						return this.LoadFromAssemblyPath(path3);
				}
			}
		}
#endif

		return null;
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
		var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
	}
}
