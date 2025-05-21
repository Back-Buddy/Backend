namespace BackBuddy.Api.Service.V1.WebSockets.Entities
{
    public record ConnectedDevice
    {
        public required DateTime ConnectedAt { get; set; }
    }
}
