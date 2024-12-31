using System.Reflection;
using System.Runtime.Serialization;

namespace Bliss.CSharp.Materials;

public static class MaterialMapTypeExtensions {
    
    /// <summary>
    /// Retrieves the string representation of the specified <see cref="MaterialMapType"/> enum value,
    /// using the value from the <see cref="EnumMemberAttribute"/> if available.
    /// If the attribute is not present, it returns the enum value's name as a string.
    /// </summary>
    /// <param name="type">The <see cref="MaterialMapType"/> enum value for which to retrieve the name.</param>
    /// <returns>The string representation of the <see cref="MaterialMapType"/>.
    /// If the <see cref="EnumMemberAttribute"/> is defined, its value is returned; otherwise, the enum name is returned.</returns>
    public static string GetName(this MaterialMapType type) {
        Type enumType = typeof(MaterialMapType);
        
        MemberInfo[] memberInfo = enumType.GetMember(type.ToString());

        if (memberInfo.Length > 0) {
            EnumMemberAttribute? attribute = memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).OfType<EnumMemberAttribute>().FirstOrDefault();

            if (attribute != null) {
                return attribute.Value!;
            }
        }
        
        return type.ToString();
    }
}