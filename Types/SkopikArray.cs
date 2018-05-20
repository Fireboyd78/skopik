using System;
using System.Collections;
using System.Collections.Generic;

namespace Skopik
{
    public interface ISkopikArray : ISkopikBlock
    {
        ISkopikObject GetEntry(int index);
        void SetEntry(int index, ISkopikObject data);
    }

    public class SkopikArray : SkopikBlock, ISkopikArray
    {
        private List<ISkopikObject> m_entries;

        public List<ISkopikObject> Entries
        {
            get { return m_entries; }
        }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        public override void CopyTo(Array array, int index)
        {
            ((ICollection)Entries).CopyTo(array, index);
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
            if (index < m_entries.Count)
            {
                m_entries[index] = data;
            }
            else
            {
                m_entries.Insert(index, data);
            }
        }
        
        public SkopikArray()
            : base(SkopikDataType.Array)
        {
            m_entries = new List<ISkopikObject>();
        }

        public SkopikArray(string name)
            : this()
        {
            Name = name;
        }
    }
}
