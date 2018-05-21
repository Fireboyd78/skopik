using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Skopik
{
    public class SkopikData : ICollection, IEnumerable<KeyValuePair<string, ISkopikObject>>
    {
        public ISkopikBlock Block { get; }

        protected IDictionary<string, ISkopikObject> Entries;

        public string Name { get; set;  }

        public bool IsScope
        {
            get { return Entries != null; }
        }

        public IEnumerator<KeyValuePair<string, ISkopikObject>> GetEnumerator()
        {
            if (IsScope)
                return Entries.GetEnumerator();

            var index = 0;

            return Block.ToDictionary((e) => $"item_{index++}").GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Block.GetEnumerator();
        }

        public int Count
        {
            get { return Block.Count; }
        }

        object ICollection.SyncRoot
        {
            get { return Block.SyncRoot; }
        }

        bool ICollection.IsSynchronized
        {
            get { return Block.IsSynchronized; }
        }
        
        public void CopyTo(Array array, int index)
        {
            Block.CopyTo(array, index);
        }

        public SkopikData this[string name]
        {
            get
            {
                ISkopikObject result = null;

                if (TryGetObject(name, out result) && (result is ISkopikBlock))
                    return new SkopikData((ISkopikBlock)result);

                throw new InvalidOperationException($"Couldn't get data block '{name}'!");
            }
        }

        public SkopikData this[int index]
        {
            get
            {
                if (index < Block.Count)
                {
                    ISkopikObject result = null;

                    if (TryGetObject(index, out result) && (result is ISkopikBlock))
                        return new SkopikData((ISkopikBlock)result);

                    throw new InvalidOperationException($"Couldn't get data block from index '{index}'!");
                }

                throw new ArgumentOutOfRangeException(nameof(index), index, "Data block index out of range.");
            }
        }

        public T Get<T>(string name)
            where T : SkopikObject
        {
            var obj = GetObject(name);

            return (obj is T)
                ? (T)obj
                : null;
        }

        public T GetValue<T>(string name)
        {
            var obj = GetObject(name);

            if (obj is ISkopikValue)
            {
                var val = (ISkopikValue)obj;
                var result = default(T);

                if (val.TryGetValue(ref result))
                    return result;

                throw new InvalidCastException($"Cannot cast value '{name}' to type '{typeof(T).Name}'.");
            }

            throw new InvalidOperationException($"Object '{name}' is not a value type.");
        }
        
        public ISkopikObject GetObject(string name)
        {
            if (IsScope)
                return Entries[name];

            throw new InvalidOperationException("Cannot get object from a non-scope block.");
        }

        public void SetObject(string name, ISkopikObject value)
        {
            if (IsScope)
            {
                if (Entries.ContainsKey(name))
                {
                    Entries[name] = value;
                }
                else
                {
                    Entries.Add(name, value);
                }
            }

            throw new InvalidOperationException($"Cannot set object in a non-scope block.");
        }

        public ISkopikObject GetObject(int index)
        {
            if (index < Block.Count)
                return Block[index];

            throw new ArgumentOutOfRangeException(nameof(index), index, "Object index is out of range.");
        }

        public void SetObject(int index, ISkopikObject value)
        {
            // scopes will throw exceptions due to not being able to add by index
            Block[index] = value;
        }

        public object GetValue(string name)
        {
            if (IsScope)
            {
                var result = Entries[name] as ISkopikValue;

                if (result == null)
                    throw new InvalidCastException($"Object '{name}' is not a value type.");

                return result.Value;
            }

            throw new InvalidOperationException("Cannot get value from a non-scope block.");
        }
        
        public void SetValue(string name, object value)
        {
            if (IsScope)
                Entries[name] = SkopikFactory.CreateValue(value);

            throw new InvalidOperationException($"Cannot set value in a non-scope block.");
        }

        public object GetValue(int index)
        {
            if (index < Block.Count)
            {
                var result = Block[index] as ISkopikValue;

                if (result == null)
                    throw new InvalidCastException($"Object at index {index} is not a value type.");

                return result.Value;
            }

            throw new ArgumentOutOfRangeException(nameof(index), index, "Value index out of range."); 
        }

        public void SetValue(int index, object value)
        {
            Block[index] = SkopikFactory.CreateValue(value);
        }

        public bool TryGetObject(string name, out ISkopikObject result)
        {
            if (IsScope)
            {
                ISkopikObject obj = null;

                if (Entries.TryGetValue(name, out obj))
                {
                    result = obj;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TrySetObject(string name, ISkopikObject value)
        {
            if (IsScope)
            {
                if (Entries.ContainsKey(name))
                {
                    Entries[name] = value;
                }
                else
                {
                    Entries.Add(name, value);
                }

                return true;
            }

            return false;
        }

        public bool TryGetObject(int index, out ISkopikObject result)
        {
            if (index < Block.Count)
            {
                result = Block[index];
                return true;
            }

            result = null;
            return false;
        }

        public bool TrySetObject(int index, ISkopikObject value)
        {
            try
            {
                // scopes will throw an exception,
                // but arrays/tuples will handle this just fine
                Block[index] = value;
                return true;
            }
            catch (Exception)
            {
                // oops!
                return false;
            }
        }

        public bool TryGetValue(string name, out object result)
        {
            ISkopikObject obj = null;

            if (TryGetObject(name, out obj))
            {
                if (obj is ISkopikValue)
                {
                    result = ((ISkopikValue)obj).Value;
                    return true;
                }
            }
            
            result = null;
            return false;
        }

        public bool TrySetValue(string name, object value)
        {
            if (SkopikFactory.IsValueType(value))
                return TrySetObject(name, SkopikFactory.CreateValue(value));

            return false;
        }

        public bool TryGetValue(int index, out object result)
        {
            ISkopikObject obj = null;

            if (TryGetObject(index, out obj))
            {
                if (obj is ISkopikValue)
                {
                    result = ((ISkopikValue)obj).Value;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool TrySetValue(int index, object value)
        {
            if (SkopikFactory.IsValueType(value))
                return TrySetObject(index, SkopikFactory.CreateValue(value));

            return false;
        }

        private object GetMember(MemberInfo info, int index, StringComparison comparisonType)
        {
            ISkopikObject result = null;

            if (IsScope)
            {
                result = Entries.FirstOrDefault((e) => String.Equals(e.Key, info.Name, comparisonType)).Value;
            }
            else
            {
                foreach (var obj in Block)
                {
                    if (obj is ISkopikBlock)
                    {
                        var block = (ISkopikBlock)obj;

                        if (String.Equals(block.Name, info.Name, comparisonType))
                        {
                            result = obj;
                            break;
                        }
                    }
                }

                if ((result == null) && (index < Block.Count))
                    result = Block[index];
            }

            if (result == null)
                throw new InvalidOperationException($"Couldn't find member '{info.Name}'!");

            if (result is ISkopikValue)
                return ((ISkopikValue)result).Value;

            throw new InvalidOperationException($"Found member '{info.Name}', but is too complex to use.");
        }

        public T ToStruct<T>(params string[] keys)
        {
            return ToStruct<T>(StringComparison.InvariantCulture, keys);
        }

        public T ToStruct<T>(StringComparison comparisonType, params string[] keys)
        {
            var type = typeof(T);
            var result = Activator.CreateInstance<T>();
            
            var props = type.GetProperties();
            var fields = type.GetFields();

            var idx = 0;
            
            foreach (var k in keys)
            {
                var isField = k.StartsWith("@");
                var key = (isField) ? k.Substring(1) : k;

                if (isField)
                {
                    var field = fields.FirstOrDefault((p) => String.Equals(p.Name, key, comparisonType));

                    if (field == null)
                        continue;

                    var value = GetMember(field, idx, comparisonType);

                    field.SetValueDirect(__makeref(result), value);
                }
                else
                {
                    var prop = props.FirstOrDefault((p) => String.Equals(p.Name, key, comparisonType));

                    if (prop == null)
                        continue;

                    var value = GetMember(prop, idx, comparisonType);
                    var obj = RuntimeHelpers.GetObjectValue(result);

                    prop.SetValue(obj, value, null);
                    result = (T)obj;
                }

                idx++;
            }

            return result;
        }
        
        public static SkopikData Load(Stream stream, string name)
        {
            ISkopikBlock block = null;

            using (var skop = new SkopikReader(stream))
            {
                block = skop.ReadScope(name);
            }

            return new SkopikData(block);
        }

        public static SkopikData Load(byte[] buffer, string name)
        {
            using (var ms = new MemoryStream(buffer, 0, buffer.Length, false))
            {
                return Load(ms, name);
            }
        }

        public static SkopikData Load(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("Skopik file was not found.");

            var name = Path.GetFileNameWithoutExtension(filename);
            var buffer = File.ReadAllBytes(filename);

            return Load(buffer, name);
        }

        public SkopikData(ISkopikBlock block)
        {
            Block = block;

            if (Block is ISkopikScope)
                Entries = ((ISkopikScope)Block).Entries;

            Name = block.Name;
        }

        public SkopikData(string name)
            : this(new SkopikScope(name))
        { }
    }
}
