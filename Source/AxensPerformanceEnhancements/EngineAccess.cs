// shared engine access for optimizations.
using System.Runtime.CompilerServices;
[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: IgnoresAccessChecksTo("UnityStubs")]
[assembly: IgnoresAccessChecksTo("Waylith")]
[assembly: IgnoresAccessChecksTo("RimWorld")]
[assembly: IgnoresAccessChecksTo("Waylith.UnityCompat")]
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute
{
    public string AssemblyName { get; } = assemblyName;
}
