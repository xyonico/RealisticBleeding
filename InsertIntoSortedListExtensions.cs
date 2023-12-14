using System;
using System.Collections;
using System.Collections.Generic;

namespace RealisticBleeding
{
    /// <summary>
    /// Container for extension functions for FastList
    /// that inserts elements lists that are presumed to be already sorted such that sort ordering is preserved
    /// </summary>
    /// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3189</author>
    /// <license>MIT</license>
    public static class InsertIntoSortedListExtensions
    {
        /// <summary>
        /// Insert a value into an FastList that is presumed to be already sorted such that sort
        /// ordering is preserved
        /// </summary>
        /// <param name="list">List to insert into</param>
        /// <param name="value">Value to insert</param>
        /// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
        public static void InsertIntoSortedList<T>(this FastList<T> list, in T value) where T : IComparable<T>
        {
            var startIndex = 0;
            var endIndex = list.Count;
            while (endIndex > startIndex)
            {
                var windowSize = endIndex - startIndex;
                var middleIndex = startIndex + (windowSize / 2);
                ref readonly var middleValue = ref list[middleIndex];
                var compareToResult = middleValue.CompareTo(value);

                if (compareToResult == 0)
                {
                    list.Insert(middleIndex, in value);
                    return;
                }

                if (compareToResult < 0)
                {
                    startIndex = middleIndex + 1;
                }
                else
                {
                    endIndex = middleIndex;
                }
            }

            list.Insert(startIndex, in value);
        }
    }
}