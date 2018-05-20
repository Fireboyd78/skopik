using System;
using System.Collections.Generic;
using System.Linq;

namespace Skopik
{
    public interface ISkopikScope : ISkopikBlock
    {
        ISkopikObject this[string name] { get; set; }

        IDictionary<string, ISkopikObject> Entries { get; }

        bool HasEntry(string name);

        ISkopikObject GetEntry(string name);
        void SetEntry(string name, ISkopikObject data);
    }

    public class SkopikScope : SkopikBlock, ISkopikScope
    {
        private Dictionary<string, ISkopikObject> m_entries;

        IDictionary<string, ISkopikObject> ISkopikScope.Entries
        {
            get { return m_entries; }
        }

        public Dictionary<string, ISkopikObject> Entries
        {
            get { return m_entries; }
        }

        public override void CopyTo(Array array, int index)
        {
            var items = m_entries.Select((e) => e.Value).ToArray();

            Array.Copy(items, 0, array, index, Count);
        }

        public override IEnumerator<ISkopikObject> GetEnumerator()
        {
            var values = Entries.Select((e) => e.Value);

            return values.GetEnumerator();
        }

        public override ISkopikObject this[int index]
        {
            get { return Entries.ElementAt(index).Value; }
            set
            {
                if (index > Entries.Count)
                    throw new IndexOutOfRangeException($"Index {index} out of range in scope.");

                var key = Entries.ElementAt(index).Key;

                Entries[key] = value;
            }
        }

        public ISkopikObject this[string name]
        {
            get { return GetEntry(name); }
            set { SetEntry(name, value); }
        }

        public override int Count
        {
            get { return m_entries.Count; }
        }
        
        public bool HasEntry(string name)
        {
            return m_entries.ContainsKey(name);
        }

        public ISkopikObject GetEntry(string name)
        {
            return (HasEntry(name)) ? m_entries[name] : null;
        }
        
        public void SetEntry(string name, ISkopikObject data)
        {
            if (HasEntry(name))
            {
                m_entries[name] = data;
            }
            else
            {
                m_entries.Add(name, data);
            }
        }
        
        public SkopikScope()
            : base(SkopikDataType.Scope)
        {
            m_entries = new Dictionary<string, ISkopikObject>();
        }

        public SkopikScope(string name)
            : this()
        {
            Name = name;
        }
    }
}
