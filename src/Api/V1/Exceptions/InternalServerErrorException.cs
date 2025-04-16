namespace BackBuddy.Api.Service.V1.Exceptions
{
    public class InternalServerErrorException : AbstractBaseException
    {
        public InternalServerErrorException() : base("System.InternalServerError", "Please try is later.", StatusCodes.Status500InternalServerError)
        {
        }
    }
}
