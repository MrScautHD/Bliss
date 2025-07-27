using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Bliss.CSharp.Materials;

public static class MaterialMapTypeExtensions {
    
    /// <summary>
    /// A thread-safe dictionary used to cache the string representation of <see cref="MaterialMapType"/> enum values.
    /// </summary>
    private static ConcurrentDictionary<MaterialMapType, string> _nameCache = new();

    /// <summary>
    /// Retrieves the name of the specified <see cref="MaterialMapType"/> enum value.
    /// </summary>
    /// <param name="type">The <see cref="MaterialMapType"/> enum value whose name is to be retrieved.</param>
    /// <returns>A string representation of the <see cref="MaterialMapType"/> enum value, either from the <see cref="EnumMemberAttribute"/> or the enum's name.</returns>
    public static string GetName(this MaterialMapType type) {
        return _nameCache.GetOrAdd(type, mapType => {
            Type enumType = typeof(MaterialMapType);
            MemberInfo[] memberInfo = enumType.GetMember(mapType.ToString());
            
            if (memberInfo.Length > 0) {
                EnumMemberAttribute? attribute = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).OfType<EnumMemberAttribute>().FirstOrDefault();
                
                if (attribute != null) {
                    return attribute.Value!;
                }
            }
            
            return mapType.ToString();
        });
    }
}