namespace Core.Tools {
    public interface IObserver<T>
    {
        public void OnValueChange(T value);
    }
}