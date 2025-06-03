using System.Linq;
using System.Reflection;

namespace FoodShelves;

// Class used to dynamically read/write properties needed from/to the attribute tree mainly for animations to work properly. Can also be used to read/set other properties as well.

/// <summary>
/// Attribute attachable to any property that can be safely read/wrote to the attributes of a block. Mainly used for animation properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TreeSerializableAttribute : Attribute {
    public object DefaultValue { get; set; }
    public TreeSerializableAttribute(object defaultValue = null) {
        DefaultValue = defaultValue;
    }
}

public static class TreeAttributeSerializer {
    private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();

    /// <summary>
    /// Gets all properties marked with TreeSerializableAttribute for the given type
    /// </summary>
    private static PropertyInfo[] GetSerializableProperties(Type type) {
        if (_propertyCache.TryGetValue(type, out PropertyInfo[] cachedProps))
            return cachedProps;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.GetCustomAttribute<TreeSerializableAttribute>() != null)
            .ToArray();

        _propertyCache[type] = properties;
        return properties;
    }

    /// <summary>
    /// Serializes all marked properties from the object to the tree
    /// </summary>
    public static void SerializeToTree(object obj, ITreeAttribute tree) {
        if (obj == null || tree == null) return;

        var properties = GetSerializableProperties(obj.GetType());

        foreach (var prop in properties) {
            string key = prop.Name;
            object value = prop.GetValue(obj);

            if (value == null) continue;

            switch (prop.PropertyType) {
                case Type t when t == typeof(bool): tree.SetBool(key, (bool)value); break;
                case Type t when t == typeof(int): tree.SetInt(key, (int)value); break;
                case Type t when t == typeof(float): tree.SetFloat(key, (float)value); break;
                case Type t when t == typeof(double): tree.SetDouble(key, (double)value); break;
                case Type t when t == typeof(long): tree.SetLong(key, (long)value); break;
                case Type t when t == typeof(string): tree.SetString(key, (string)value); break;
                case Type t when t == typeof(byte[]): tree.SetBytes(key, (byte[])value); break;
            }
        }
    }

    /// <summary>
    /// Deserializes all marked properties from the tree to the object
    /// </summary>
    public static void DeserializeFromTree(object obj, ITreeAttribute tree) {
        if (obj == null || tree == null) return;

        var properties = GetSerializableProperties(obj.GetType());

        foreach (var prop in properties) {
            var attr = prop.GetCustomAttribute<TreeSerializableAttribute>();
            string key = prop.Name;

            if (prop.PropertyType == typeof(bool)) {
                bool defaultValue = attr.DefaultValue is bool b ? b : false;
                prop.SetValue(obj, tree.GetBool(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(int)) {
                int defaultValue = attr.DefaultValue is int i ? i : 0;
                prop.SetValue(obj, tree.GetInt(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(float)) {
                float defaultValue = attr.DefaultValue is float f ? f : 0f;
                prop.SetValue(obj, tree.GetFloat(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(double)) {
                double defaultValue = attr.DefaultValue is double d ? d : 0.0;
                prop.SetValue(obj, tree.GetDouble(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(long)) {
                long defaultValue = attr.DefaultValue is long l ? l : 0L;
                prop.SetValue(obj, tree.GetLong(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(string)) {
                string defaultValue = attr.DefaultValue as string;
                prop.SetValue(obj, tree.GetString(key, defaultValue));
            }
            else if (prop.PropertyType == typeof(byte[])) {
                byte[] defaultValue = attr.DefaultValue as byte[];
                prop.SetValue(obj, tree.GetBytes(key, defaultValue));
            }
        }
    }

    /// <summary>
    /// Checks if a type has any properties marked for serialization
    /// </summary>
    public static bool HasSerializableProperties(Type type) {
        return GetSerializableProperties(type).Length > 0;
    }

    /// <summary>
    /// Gets the names of all serializable properties for a type
    /// </summary>
    public static string[] GetSerializablePropertyNames(Type type) {
        return GetSerializableProperties(type).Select(p => p.Name).ToArray();
    }

    /// <summary>
    /// Clears the property cache
    /// </summary>
    public static void ClearCache() {
        _propertyCache.Clear();
    }
}

