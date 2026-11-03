using System;

namespace BedtimeCore.EditorHistory
{
    public interface IHistorySelector
    {
        public bool Select(object selection);
        public event Action<object> AddToHistory; 
    }
}