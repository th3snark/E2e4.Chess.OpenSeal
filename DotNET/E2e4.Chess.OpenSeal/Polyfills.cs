#if !NET && !NETSTANDARD2_1
// Nullable-analysis attributes that older targets do not ship. They are internal polyfills: the
// compiler recognises them by name, so the annotations still apply to callers on newer targets.
namespace System.Diagnostics.CodeAnalysis;

[System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : System.Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}
#endif
