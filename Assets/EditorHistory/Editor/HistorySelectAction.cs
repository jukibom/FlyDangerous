namespace BedtimeCore.EditorHistory
{
    public struct HistorySelectAction
    {
        public NavigationDirection Direction { get; }
        public bool StopNavigation { get; set; }
        
        public HistorySelectAction(NavigationDirection direction)
        {
            Direction = direction;
            StopNavigation = false;
        }
    }
}