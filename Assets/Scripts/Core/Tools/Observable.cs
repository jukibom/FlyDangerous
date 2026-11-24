using System.Collections.Generic;

namespace Core.Tools {
    /// <summary>
    /// Simple observable paradigm, will respond immediately with any existing value or trigger when a value is set.
    /// Uses optional under the hood such that if a value (including primitive / struct) is not set, it will not fire on
    /// subscribe.
    /// See IObservable for subscription.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Observable<T>
    {
        private List<IObserver<T>> _observers = new();
        private readonly Optional<T> _optionalValue = new();

        public void Set(T value)
        {
            if (_optionalValue.HasValue && _optionalValue.Value!.Equals(value)) return;
            _optionalValue.Value = value;
            
            // iterate in reverse in case any of our responders call Unsubscribe
            for (int i = _observers.Count - 1; i >= 0; i--)
            {
                _observers[i].OnValueChange(value);
            }
        }
        
        public void Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            if (_optionalValue.HasValue) observer.OnValueChange(_optionalValue.Value);
        }

        public void Unsubscribe(IObserver<T> observer)
        {
            _observers.Remove(observer);
        }
    }
}