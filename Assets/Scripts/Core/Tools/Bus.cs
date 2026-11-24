using System;
using System.Collections.Generic;

namespace Core.Tools {
    /// <summary>
    /// Simple typed message bus. Use it like you would subscribe to concrete actions, and make damn sure you
    /// unsubscribe on disable or destroy or whatever because these carry across scenes.
    /// </summary>
    /// <typeparam name="T">any object type to pass along - it should be unique to the action or message type.</typeparam>
    public struct Bus<T> {
        
        private static readonly List<Action<T>> Actions = new();

        public static void Subscribe(Action<T> action) {
            Actions.Add(action);
        }

        public static void Unsubscribe(Action<T> action) {
            Actions.Remove(action);
        }
        
        public static void Emit(T msg) {
            foreach (var action in Actions) {
                action.Invoke(msg);
            }
        }

        public static void ClearAll() {
            Actions.Clear();
        }
    }
}