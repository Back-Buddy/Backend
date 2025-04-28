namespace BackBuddy.Api.Service.V1.Utilities
{
    public class Page<T> where T : class
    {
        public required T Items { get; set; }
        public bool HasMoreEntries { get; set; }
    }
}
