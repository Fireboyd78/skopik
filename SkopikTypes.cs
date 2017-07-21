using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skopik
{
    using ArrayDataValue = List<SkopikObject>;
    using ScopeDataValue = Dictionary<string, SkopikObject>;

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

    public abstract class SkopikObject
    {
        /// <summary>
        /// Gets or sets the actual object value of this type.
        /// </summary>
        protected object DataValue { get; set; }

        public abstract SkopikDataType DataType { get; }
    }

    public class SkopikNull : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Null; }
        }
        
        public SkopikNull()
        {
            DataValue = null;
        }
    }

    public abstract class SkopikScopeBase : SkopikObject
    {
        /// <summary>
        /// Gets or sets the name of this scoped object.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets whether or not this scoped object is anonymous.
        /// </summary>
        public virtual bool IsAnonymous
        {
            get { return (String.IsNullOrEmpty(Name)); }
        }

        /// <summary>
        /// Gets whether or not this scoped object is empty.
        /// </summary>
        public abstract bool IsEmpty { get; }
    }

    public class SkopikScope : SkopikScopeBase
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Scope; }
        }
        
        public ScopeDataValue ScopeData
        {
            get { return (ScopeDataValue)DataValue; }
        }
        
        public override bool IsEmpty
        {
            get { return (ScopeData.Count == 0); }
        }

        public bool IsObjectInScope(string name)
        {
            if (ScopeData.Count > 0)
                return ScopeData.ContainsKey(name);

            return false;
        }

        public bool IsObjectInScope(SkopikObject obj)
        {
            if (ScopeData.Count > 0)
                return ScopeData.ContainsValue(obj);

            return false;
        }
        
        public SkopikScope()
        {
            DataValue = new ScopeDataValue();
        }

        public SkopikScope(string name)
            : this()
        {
            Name = name;
        }
    }
    
    public class SkopikArray : SkopikScopeBase
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Array; }
        }
        
        public ArrayDataValue ArrayData
        {
            get { return (ArrayDataValue)DataValue; }
        }
        
        public override bool IsEmpty
        {
            get { return (ArrayData.Count == 0); }
        }

        public SkopikArray()
        {
            DataValue = new ArrayDataValue();
        }

        public SkopikArray(string name)
            : this()
        {
            Name = name;
        }
    }

    public class SkopikBoolean : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Boolean; }
        }

        public bool Value
        {
            get { return (bool)DataValue; }
            set { DataValue = value; }
        }

        public SkopikBoolean()
        {
            Value = false;
        }

        public SkopikBoolean(bool value)
        {
            Value = value;
        }

        public SkopikBoolean(string value)
        {
            var parseVal = bool.Parse(value);
            Value = parseVal;
        }
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

    public abstract class SkopikNumber : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.None; }
        }

        /// <summary>
        /// Gets or sets the number type of this integer.
        /// </summary>
        public SkopikNumberType NumberType { get; set; }
    }

    public class SkopikInteger32 : SkopikNumber
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Integer32; }
        }

        public int Value
        {
            get { return (int)DataValue; }
            set { DataValue = value; }
        }

        public SkopikInteger32() : base()
        {
            DataValue = default(int);
        }

        public SkopikInteger32(int value)
        {
            DataValue = value;
        }
    }

    public class SkopikUInteger32 : SkopikNumber
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.UInteger32; }
        }

        public uint Value
        {
            get { return (uint)DataValue; }
            set { DataValue = value; }
        }

        public SkopikUInteger32() : base()
        {
            DataValue = default(uint);
        }

        public SkopikUInteger32(uint value)
        {
            DataValue = value;
        }
    }

    public class SkopikInteger64 : SkopikNumber
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Integer64; }
        }

        public long Value
        {
            get { return (long)DataValue; }
            set { DataValue = value; }
        }

        public SkopikInteger64() : base()
        {
            DataValue = default(long);
        }

        public SkopikInteger64(long value)
        {
            DataValue = value;
        }
    }

    public class SkopikUInteger64 : SkopikNumber
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.UInteger64; }
        }

        public ulong Value
        {
            get { return (ulong)DataValue; }
            set { DataValue = value; }
        }

        public SkopikUInteger64() : base()
        {
            DataValue = default(ulong);
        }

        public SkopikUInteger64(ulong value)
        {
            DataValue = value;
        }
    }

    public class SkopikFloat : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Float; }
        }

        public float Value
        {
            get { return (float)DataValue; }
            set { DataValue = value; }
        }

        public SkopikFloat() : base()
        {
            DataValue = default(float);
        }

        public SkopikFloat(float value)
        {
            DataValue = value;
        }
    }

    public class SkopikDouble : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Double; }
        }

        public double Value
        {
            get { return (double)DataValue; }
            set { DataValue = value; }
        }

        public SkopikDouble() : base()
        {
            DataValue = default(double);
        }

        public SkopikDouble(double value)
        {
            DataValue = value;
        }
    }

    public class SkopikString : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.String; }
        }

        /// <summary>
        /// Gets or sets the value of this string.
        /// </summary>
        public string Value { get; set; }

        public SkopikString()
        {
            Value = String.Empty;
        }

        public SkopikString(string value)
        {
            Value = value;
        }
    }

    public class SkopikReference : SkopikObject
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Reference; }
        }

        /// <summary>
        /// Gets the active scope.
        /// </summary>
        public SkopikScope Scope { get; }

        /// <summary>
        /// Gets the referenced object.
        /// </summary>
        public SkopikObject ObjectReference { get; }

        public SkopikReference(SkopikScope scope, string objName)
        {
            if (scope == null)
                throw new ArgumentNullException(nameof(scope), "Scope cannot be null.");
            if (String.IsNullOrEmpty(objName))
                throw new ArgumentNullException(nameof(objName), "Object name cannot be null or empty.");

            if (scope.IsObjectInScope(objName))
            {
                Scope = scope;
                ObjectReference = scope.ScopeData[objName];
            }
            else
            {
                throw new InvalidOperationException("Object was not found in the scope.");
            }
        }

        public SkopikReference(SkopikScope scope, SkopikObject objRef)
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
