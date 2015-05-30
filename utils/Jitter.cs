using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace httpload.utils
{
	internal static class Jitter
	{
		public static void LoadAssembliesAndJitMethods()
		{
			Assembly
				.GetEntryAssembly()
				.GetReferencedAssemblies()
				.Select(Assembly.Load)
				.Where(assembly => !assembly.GlobalAssemblyCache)
				.SelectMany(assembly => assembly
					.GetTypes()
					.Where(type => !(type.IsInterface || type.IsGenericTypeDefinition))
					.Where(type => type.GetConstructor(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null))
				.SelectMany(type => type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
					.Where(method => !(method.IsAbstract || method.IsGenericMethodDefinition || method.ContainsGenericParameters)))
				.ForEach(method => RuntimeHelpers.PrepareMethod(method.MethodHandle));
		}
	}
}