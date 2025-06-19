using BackBuddy.Core.Library.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Core.Library.Device.Dtos
{
    public record ReportUpdateDto
    {
        [DefaultValue(null)]
        public string? Name { get; init; }
        [DefaultValue(null)]
        public ReportVisibilityType? VisibilityType { get; init; }
    }
}
