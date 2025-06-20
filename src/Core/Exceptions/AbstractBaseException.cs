using BackBuddy.Core.Library.Exceptions.DTOs;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BackBuddy.Core.Library.Exceptions
{
    public abstract class AbstractBaseException : Exception
    {
        protected AbstractBaseException(string code, string message, int statusCode) : base(message)
        {
            WriteData(code, message, statusCode);
        }

        protected AbstractBaseException(string code, string message, int statusCode, Exception ex) : base(message, ex)
        {
            WriteData(code, message, statusCode);
        }

        private void WriteData(string code, string message, int statusCode)
        {
            Data["Code"] = code;
            Data["Message"] = message;
            Data["StatusCode"] = statusCode;
        }

        public async virtual Task WriteToResponse(HttpResponse response)
        {
            if (response.HasStarted) return;
            response.StatusCode = int.Parse(Data["StatusCode"]?.ToString() ?? throw new InvalidOperationException("Exception StatusCode not found!"));
            response.ContentType = "applicatzion/json";
            await response.WriteAsync(JsonSerializer.Serialize(GetErrors()));
        }

        public virtual List<ErrorDto> GetErrors()
        {
            return [new ErrorDto { Code = Data["Code"]?.ToString() ?? throw new InvalidOperationException("Exception Code not found!"), Message = Data["Message"]?.ToString() ?? throw new InvalidOperationException("Exception Message not found!") }];
        }
    }
}
