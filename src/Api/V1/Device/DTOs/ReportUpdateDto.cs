using BackBuddy.Api.Service.V1.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record ReportUpdateDto
    {
        [DefaultValue(null)]
        public string? Name { get; init; }
        [DefaultValue(null)]
        public ReportVisibilityType? VisibilityType { get; init; }
    }
}
