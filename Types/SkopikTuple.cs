using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skopik
{
    public interface ISkopikTuple : ISkopikBlock
    {
        SkopikDataType TupleType { get; }

        IEnumerable<object> GetValues();
        IEnumerable<T> GetValues<T>();
    }

    public class SkopikTuple : SkopikBlock, ISkopikTuple
    {
        private List<ISkopikObject> m_entries;

        public List<ISkopikObject> Entries
        {
            get { return m_entries; }
        }
        
        public SkopikDataType TupleType { get; }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        public override void CopyTo(Array array, int index)
        {
            ((ICollection)Entries).CopyTo(array, index);
        }

        public void CopyTo<T>(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo<T>(T[] array, int index)
        {
            if (!SkopikFactory.IsValueType(typeof(T), TupleType))
                throw new InvalidOperationException($"Cannot cast tuple to array of '{typeof(T).Name}'.");

            var items = Entries.Select((e) => e.GetData()).ToArray();

            items.CopyTo(array, 0);
        }

        public override ISkopikObject this[int index]
        {
            get { return GetEntry(index); }
            set { SetEntry(index, value); }
        }

        public override int Count
        {
            get { return m_entries.Count; }
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

        public IEnumerable<object> GetValues()
        {
            foreach (var entry in Entries)
            {
                if (!entry.IsValue)
                    yield break;

                yield return entry.GetData();
            }
        }

        public IEnumerable<T> GetValues<T>()
        {
            var targetType = typeof(T);

            if (targetType.IsSubclassOf(typeof(ISkopikObject)))
                throw new InvalidOperationException("Cannot retrieve Skopik object values!");

            foreach (var entry in GetValues())
            {
                if (!targetType.IsInstanceOfType(entry))
                    yield break;

                yield return (T)entry;
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
