using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    /// <summary>
    /// Wrapped type.
    /// </summary>
    /// <remarks>Useful to avoid pass by reference parameters.</remarks>
    public class Wrapped<T>
    {
        public T Value { get; set; }
        public Wrapped(T value)
        {
            Value = value;
        }
    }
}