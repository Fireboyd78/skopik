using System;

namespace Skopik
{
    public interface ISkopikValue : ISkopikObject, IConvertible
    {
        object Value { get; set; }
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
}
