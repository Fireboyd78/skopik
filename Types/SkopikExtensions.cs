using System;

namespace Skopik
{
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
}
