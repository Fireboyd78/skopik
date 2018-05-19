using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static ISkopikObject CreateValue<T>(T value)
        {
            var type = typeof(T);

            foreach (var kv in TypeLookup)
            {
                var valueType = kv.Value;

                if (valueType == type)
                {
                    try
                    {
                        var genType = typeof(SkopikValue<>).MakeGenericType(type);

                        return (ISkopikObject)Activator.CreateInstance(genType, value);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error creating value object: {e.Message}");
                    }
                }
            }

            throw new InvalidOperationException("Could not create a value object using the specified data.");
        }

        public static ISkopikObject CreateValue<T>(string textValue, Func<string, T> parseFn)
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

    public static class SkopikExtensions
    {
        public static SkopikValue<T> AsValue<T>(this ISkopikObject obj)
        {
            var valueType = typeof(T);
            var type = typeof(SkopikValue<T>);

            if (SkopikFactory.IsValueType(valueType, obj.DataType))
            {
                var myType = obj.GetType();

                if (myType.IsGenericType)
                {
                    var myValueType = myType.GetGenericArguments()[0];

                    // already the proper type
                    if (myValueType == valueType)
                        return (SkopikValue<T>)obj;
                }
                else
                {
                    var data = obj.GetData();

                    return new SkopikValue<T>((T)data);
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot cast to a non-value type!");
            }

            // couldn't perform cast
            return null;
        }
    }

    public interface ISkopikObject
    {
        object GetData();
        void SetData(object value);

        SkopikDataType DataType { get; }

        bool IsNone { get; }
        bool IsNull { get; }
        bool IsArray { get; }
        bool IsScope { get; }
        bool IsTuple { get; }
        bool IsValue { get; }
    }

    public interface ISkopikValue : ISkopikObject, IConvertible
    {
        object Value { get; set; }
    }

    public interface ISkopikBlock : ISkopikObject, ICollection, IEnumerable<ISkopikObject>
    {
        ISkopikObject this[int index] { get; set; }
        
        /// <summary>
        /// Gets or sets the name of this scoped object.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets whether or not this scoped object is anonymous.
        /// </summary>
        bool IsAnonymous { get; }

        /// <summary>
        /// Gets whether or not this scoped object is empty.
        /// </summary>
        bool IsEmpty { get; }
    }

    public interface ISkopikScope : ISkopikBlock
    {
        ISkopikObject this[string name] { get; set; }

        bool HasEntry(string name);

        ISkopikObject GetEntry(string name);
        void SetEntry(string name, ISkopikObject data);
    }

    public interface ISkopikArray : ISkopikBlock
    {
        ISkopikObject GetEntry(int index);
        void SetEntry(int index, ISkopikObject data);
    }

    public interface ISkopikTuple : ISkopikBlock
    {
        SkopikDataType TupleType { get; }

        IEnumerable<object> GetValues();
        IEnumerable<T> GetValues<T>();
    }

    public class SkopikObject : ISkopikObject
    {
        object ISkopikObject.GetData()
        {
            return null;
        }

        void ISkopikObject.SetData(object value)
        {
            throw new NotSupportedException("Cannot set value on an empty/null type!");
        }

        public static SkopikObject Null
        {
            get { return new SkopikObject(SkopikDataType.Null); }
        }

        public SkopikDataType DataType { get; }

        public bool IsNone
        {
            get { return (DataType & SkopikDataType.None) != 0; }
        }

        public bool IsNull
        {
            get { return (DataType & SkopikDataType.Null) != 0; }
        }

        public bool IsArray
        {
            get { return (DataType & SkopikDataType.Array) != 0; }
        }

        public bool IsScope
        {
            get { return (DataType & SkopikDataType.Scope) != 0; }
        }

        public bool IsTuple
        {
            get { return (DataType & SkopikDataType.Tuple) != 0; }
        }

        public bool IsValue
        {
            get { return !(IsArray | IsScope | IsTuple); }
        }

        protected SkopikObject(SkopikDataType type)
        {
            DataType = type;
        }
    }

    public abstract class SkopikValue : SkopikObject, ISkopikValue
    {
        public abstract object GetValue();
        public abstract void SetValue(object value);

        object ISkopikValue.Value
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        TypeCode IConvertible.GetTypeCode()
        {
            switch (DataType)
            {
            case SkopikDataType.Null:       return TypeCode.Empty;
            case SkopikDataType.Boolean:    return TypeCode.Boolean;
            case SkopikDataType.Binary:     return TypeCode.Int32;
            case SkopikDataType.String:     return TypeCode.String;
            case SkopikDataType.Float:      return TypeCode.Single;
            case SkopikDataType.Double:     return TypeCode.Double;
            case SkopikDataType.Integer32:  return TypeCode.Int32;
            case SkopikDataType.Integer64:  return TypeCode.Int64;
            case SkopikDataType.UInteger32: return TypeCode.UInt32;
            case SkopikDataType.UInteger64: return TypeCode.UInt64;
            }

            return TypeCode.Object;
        }

        private T ConvertTo<T>(Func<object, IFormatProvider, T> convertFn, IFormatProvider provider)
        {
            var data = GetValue();

            if (data != null)
                return convertFn(data, provider);
                    
            return default(T);
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToBoolean, provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToByte, provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToChar, provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException("Unsupported value type cast.");
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToDecimal, provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToDouble, provider);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToInt16, provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToInt32, provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToInt64, provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToSByte, provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToSingle, provider);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToString, provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            var data = GetValue();

            if (data is IConvertible)
                return Convert.ChangeType(data, conversionType, provider);
            
            throw new InvalidOperationException($"Cannot convert object of type '{data.GetType().Name}' to type '{conversionType.Name}'.");
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToUInt16, provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToUInt32, provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return ConvertTo(Convert.ToUInt64, provider);
        }

        public override string ToString()
        {
            return GetValue()?.ToString() ?? "<null>";
        }

        protected SkopikValue(SkopikDataType dataType)
            : base(dataType)
        { }
    }

    public class SkopikValue<T> : SkopikValue, ISkopikObject
    {
        public T Value { get; set; }

        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            if (!SkopikFactory.IsValueType(value, DataType))
                throw new InvalidOperationException("Value does not match underlying value type.");

            Value = (T)value;
        }

        object ISkopikObject.GetData()
        {
            return Value;
        }

        void ISkopikObject.SetData(object value)
        {
            SetValue(value);
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "<null>";
        }

        public SkopikValue()
            : base(SkopikFactory.GetValueType(typeof(T)))
        { }

        public SkopikValue(T value)
            : this()
        {
            Value = value;
        }
    }

    public abstract class SkopikBlock : SkopikObject, ISkopikBlock
    {
        public abstract IEnumerator<ISkopikObject> GetEnumerator();
        
        public abstract int Count { get; }

        public abstract void CopyTo(Array array, int index);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        object ICollection.SyncRoot
        {
            get { return this; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        public abstract ISkopikObject this[int index] { get; set; }
        
        public string Name { get; set; }

        public bool IsAnonymous
        {
            get { return String.IsNullOrEmpty(Name); }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }
        
        protected SkopikBlock(SkopikDataType type)
            : base (type)
        { }
    }

    public class SkopikScope : SkopikBlock, ISkopikScope
    {
        private Dictionary<string, ISkopikObject> m_entries;

        public Dictionary<string, ISkopikObject> Entries
        {
            get { return m_entries; }
        }

        public override void CopyTo(Array array, int index)
        {
            var items = m_entries.Select((e) => e.Value).ToArray();

            Array.Copy(items, 0, array, index, Count);
        }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            var values = Entries.Select((e) => e.Value);

            return values.GetEnumerator();
        }

        public override ISkopikObject this[int index]
        {
            get { return Entries.ElementAt(index).Value; }
            set
            {
                if (index > Entries.Count)
                    throw new IndexOutOfRangeException($"Index {index} out of range in scope.");

                var key = Entries.ElementAt(index).Key;

                Entries[key] = value;
            }
        }

        public ISkopikObject this[string name]
        {
            get { return GetEntry(name); }
            set { SetEntry(name, value); }
        }

        public override int Count
        {
            get { return m_entries.Count; }
        }
        
        public bool HasEntry(string name)
        {
            return m_entries.ContainsKey(name);
        }

        public ISkopikObject GetEntry(string name)
        {
            return (HasEntry(name)) ? m_entries[name] : null;
        }
        
        public void SetEntry(string name, ISkopikObject data)
        {
            if (HasEntry(name))
            {
                m_entries[name] = data;
            }
            else
            {
                m_entries.Add(name, data);
            }
        }
        
        public SkopikScope()
            : base(SkopikDataType.Scope)
        {
            m_entries = new Dictionary<string, ISkopikObject>();
        }

        public SkopikScope(string name)
            : this()
        {
            Name = name;
        }
    }

    public class SkopikArray : SkopikBlock, ISkopikArray
    {
        private List<ISkopikObject> m_entries;

        public List<ISkopikObject> Entries
        {
            get { return m_entries; }
        }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        public override void CopyTo(Array array, int index)
        {
            ((ICollection)Entries).CopyTo(array, index);
        }

        public override ISkopikObject this[int index]
        {
            get { return GetEntry(index); }
            set { SetEntry(index, value); }
        }

        public override int Count
        {
            get { return m_entries.Count; }
        }

        public ISkopikObject GetEntry(int index)
        {
            return (index < m_entries.Count) ? m_entries[index] : null;
        }

        public void SetEntry(int index, ISkopikObject data)
        {
            if (index < m_entries.Count)
            {
                m_entries[index] = data;
            }
            else
            {
                m_entries.Insert(index, data);
            }
        }
        
        public SkopikArray()
            : base(SkopikDataType.Array)
        {
            m_entries = new List<ISkopikObject>();
        }

        public SkopikArray(string name)
            : this()
        {
            Name = name;
        }
    }

    public class SkopikTuple : SkopikBlock, ISkopikTuple
    {
        private List<ISkopikObject> m_entries;

        public List<ISkopikObject> Entries
        {
            get { return m_entries; }
        }
        
        public SkopikDataType TupleType { get; }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        public override void CopyTo(Array array, int index)
        {
            ((ICollection)Entries).CopyTo(array, index);
        }

        public void CopyTo<T>(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo<T>(T[] array, int index)
        {
            if (!SkopikFactory.IsValueType(typeof(T), TupleType))
                throw new InvalidOperationException($"Cannot cast tuple to array of '{typeof(T).Name}'.");

            var items = Entries.Select((e) => e.GetData()).ToArray();

            items.CopyTo(array, 0);
        }

        public override ISkopikObject this[int index]
        {
            get { return GetEntry(index); }
            set { SetEntry(index, value); }
        }

        public override int Count
        {
            get { return m_entries.Count; }
        }
        
        public ISkopikObject GetEntry(int index)
        {
            return (index < m_entries.Count) ? m_entries[index] : null;
        }

        public void SetEntry(int index, ISkopikObject data)
        {
            if (data.DataType != TupleType)
                throw new InvalidOperationException("Tuple data mismatch!");

            if (index < m_entries.Count)
            {
                m_entries[index] = data;
            }
            else
            {
                m_entries.Insert(index, data);
            }
        }

        public IEnumerable<object> GetValues()
        {
            foreach (var entry in Entries)
            {
                if (!entry.IsValue)
                    yield break;

                yield return entry.GetData();
            }
        }

        public IEnumerable<T> GetValues<T>()
        {
            var targetType = typeof(T);

            if (targetType.IsSubclassOf(typeof(ISkopikObject)))
                throw new InvalidOperationException("Cannot retrieve Skopik object values!");

            foreach (var entry in GetValues())
            {
                if (!targetType.IsInstanceOfType(entry))
                    yield break;

                yield return (T)entry;
            }
        }
        
        public SkopikTuple(SkopikDataType tupleType)
            : base(SkopikDataType.Tuple)
        {
            m_entries = new List<ISkopikObject>();
            TupleType = tupleType;
        }

        public SkopikTuple(SkopikDataType tupleType, string name)
            : this(tupleType)
        {
            Name = name;
        }
    }
}
