using System;
using System.Collections;
using System.Collections.Generic;

namespace Skopik
{
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

        public dynamic AsDynamic()
        {
            return new SkopikDynamicBlock(this);
        }
        
        protected SkopikBlock(SkopikDataType type)
            : base (type)
        { }
    }
}
