using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Skopik
{
    public class SkopikDynamicBlock : DynamicObject
    {
        protected ISkopikBlock Block;

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return (Block is ISkopikScope)
                ? ((ISkopikScope)Block).Entries.Select((e) => e.Key)
                : null;
        }

        protected bool TryGetEntries(out IDictionary<string, ISkopikObject> result)
        {
            if (Block is ISkopikScope)
            {
                result = ((ISkopikScope)Block).Entries;
                return true;
            }

            result = null;
            return false;
        }
        
        protected bool TryGetIndex(GetIndexBinder binder, int index, out object result)
        {
            if (index < Block.Count)
            {
                var obj = Block[index];

                if (obj is ISkopikValue)
                {
                    result = ((ISkopikValue)obj).Value;
                    return true;
                }
                else if (obj is ISkopikBlock)
                {
                    result = new SkopikDynamicBlock((ISkopikBlock)obj);
                    return true;
                }
            }

            result = null;
            return false;
        }

        protected bool TrySetIndex(SetIndexBinder binder, int index, object value)
        {
            if (index < Block.Count)
            {
                ISkopikObject result = null;

                if (value is ISkopikObject)
                {
                    result = (ISkopikObject)value;
                }
                else if (SkopikFactory.IsValueType(value))
                {
                    result = SkopikFactory.CreateValue(value);
                }

                if (result != null)
                {
                    Block[index] = result;
                    return true;
                }
            }

            return false;
        }
        
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1)
            {
                object index = indexes[0];

                if (index is int)
                    return TryGetIndex(binder, (int)index, out result);

                throw new InvalidOperationException("What the hell are you doing?");
            }
            
            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length == 1)
            {
                object index = indexes[0];

                if (index is int)
                    return TrySetIndex(binder, (int)index, value);

                throw new InvalidOperationException("What the hell are you doing?");
            }

            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            IDictionary<string, ISkopikObject> entries = null;

            if (TryGetEntries(out entries))
            {
                ISkopikObject obj = null;

                if (entries.TryGetValue(binder.Name, out obj))
                {
                    if (obj is ISkopikValue)
                    {
                        result = ((ISkopikValue)obj).Value;
                        return true;
                    }
                    else if (obj is ISkopikBlock)
                    {
                        result = new SkopikDynamicBlock((ISkopikBlock)obj);
                        return true;
                    }
                }
            }
            
            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            IDictionary<string, ISkopikObject> entries = null;

            if (TryGetEntries(out entries))
            {
                ISkopikObject obj = null;

                if (entries.TryGetValue(binder.Name, out obj))
                {
                    ISkopikObject result = null;

                    if (value is ISkopikObject)
                    {
                        result = (ISkopikObject)value;
                    }
                    else if (SkopikFactory.IsValueType(value))
                    {
                        result = SkopikFactory.CreateValue(value);
                    }

                    if (result != null)
                    {
                        entries[binder.Name] = result;
                        return true;
                    }
                }
            }
            
            return false;
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            throw new InvalidOperationException("What the hell are you doing?!");
        }

        public override string ToString()
        {
            return Block.ToString();
        }

        public SkopikDynamicBlock(ISkopikBlock block)
            : base()
        {
            Block = block;
        }
    }
}
