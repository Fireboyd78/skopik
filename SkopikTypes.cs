using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Skopik
{
    using ArrayDataValue = List<ISkopikObject>;
    using ScopeDataValue = Dictionary<string, ISkopikObject>;

    public enum SkopikDataType : int
    {
        // not a skopik data type
        // does not necessarily mean it's invalid
        None,
        
        /*
            Object types
        */
        
        Null            = (1 << 0),
        Reference       = (1 << 1),

        Scope           = (1 << 2),
        Array           = (1 << 3),

        String          = (1 << 4),

        /*
            Special types
        */

        Keyword         = (1 << 5),

        Operator        = (1 << 6),
        Reserved        = (1 << 7),

        /*
            Composite operators
        */

        OpStmt          = (1 << 8) | Operator,
        OpStmtBlock     = (1 << 9) | Operator,

        OpBlockStmtEnd  = (1 << 10) | Operator,
        OpBlockOpen     = (1 << 11) | Operator,
        OpBlockClose    = (1 << 12) | Operator,

        OpScopeStmtEnd  = OpBlockStmtEnd | Scope,
        OpScopeOpen     = OpBlockOpen | Scope,
        OpScopeClose    = OpBlockClose | Scope,

        OpArrayStmtEnd  = OpBlockStmtEnd | Array,
        OpArrayOpen     = OpBlockOpen | Array,
        OpArrayClose    = OpBlockClose | Array,
        
        /*
            Number types
        */

        Boolean     = (1 << 16),

        Integer     = (1 << 17),

        Float       = (1 << 18),
        Double      = (1 << 19),

        /*
            Number flags
        */

        Signed      = (1 << 20),
        Unsigned    = (1 << 21),

        BitField    = (1 << 22),

        Long        = (1 << 23),

        NumberFlagMask = (Signed | Unsigned | BitField | Long),
        
        /*
            Composite number types
        */

        Binary      = Integer | BitField,

        Integer32   = Integer | Signed,
        Integer64   = Integer | Signed | Long,

        UInteger32  = Integer | Unsigned,
        UInteger64  = Integer | Unsigned | Long,
    }

    public enum SkopikNumberType
    {
        /// <summary>
        /// The default number type. The number will be formatted normally.
        /// </summary>
        Default,

        /// <summary>
        /// The binary number type. The number will be formatted as a binary sequence.
        /// </summary>
        Binary,

        /// <summary>
        /// The hexadecimal number type. The number will be formatted as a hexadecimal number.
        /// </summary>
        Hexadecimal,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SkopikTypeAttribute : Attribute
    {
        /// <summary>
        /// Gets the data type representing the Skopik object.
        /// </summary>
        public SkopikDataType DataType { get; }

        public SkopikTypeAttribute(SkopikDataType dataType)
        {
            DataType = dataType;
        }
    }

    public interface ISkopikObject
    {
        /// <summary>
        /// Gets the current value of the object.
        /// </summary>
        /// <returns>The current value of the object.</returns>
        object GetValue();
    }
    
    public interface ISkopikTypeObject<T> : ISkopikObject
    {
        /// <summary>
        /// Gets or sets the current value of the object.
        /// </summary>
        T Value { get; set; }   
    }

    public interface ISkopikScopedObject : ISkopikObject
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

    public interface ISkopikScopedTypeObject<T> : ISkopikScopedObject
    {
        /// <summary>
        /// Gets the inner data of this scoped object.
        /// </summary>
        T InnerData { get; }
    }

    public interface ISkopikNumberObject : ISkopikObject
    {
        SkopikNumberType NumberType { get; set; }
    }

    public static class SkopikObjectUtils
    {
        /// <summary>
        /// Gets the underlying <see cref="SkopikDataType"/> of the object.
        /// </summary>
        /// <param name="obj">The <see cref="ISkopikObject"/> to retrieve the type of.</param>
        /// <returns>A <see cref="SkopikDataType"/> describing the <see cref="ISkopikObject"/>.</returns>
        public static SkopikDataType GetObjectType(this ISkopikObject obj)
        {
            var type = obj.GetType();
            var attrs = type.GetCustomAttributes(typeof(SkopikTypeAttribute), false);

            if (attrs.Length == 0)
                return SkopikDataType.None;

            var attr = attrs[0] as SkopikTypeAttribute;

            if (attr == null)
                throw new InvalidOperationException("A fatal error occurred while trying to retrieve the data type of an object.");

            return attr.DataType;
        }
    }

    public abstract class SkopikTypeObject<T> : ISkopikTypeObject<T>
    {
        object ISkopikObject.GetValue()
        {
            return Value;
        }
        
        public T Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public SkopikTypeObject()
        {
            Value = default(T);
        }

        public SkopikTypeObject(T value)
        {
            Value = value;
        }
    }
    
    public abstract class SkopikNumberObject<T> : SkopikTypeObject<T>, ISkopikNumberObject
    {
        public SkopikNumberType NumberType { get; set; }

        public SkopikNumberObject()
            : base() { }
        public SkopikNumberObject(T value)
            : base(value) { }
    }

    public abstract class SkopikScopedObject : ISkopikScopedObject
    {
        object ISkopikObject.GetValue()
        {
            throw new InvalidOperationException("Cannot retrieve the value of a scoped object from the base type.");
        }
        
        public string Name { get; set; }

        public bool IsAnonymous
        {
            get { return String.IsNullOrEmpty(Name); }
        }

        public virtual bool IsEmpty
        {
            get { return true; }
        }
    }

    public abstract class SkopikScopedTypeObject<T> : SkopikScopedObject, ISkopikScopedTypeObject<T>
        where T : ICollection, IEnumerable, new()
    {
        protected T m_innerData;

        object ISkopikObject.GetValue()
        {
            return m_innerData;
        }

        public T InnerData
        {
            get
            {
                if (m_innerData == null)
                    m_innerData = new T();

                return m_innerData;
            }
        }

        public override bool IsEmpty
        {
            get { return (InnerData.Count == 0); }
        }

        /// <summary>
        /// Determines whether or not the specified <see cref="ISkopikObject"/> is present within the scope.
        /// </summary>
        /// <param name="obj">The <see cref="ISkopikObject"/> to find.</param>
        /// <returns>True if the <see cref="ISkopikObject"/> was found; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="obj"/> parameter is null.</exception>
        public abstract bool IsObjectInScope(ISkopikObject obj);
    }

    [SkopikType(SkopikDataType.Scope)]
    public class SkopikScope : SkopikScopedTypeObject<ScopeDataValue>
    {
        /// <summary>
        /// Gets a <see cref="ISkopikObject"/> with the specified name in the current scope.
        /// </summary>
        /// <param name="name">The name of the <see cref="ISkopikObject"/> to retrieve.</param>
        /// <returns>The <see cref="ISkopikObject"/> with the specified <paramref name="name"/>; otherwise, null.</returns>
        public ISkopikObject this[string name]
        {
            get
            {
                if (InnerData.ContainsKey(name))
                    return InnerData[name];

                return null;
            }
        }

        /// <summary>
        /// Determines whether or not the <see cref="ISkopikObject"/> with the specified name is present within the scope.
        /// </summary>
        /// <param name="name">The name of the <see cref="ISkopikObject"/> to find.</param>
        /// <returns>True if the <see cref="ISkopikObject"/> was found; otherwise, false.</returns>
        public bool IsObjectInScope(string name)
        {
            if (InnerData.Count > 0)
                return InnerData.ContainsKey(name);

            return false;
        }

        public override bool IsObjectInScope(ISkopikObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");

            if (InnerData.Count > 0)
                return InnerData.ContainsValue(obj);

            return false;
        }

        public SkopikScope(string name)
        {
            Name = name;
        }
    }

    [SkopikType(SkopikDataType.Array)]
    public class SkopikArray : SkopikScopedTypeObject<ArrayDataValue>
    {
        /// <summary>
        /// Gets a <see cref="ISkopikObject"/> from the specified index in the scope.
        /// </summary>
        /// <param name="index">The index of the <see cref="ISkopikObject"/> to retrieve.</param>
        /// <returns>The <see cref="ISkopikObject"/> with the specified <paramref name="index"/>; otherwise, null.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> exceeds the number of objects in the array.</exception>
        public ISkopikObject this[int index]
        {
            get
            {
                if (index >= InnerData.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Not enough objects present.");

                return InnerData[index];
            }
        }

        /// <summary>
        /// Retrieves the index of the specified <see cref="ISkopikObject"/> in the scope.
        /// </summary>
        /// <param name="obj">The object to retrieve the index of.</param>
        /// <returns>The zero-based index of the <see cref="ISkopikObject"/> in the array. Returns -1 if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="obj"/> parameter is null.</exception>
        public int GetObjectIndex(ISkopikObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");

            for (int i = 0; i < InnerData.Count; i++)
            {
                // check against the actual reference
                if (Object.ReferenceEquals(obj, InnerData[i]))
                    return i;
            }

            // not found
            return -1;
        }

        public override bool IsObjectInScope(ISkopikObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Object cannot be null.");

            return (GetObjectIndex(obj) != -1);
        }

        public SkopikArray(string name)
        {
            Name = name;
        }
    }

    [SkopikType(SkopikDataType.Null)]
    public class SkopikNull : ISkopikObject
    {
        object ISkopikObject.GetValue()
        {
            return null;
        }
        
        public SkopikNull()
        {
        }
    }

    [SkopikType(SkopikDataType.Boolean)]
    public class SkopikBoolean : SkopikTypeObject<Boolean>
    {
        public SkopikBoolean()
            : base() { }
        public SkopikBoolean(bool value)
            : base(value) { }

        public SkopikBoolean(string value)
        {
            var parseVal = bool.Parse(value);
            Value = parseVal;
        }
    }

    [SkopikType(SkopikDataType.Integer32)]
    public class SkopikInteger32 : SkopikNumberObject<Int32>
    {
        public SkopikInteger32()
            : base() { }
        public SkopikInteger32(int value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.UInteger32)]
    public class SkopikUInteger32 : SkopikNumberObject<UInt32>
    {
        public SkopikUInteger32()
            : base() { }
        public SkopikUInteger32(uint value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.Integer64)]
    public class SkopikInteger64 : SkopikNumberObject<Int64>
    {
        public SkopikInteger64()
            : base() { }
        public SkopikInteger64(long value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.UInteger64)]
    public class SkopikUInteger64 : SkopikNumberObject<UInt64>
    {
        public SkopikUInteger64()
            : base() { }
        public SkopikUInteger64(ulong value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.Float)]
    public class SkopikFloat : SkopikNumberObject<Single>
    {
        public SkopikFloat()
            : base() { }
        public SkopikFloat(float value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.Double)]
    public class SkopikDouble : SkopikNumberObject<Double>
    {
        public SkopikDouble()
            : base() { }
        public SkopikDouble(double value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.String)]
    public class SkopikString : SkopikTypeObject<String>
    {
        public SkopikString()
            : base() { }
        public SkopikString(string value)
            : base(value) { }
    }

    [SkopikType(SkopikDataType.Reference)]
    public class SkopikReference : ISkopikObject
    {
        object ISkopikObject.GetValue()
        {
            return ObjectReference;
        }

        /// <summary>
        /// Gets the active scope.
        /// </summary>
        public SkopikScope Scope { get; }

        /// <summary>
        /// Gets the referenced object.
        /// </summary>
        public ISkopikObject ObjectReference { get; }

        public SkopikReference(SkopikScope scope, string objName)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope), "Scope cannot be null.");
            if (String.IsNullOrEmpty(objName))
                throw new ArgumentNullException(nameof(objName), "Object name cannot be null or empty.");

            if (scope.IsObjectInScope(objName))
            {
                Scope = scope;
                ObjectReference = scope.InnerData[objName];
            }
            else
            {
                throw new InvalidOperationException("Object is outside of the scope.");
            }
        }

        public SkopikReference(SkopikScope scope, ISkopikObject objRef)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope), "Scope cannot be null.");
            if (objRef == null)
                throw new ArgumentNullException(nameof(objRef), "Object reference cannot be null.");

            if (scope.IsObjectInScope(objRef))
            {
                Scope = scope;
                ObjectReference = objRef;
            }
            else
            {
                throw new InvalidOperationException("Object is outside of the scope.");
            }
        }
    }
}
