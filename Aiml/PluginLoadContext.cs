#if NET5_0_OR_GREATER
using System.Reflection;
using System.Runtime.Loader;

namespace Aiml;
internal class PluginLoadContext(string pluginPath) : AssemblyLoadContext {
	private readonly AssemblyDependencyResolver resolver = new(pluginPath);

	protected override Assembly? Load(AssemblyName assemblyName) {
		// Check whether the assembly is already loaded (possibly another plugin assembly).
		foreach (var a in AppDomain.CurrentDomain.GetAssemblies()) {
			if (a.GetName().FullName == assemblyName.FullName)
				return a;
		}

		var assemblyPath = this.resolver.ResolveAssemblyToPath(assemblyName);
		if (assemblyPath != null)
			return this.LoadFromAssemblyPath(assemblyPath);

		// The assembly couldn't be found from deps.json; look for it in the plugins directory.
		if (assemblyName.Name == null)
			return null;
		var path = Path.Combine("plugins", $"{assemblyName.Name}.dll");
		if (File.Exists(path))
			return this.LoadFromAssemblyPath(path);

		return null;
	}

	protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
		var libraryPath = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
		return libraryPath != null ? this.LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
	}
}
#endif
