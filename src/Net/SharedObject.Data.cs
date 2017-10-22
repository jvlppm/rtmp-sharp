using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Threading.Tasks;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    partial class SharedObject
    {
        public interface IData : IDictionary<string, object>
        {
            event EventHandler OnSync;
        }

        class DataAcessor : DynamicObject, IData
        {
            public readonly IDictionary<string, object> Properties = new Dictionary<string, object>();

            readonly SharedObject owner;

            public event EventHandler OnSync;

            public DataAcessor(SharedObject owner)
            {
                this.owner = owner;
            }

            public void FireSyncCompleted() => OnSync?.Invoke(this, EventArgs.Empty);

            #region DynamicObject implementation
            public override bool TryGetMember(GetMemberBinder binder, out object value)
            {
                return Properties.TryGetValue(binder.Name, out value);
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                this[binder.Name] = value;
                return true;
            }

            public override bool TryDeleteMember(DeleteMemberBinder binder)
            {
                return Remove(binder.Name);
            }
            #endregion

            #region IDictionary implementation
            public object this[string key] {
                get => Properties[key];
                set {
                    Properties[key] = value;
                    // owner.message.Events.Add(new SharedObjectMessage.);
                    throw new NotImplementedException();
                }
            }

            public ICollection<string> Keys => Properties.Keys;

            public ICollection<object> Values => Properties.Values;

            public int Count => Properties.Count;

            public bool IsReadOnly => Properties.IsReadOnly;

            public void Add(string key, object value)
            {
                Properties.Add(key, value);
            }

            public void Add(KeyValuePair<string, object> item)
            {
                Properties.Add(item);
            }

            public void Clear()
            {
                Properties.Clear();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                return Properties.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return Properties.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                Properties.CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return Properties.GetEnumerator();
            }

            public bool Remove(string key)
            {
                if (Properties.Remove(key)) {
                    //owner.message.Events.Add(new SharedObjectMessage.);
                    throw new NotImplementedException();
                    // return true;
                }
                return false;
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                if (Properties.Remove(item)) {
                    //owner.message.Events.Add(new SharedObjectMessage.);
                    throw new NotImplementedException();
                    // return true;
                }
                return false;
            }

            public bool TryGetValue(string key, out object value)
            {
                return Properties.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Properties.GetEnumerator();
            }
            #endregion
        }
    }
}
