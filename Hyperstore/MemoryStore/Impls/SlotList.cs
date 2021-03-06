//	Copyright � 2013 - 2014, Alain Metge. All rights reserved.
//
//		This file is part of Hyperstore (http://www.hyperstore.org)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region Imports

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#endregion

namespace Hyperstore.Modeling.MemoryStore
{
    /// <summary>
    ///     Liste de tous les slots li�s � une valeur
    /// </summary>
    /// <remarks>
    ///     Cette liste est impl�ment�e sous la forme d'une liste chain�e afin de pouvoir it�rer dessus alors qu'elle est
    ///     modifi�e (TODO voir si cela � un sens maintenant)
    /// </remarks>
    internal sealed class SlotList : IEnumerable<ISlot>, ISlotList
    {
        private readonly NodeType _elementType;
        private readonly object _ownerKey;
        private readonly List<ISlot> _slots;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets or sets the identifier.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public Identity Id { get; private set; }

        internal SlotList(SlotList clone)
        {
            DebugContract.Requires(clone, "clone");

            _ownerKey = clone._ownerKey;
            _elementType = clone._elementType;
            Id = clone.Id;
            _ownerKey = clone._ownerKey;
            _slots = new List<ISlot>(Length + 2);
            _slots.AddRange(clone._slots.Where(s => s.Id > 0));
            Length = _slots.Count;
        }

        internal SlotList(Identity id, NodeType elementType, object ownerKey = null)
        {
            DebugContract.Requires(elementType != NodeType.Property || ownerKey != null);

            Id = id;
            _ownerKey = ownerKey;
            _elementType = elementType;
            _slots = new List<ISlot>(12);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the length.
        /// </summary>
        /// <value>
        ///  The length.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public int Length { get; private set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the enumerator.
        /// </summary>
        /// <returns>
        ///  The enumerator.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public IEnumerator<ISlot> GetEnumerator()
        {
            return new ReverseEnumerator(_slots);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ReverseEnumerator(_slots);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the key that owns this item.
        /// </summary>
        /// <value>
        ///  The owner key.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public object OwnerKey
        {
            get { return _ownerKey; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets the type of the element.
        /// </summary>
        /// <value>
        ///  The type of the element.
        /// </value>
        ///-------------------------------------------------------------------------------------------------
        public NodeType ElementType
        {
            get { return _elementType; }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Adds slot.
        /// </summary>
        /// <param name="slot">
        ///  The slot to remove.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void Add(ISlot slot)
        {
            DebugContract.Requires(slot);

            this._slots.Add(slot);
            Length++;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Removes the given slot.
        /// </summary>
        /// <param name="slot">
        ///  The slot to remove.
        /// </param>
        ///-------------------------------------------------------------------------------------------------
        public void Remove(ISlot slot)
        {
            DebugContract.Requires(slot);

            // Pas de lock ici car le Remove est tjs appel� dans le contexte du vaccum qui
            // a un lock exclusif sur toutes les donn�es
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                if (s != null && s.Id == slot.Id)
                {
                    // On se contente de mettre � null
                    _slots[i] = null;
                    Length--;
                }
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Gets in snapshot.
        /// </summary>
        /// <param name="ctx">
        ///  The context.
        /// </param>
        /// <returns>
        ///  The in snapshot.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public ISlot GetInSnapshot(CommandContext ctx)
        {
            DebugContract.Requires(ctx);

            //    this._lock.EnterReadLock();
            var enumerator = GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    if (ctx.IsValidInSnapshot(item))
                        return item;
                }
            }
            finally
            {
                if (enumerator != null)
                    enumerator.Dispose();
                //      this._lock.ExitReadLock();
            }
            return null;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Renvoi le slot actif (le dernier non supprim�) - TODO je ne pense pas que ce soit bon.
        /// </summary>
        /// <returns>
        ///  The active slot.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------
        public ISlot GetActiveSlot()
        {
            //this._lock.EnterReadLock();
            var enumerator = GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var slot = enumerator.Current;
                    if (slot.XMax == null)
                        return slot;
                }
            }
            finally
            {
                if (enumerator != null)
                    enumerator.Dispose();
                //    this._lock.ExitReadLock();
            }
            return null;
        }

        #region IDisposable Members

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///  resources.
        /// </summary>
        ///-------------------------------------------------------------------------------------------------
        public void Dispose()
        {
        }

        #endregion

        private class ReverseEnumerator : IEnumerator<ISlot>
        {
            private ISlot _current;
            private IList<ISlot> _list;
            private int _pos;

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Constructor.
            /// </summary>
            /// <param name="list">
            ///  The list.
            /// </param>
            ///-------------------------------------------------------------------------------------------------
            public ReverseEnumerator(IList<ISlot> list)
            {
                DebugContract.Requires(list);

                _list = list;
                _pos = list.Count;
            }

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Gets the current.
            /// </summary>
            /// <value>
            ///  The current.
            /// </value>
            ///-------------------------------------------------------------------------------------------------
            public ISlot Current
            {
                get { return _current; }
            }

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
            ///  resources.
            /// </summary>
            ///-------------------------------------------------------------------------------------------------
            public void Dispose()
            {
                _list = null;
            }

            object IEnumerator.Current
            {
                get { return _current; }
            }

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Determines if we can move next.
            /// </summary>
            /// <returns>
            ///  true if it succeeds, false if it fails.
            /// </returns>
            ///-------------------------------------------------------------------------------------------------
            public bool MoveNext()
            {
                if (_pos > 0)
                {
                    _pos--;
                    _current = _list[_pos];
                    return true;
                }
                _current = null;
                return false;
            }

            ///-------------------------------------------------------------------------------------------------
            /// <summary>
            ///  Resets this instance.
            /// </summary>
            ///-------------------------------------------------------------------------------------------------
            public void Reset()
            {
                _current = null;
            }
        }
    }
}