using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Core.Tools { 
    /// <summary>
    /// Simple wrapping class for null. Forces type safety and annotates nullable fields.
    /// </summary>
    /// <typeparam name="T">any object or, crucially, primitive type / struct (no more implicit default)</typeparam>
    public class Optional<T> {
        
        /// <summary>
        /// can be used to return empty optionals instead of allocating at runtime.
        /// </summary>
        public static Optional<T> Empty { get; } = new();
        
        private T _value;
        
        public bool HasValue { get; private set; }

        [CanBeNull]
        public T Value
        {
            get
            {
                if (HasValue) return _value;
                throw new NullReferenceException("Optional value is null. Use HasValue to check for null.");
            }
            set {
                if (value == null) return;
                _value = value;
                HasValue = true;
            }
        }

        public bool TryGetValue([MaybeNullWhen(false)] out T value) {
            value = _value;
            return HasValue;
        }
        
        public Optional()
        {
            HasValue = false;
        }
        
        public Optional(T value)
        {
            HasValue = true;
            Value = value;
        }
    }
}