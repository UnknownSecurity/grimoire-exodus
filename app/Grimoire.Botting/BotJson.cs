using Grimoire.Game.Data;
using Grimoire.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Grimoire.Botting
{
    /// <summary>
    /// Central JSON settings for bot and equipment-set files.
    /// Polymorphic type metadata is required for IBotCommand and ISetInterface,
    /// but must never be allowed to instantiate arbitrary framework types.
    /// </summary>
    public static class BotJson
    {
        public static JsonSerializerSettings CreateSettings(
            DefaultValueHandling defaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling nullValueHandling = NullValueHandling.Ignore)
        {
            return new JsonSerializerSettings
            {
                DefaultValueHandling = defaultValueHandling,
                NullValueHandling = nullValueHandling,
                TypeNameHandling = TypeNameHandling.All,
                SerializationBinder = SafeBotSerializationBinder.Instance
            };
        }

        private sealed class SafeBotSerializationBinder : ISerializationBinder
        {
            public static readonly SafeBotSerializationBinder Instance = new SafeBotSerializationBinder();

            private static readonly Assembly ClientAssembly = typeof(Configuration).Assembly;
            private static readonly Assembly CoreAssembly = typeof(string).Assembly;

            private static readonly HashSet<Type> SafeGenericDefinitions = new HashSet<Type>
            {
                typeof(List<>),
                typeof(Dictionary<,>),
                typeof(Nullable<>),
                typeof(KeyValuePair<,>)
            };

            public Type BindToType(string assemblyName, string typeName)
            {
                Type type = ResolveType(assemblyName, typeName);
                if (type == null || !IsAllowed(type))
                {
                    throw new JsonSerializationException("Bot file contains a disallowed type: " + typeName);
                }
                return type;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (!IsAllowed(serializedType))
                {
                    throw new JsonSerializationException("Cannot serialize disallowed bot type: " + serializedType.FullName);
                }

                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
            }

            private static Type ResolveType(string assemblyName, string typeName)
            {
                Type type = null;
                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    try
                    {
                        // Generic metadata embeds assembly names for each type argument.
                        // Resolve those names explicitly so legacy GBots keep working even
                        // though Grimoire.exe is distributed as Grimoire Exodus.exe.
                        type = Type.GetType(
                            typeName + ", " + assemblyName,
                            ResolveLegacyAssembly,
                            ResolveAssemblyType,
                            false);
                    }
                    catch (Exception)
                    {
                        type = null;
                    }
                }

                // Old bot files commonly omit the assembly version or use the
                // executable's former version. Resolve local types by name.
                return type ?? ClientAssembly.GetType(typeName, false);
            }

            private static Assembly ResolveLegacyAssembly(AssemblyName requestedAssembly)
            {
                string requestedName = requestedAssembly?.Name ?? string.Empty;
                string clientName = ClientAssembly.GetName().Name;
                if (string.Equals(requestedName, clientName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(requestedName, "Grimoire", StringComparison.OrdinalIgnoreCase))
                {
                    return ClientAssembly;
                }

                string coreName = CoreAssembly.GetName().Name;
                if (string.Equals(requestedName, coreName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(requestedName, "mscorlib", StringComparison.OrdinalIgnoreCase))
                {
                    return CoreAssembly;
                }

                return null;
            }

            private static Type ResolveAssemblyType(Assembly assembly, string typeName, bool ignoreCase)
            {
                return assembly?.GetType(typeName, false, ignoreCase);
            }

            private static bool IsAllowed(Type type)
            {
                if (type == null)
                    return false;

                if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
                    return true;

                if (type.IsArray)
                    return IsAllowed(type.GetElementType());

                if (type.IsGenericType)
                {
                    if (!SafeGenericDefinitions.Contains(type.GetGenericTypeDefinition()))
                        return false;

                    foreach (Type argument in type.GetGenericArguments())
                    {
                        if (!IsAllowed(argument))
                            return false;
                    }
                    return true;
                }

                if (type.Assembly != ClientAssembly)
                    return false;

                string typeNamespace = type.Namespace ?? string.Empty;
                return typeof(IBotCommand).IsAssignableFrom(type)
                    || type == typeof(Configuration)
                    || typeNamespace.StartsWith("Grimoire.Botting.Commands", StringComparison.Ordinal)
                    || typeNamespace.StartsWith("Grimoire.Game.Data", StringComparison.Ordinal)
                    || type == typeof(SetItem)
                    || type == typeof(ISetInterface)
                    || type == typeof(Item);
            }
        }
    }
}
