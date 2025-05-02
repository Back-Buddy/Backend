using MassTransit;

namespace BackBuddy.Api.Service.V1.Utilities
{
    public static class PageExtension
    {
        public static void AddPageHeader(this HttpResponse response, bool hasMoreEntires)
        {
            response.Headers.Append("X-Has-More-Entries", hasMoreEntires.ToString());
        }

        public static int Offset(this PageRequestDto page)
        {
            return (page.Page - 1) * page.Size;
        }
    }
}
