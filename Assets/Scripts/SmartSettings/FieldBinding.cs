using System.Reflection;

namespace SmartSettings
{
    public static class FieldBinding
    {
        public const BindingFlags Bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
    }
}