using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Core.Library.Utilities
{
    public class PageRequestDto
    {
        [DefaultValue(10)]
        [Range(1, 100)]
        public required int Size { get; init; } = 10;

        [DefaultValue(1)]
        [Range(1, int.MaxValue)]
        public required int Page { get; init; } = 1;
    }
}
