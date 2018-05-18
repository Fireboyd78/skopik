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
    
    public interface ISkopikBlock : ISkopikObject
    {
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
        ISkopikObject this[int index] { get; set; }

        ISkopikObject GetEntry(int index);
        void SetEntry(int index, ISkopikObject data);
    }

    public interface ISkopikTuple : ISkopikBlock
    {
        ISkopikObject this[int index] { get; set; }

        SkopikDataType TupleType { get; }
    }

    public class SkopikObject : ISkopikObject
    {
        object ISkopikObject.GetData()            => null;
        void ISkopikObject.SetData(object value)  => throw new NotSupportedException("Cannot set value on an empty/null type!");

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
        
        public SkopikValue<T> AsValue<T>()
        {
            var valueType = typeof(T);
            var type = typeof(SkopikValue<T>);

            if (SkopikFactory.IsValueType(valueType, DataType))
            {
                var myType = GetType();

                if (myType.IsGenericType)
                {
                    var myValueType = myType.GetGenericArguments()[0];

                    // already the proper type
                    if (myValueType == valueType)
                        return (SkopikValue<T>)this;
                }
                else
                {
                    var data = ((ISkopikObject)this).GetData();

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
        
        protected SkopikObject(SkopikDataType type)
        {
            DataType = type;
        }
    }

    public class SkopikValue<T> : SkopikObject, ISkopikObject
    {
        public T Value { get; set; }

        object ISkopikObject.GetData()
        {
            return Value;
        }

        void ISkopikObject.SetData(object value)
        {
            if (!SkopikFactory.IsValueType(value, DataType))
                throw new InvalidOperationException("Value does not match underlying value type.");

            Value = (T)value;
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
        public string Name { get; set; }

        public bool IsAnonymous
        {
            get { return String.IsNullOrEmpty(Name); }
        }

        public virtual bool IsEmpty
        {
            get { return true; }
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

        public ISkopikObject this[string name]
        {
            get { return GetEntry(name);  }
            set { SetEntry(name, value); }
        }
        
        public override bool IsEmpty
        {
            get { return m_entries.Count == 0; }
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

        public ISkopikObject this[int index]
        {
            get { return GetEntry(index); }
            set { SetEntry(index, value); }
        }
        
        public override bool IsEmpty
        {
            get { return m_entries.Count == 0; }
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

        public ISkopikObject this[int index]
        {
            get { return GetEntry(index); }
            set { SetEntry(index, value); }
        }

        public override bool IsEmpty
        {
            get { return m_entries.Count == 0; }
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
