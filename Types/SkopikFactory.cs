using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Skopik
{
    using TypeMapDictionary = Dictionary<SkopikDataType, Type>;

    static class SkopikFactory
    {
        public static TypeMapDictionary TypeLookup;

        public static Type GetType(SkopikDataType type)
        {
            return TypeLookup[type];
        }

        public static SkopikDataType GetValueType(Type type)
        {
            foreach (var kv in TypeLookup)
            {
                if (kv.Value == type)
                    return kv.Key;
            }

            return SkopikDataType.None;
        }
        
        public static bool IsValueType(object value, SkopikDataType type)
        {
            return GetType(type).IsInstanceOfType(value);
        }
        
        public static bool IsValueType(Type value, SkopikDataType type)
        {
            return GetType(type).IsAssignableFrom(value);
        }

        public static bool IsValueType(object value)
        {
            var type = value.GetType();

            return IsValueType(type);
        }

        public static bool IsValueType(Type type)
        {
            foreach (var kv in TypeLookup)
            {
                var valueType = kv.Value;

                if (valueType == type)
                    return true;
            }

            return false;
        }

        public static ISkopikValue CreateValue(object value)
        {
            var type = value.GetType();

            foreach (var kv in TypeLookup)
            {
                var valueType = kv.Value;

                if (valueType == type)
                {
                    try
                    {
                        var genType = typeof(SkopikValue<>).MakeGenericType(type);

                        return (ISkopikValue)Activator.CreateInstance(genType, value);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error creating value object: {e.Message}");
                    }
                }
            }

            throw new InvalidOperationException("Could not create a value object using the specified data.");
        }
        
        public static ISkopikValue CreateValue<T>(string textValue, Func<string, T> parseFn)
        {
            T value = default(T);

            try
            {
                value = parseFn(textValue);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error parsing value '{textValue}': {e.Message}");
            }

            return CreateValue(value);
        }

        public static ISkopikValue CreateValue<T>(string textValue, Func<string, IFormatProvider, T> parseFn, IFormatProvider provider)
        {
            T value = default(T);

            try
            {
                value = parseFn(textValue, provider);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error parsing value '{textValue}': {e.Message}");
            }

            return CreateValue(value);
        }

        static SkopikFactory()
        {
            TypeLookup = new TypeMapDictionary() {
                { SkopikDataType.Binary,        typeof(BitArray) },
                { SkopikDataType.String,        typeof(String) },

                { SkopikDataType.Boolean,       typeof(Boolean) },

                { SkopikDataType.Integer32,     typeof(Int32) },
                { SkopikDataType.Integer64,     typeof(Int64) },
                { SkopikDataType.UInteger32,    typeof(UInt32) },
                { SkopikDataType.UInteger64,    typeof(UInt64) },

                { SkopikDataType.Float,         typeof(Single) },
                { SkopikDataType.Double,        typeof(Double) },
            };
        }
    }
}
