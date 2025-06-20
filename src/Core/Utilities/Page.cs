namespace BackBuddy.Core.Library.Utilities
{
    public class Page<T> where T : class
    {
        public required T Items { get; set; }
        public bool HasMoreEntries { get; set; }
    }
}
