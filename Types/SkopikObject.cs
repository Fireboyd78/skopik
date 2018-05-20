using System;

namespace Skopik
{
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
}
