using System.ComponentModel;

namespace BackBuddy.Integration_Test.V1.Dtos
{
    public class DeviceQueryDto
    {
        [DefaultValue(0)]
        public int Page { get; set; } = 0;

        [DefaultValue(10)]
        public int Size { get; set; } = 10;

        [DefaultValue(null)]
        public bool? Active { get; set; } = null;
    }
}
