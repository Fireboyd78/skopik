using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skopik
{
    public enum SkopikDataType
    {
        Invalid         = -1,

        Null,

        Scope,
        Array,

        Boolean,
        
        Integer32,
        Integer64,

        UInteger32,
        UInteger64,
        
        Float,
        Double,

        String,

        Reference,

        Reserved        = 200,
    }

    public abstract class SkopikObjectType
    {
        /// <summary>
        /// Gets or sets the actual object value of this type.
        /// </summary>
        protected object DataValue { get; set; }

        public abstract SkopikDataType DataType { get; }
    }

    public class SkopikNullType : SkopikObjectType
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Null; }
        }
        
        public SkopikNullType()
        {
            DataValue = null;
        }
    }

    public abstract class SkopikBaseScopeType : SkopikObjectType
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

    public class SkopikScopeType : SkopikBaseScopeType
    {
        public class ScopeDataValue : Dictionary<string, SkopikObjectType> { }
        
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

        public bool IsObjectInScope(SkopikObjectType obj)
        {
            if (ScopeData.Count > 0)
                return ScopeData.ContainsValue(obj);

            return false;
        }
        
        public SkopikScopeType()
        {
            DataValue = new ScopeDataValue();
        }

        public SkopikScopeType(string name)
            : this()
        {
            Name = name;
        }
    }

    public class SkopikArrayType : SkopikBaseScopeType
    {
        public class ArrayDataValue : List<SkopikObjectType> { }

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

        public SkopikArrayType()
        {
            DataValue = new ArrayDataValue();
        }

        public SkopikArrayType(string name)
            : this()
        {
            Name = name;
        }
    }

    public class SkopikBooleanType : SkopikObjectType
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

        public SkopikBooleanType()
        {
            Value = false;
        }

        public SkopikBooleanType(bool value)
        {
            Value = value;
        }

        public SkopikBooleanType(string value)
        {
            var parseVal = bool.Parse(value);
            Value = parseVal;
        }
    }

    public enum SkopikIntegerDisplayType
    {
        /// <summary>
        /// The default display type. The number will be formatted normally.
        /// </summary>
        Default,

        /// <summary>
        /// The binary display type. The number will be formatted as a binary sequence.
        /// </summary>
        Binary,

        /// <summary>
        /// The hexadecimal display type. The number will be formatted as a hexadecimal number.
        /// </summary>
        Hexadecimal,
    }

    public abstract class SkopikIntegerBaseType : SkopikObjectType
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Invalid; }
        }

        /// <summary>
        /// Gets or sets the display type of this integer.
        /// </summary>
        public SkopikIntegerDisplayType DisplayType { get; set; }
    }

    public class SkopikInteger32Type : SkopikIntegerBaseType
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

        public SkopikInteger32Type() : base()
        {
            DataValue = default(int);
        }

        public SkopikInteger32Type(int value)
        {
            DataValue = value;
        }

        public SkopikInteger32Type(string value)
        {
            var parseVal = int.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikUInteger32Type : SkopikIntegerBaseType
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

        public SkopikUInteger32Type() : base()
        {
            DataValue = default(uint);
        }

        public SkopikUInteger32Type(string value)
        {
            var parseVal = uint.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikInteger64Type : SkopikIntegerBaseType
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

        public SkopikInteger64Type() : base()
        {
            DataValue = default(long);
        }

        public SkopikInteger64Type(long value)
        {
            DataValue = value;
        }

        public SkopikInteger64Type(string value)
        {
            var parseVal = long.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikUInteger64Type : SkopikIntegerBaseType
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

        public SkopikUInteger64Type() : base()
        {
            DataValue = default(ulong);
        }

        public SkopikUInteger64Type(ulong value)
        {
            DataValue = value;
        }

        public SkopikUInteger64Type(string value)
        {
            var parseVal = ulong.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikFloatType : SkopikObjectType
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

        public SkopikFloatType() : base()
        {
            DataValue = default(float);
        }

        public SkopikFloatType(float value)
        {
            DataValue = value;
        }

        public SkopikFloatType(string value)
        {
            var parseVal = float.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikDoubleType : SkopikObjectType
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

        public SkopikDoubleType() : base()
        {
            DataValue = default(double);
        }

        public SkopikDoubleType(double value)
        {
            DataValue = value;
        }

        public SkopikDoubleType(string value)
        {
            var parseVal = double.Parse(value);
            DataValue = parseVal;
        }
    }

    public class SkopikStringType : SkopikObjectType
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.String; }
        }

        /// <summary>
        /// Gets or sets the value of this string.
        /// </summary>
        public string Value { get; set; }

        public SkopikStringType()
        {
            Value = String.Empty;
        }

        public SkopikStringType(string value)
        {
            Value = value;
        }
    }

    public class SkopikReferenceType : SkopikObjectType
    {
        public override SkopikDataType DataType
        {
            get { return SkopikDataType.Reference; }
        }

        /// <summary>
        /// Gets the active scope.
        /// </summary>
        public SkopikScopeType Scope { get; }

        /// <summary>
        /// Gets the referenced object.
        /// </summary>
        public SkopikObjectType ObjectReference { get; }

        public SkopikReferenceType(SkopikScopeType scope, string objName)
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

        public SkopikReferenceType(SkopikScopeType scope, SkopikObjectType objRef)
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
