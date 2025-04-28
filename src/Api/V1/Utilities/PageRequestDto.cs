using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BackBuddy.Api.Service.V1.Utilities
{
    public class PageRequestDto
    {
        [DefaultValue(1)]
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [DefaultValue(10)]
        [Range(1, 1000)]
        public int Size { get; set; } = 10;

        public int Offset() => (Page - 1) * Size;
    }
}
