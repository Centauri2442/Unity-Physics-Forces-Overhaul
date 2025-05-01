// SPDX-FileCopyrightText: (c)2024 CentauriCore LLC
// SPDX-FileCopyrightText: (c)2024 Yewnyx Studios
// SPDX-FileCopyrightText: (c)2024 CASCAS! LLC
// SPDX-FileCopyrightText: All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnet.Utilities
{
    /// <summary>
    /// A bi-directional dictionary that allows lookup in both directions. Each key maps to a unique value, and each value maps to a unique key!
    /// </summary>
    public class BiDictionary<TKey, TValue> // TODO - There might be a better way to do this, but this is a convenient wrapper for bi-directional dictionary lookups
    {
        // Internal dictionaries for forward and reverse
        private Dictionary<TKey, TValue> forward = new Dictionary<TKey, TValue>();
        private Dictionary<TValue, TKey> reverse = new Dictionary<TValue, TKey>();

        #region Add Methods

        /// <summary>
        /// Adds a new key-value pair to the bi-directional dictionary. Ensures that neither the key nor the value already exists!
        /// </summary>
        /// <param name="key">The key to be added.</param>
        /// <param name="value">The value to be associated with the key.</param>
        public void Add(TKey key, TValue value)
        {
            if (forward.ContainsKey(key) || reverse.ContainsKey(value))
            {
                throw new ArgumentException("Duplicate key or value detected.");
            }

            forward[key] = value;
            reverse[value] = key;
        }

        /// <summary>
        /// Attempts to add a key-value pair to the bi-directional dictionary. If the key or value already exists, it fails and returns false!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (forward.ContainsKey(key) || reverse.ContainsKey(value)) return false;
            
            forward[key] = value;
            reverse[value] = key;

            return true;
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Retrieves the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose corresponding value is to be retrieved.</param>
        /// <param name="value">The value that is returned, if it exists.</param>
        /// <returns>The value associated with the specified key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the key is not found.</exception>
        public TValue GetByKey(TKey key, out TValue value)
        {
            return forward.TryGetValue(key, out value) ? value : throw new KeyNotFoundException();
        }

        /// <summary>
        /// Retrieves the key associated with the specified value.
        /// </summary>
        /// <param name="value">The value whose corresponding key is to be retrieved.</param>
        /// <param name="key">The key that is returned, if it exists.</param>
        /// <returns>The key associated with the specified value.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the value is not found.</exception>
        public TKey GetByValue(TValue value, out TKey key)
        {
            return reverse.TryGetValue(value, out key) ? key : throw new KeyNotFoundException();
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => forward.GetEnumerator();

        #endregion

        #region Contains Methods

        /// <summary>
        /// Checks if the dictionary contains a specific key.
        /// </summary>
        /// <param name="key">The key to check for existence.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool ContainsKey(TKey key)
        {
            return forward.ContainsKey(key);
        }

        /// <summary>
        /// Checks if the dictionary contains a specific value.
        /// </summary>
        /// <param name="value">The value to check for existence.</param>
        /// <returns>True if the value exists, false otherwise.</returns>
        public bool ContainsValue(TValue value)
        {
            return reverse.ContainsKey(value);
        }
        
        /// <summary>
        /// Checks if the dictionary contains a specific key.
        /// </summary>
        /// <param name="key">The key to check for existence.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool Contains(TKey key)
        {
            return forward.ContainsKey(key);
        }

        /// <summary>
        /// Checks if the dictionary contains a specific value.
        /// </summary>
        /// <param name="value">The value to check for existence.</param>
        /// <returns>True if the value exists, false otherwise.</returns>
        public bool Contains(TValue value)
        {
            return reverse.ContainsKey(value);
        }

        #endregion

        #region Removal Methods

        /// <summary>
        /// Removes a key-value pair using the key.
        /// </summary>
        /// <param name="key">The key to be removed.</param>
        /// <returns>True if the key was removed successfully, false otherwise.</returns>
        public bool Remove(TKey key)
        {
            if (forward.TryGetValue(key, out TValue value))
            {
                forward.Remove(key);
                reverse.Remove(value);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Removes a key-value pair using the value.
        /// </summary>
        /// <param name="value">The value to be removed.</param>
        /// <returns>True if the value was removed successfully, false otherwise.</returns>
        public bool Remove(TValue value)
        {
            if (reverse.TryGetValue(value, out TKey key))
            {
                reverse.Remove(value);
                forward.Remove(key);
                return true;
            }
            
            return false;
        }

        #endregion
        
        /// <summary>
        /// Clears all key-value pairs from the dictionary.
        /// </summary>
        public void Clear()
        {
            forward.Clear();
            reverse.Clear();
        }
    }
}
