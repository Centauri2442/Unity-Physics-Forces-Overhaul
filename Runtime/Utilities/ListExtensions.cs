using System.Collections.Generic;

namespace Magnet.Utilities
{
    public static class ListExtensions
    {
        /// <summary>
        /// Removes all null values from the list, including uninitialized or null-equivalent objects.
        /// This works for lists containing reference types, nullable value types, and Unity-specific types like Scene.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list from which null values will be removed.</param>
        public static void RemoveNullValues<T>(this List<T> list)
        {
            list.RemoveAll(item => item == null || item.Equals(null));
        }
        
        /// <summary>
        /// Adds an item to the list if it does not already exist.
        /// Returns true if the item was added; false if it already exists in the list.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the list.</typeparam>
        /// <param name="list">The list to which the item should be added.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>True if the item was added; false if it already exists.</returns>
        public static bool AddUnique<T>(this List<T> list, T item)
        {
            if (list.Contains(item))
                return false;

            list.Add(item);
            return true;
        }
    }
}
