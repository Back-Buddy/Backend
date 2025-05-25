using System.ComponentModel;

namespace BackBuddy.Integration_Test.V1.Dtos
{
    public class DeviceQueryDto
    {
        [DefaultValue(null)]
        public bool? Active { get; set; } = null;
    }
}
