using BackBuddy.Core.Library.Exceptions;
using MassTransit;
using System.Reflection;

namespace BackBuddy.Core.Library.ExceptionHandlers
{
    public static class MasstransitExceptionHandler
    {
        private static readonly List<Assembly> AssembliesToCheck = [..AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            AssemblyName name = x.GetName();
            if (name.Name == null)
                return false;
            Console.WriteLine($"Checking assembly: {name.Name}");
            return name.Name.StartsWith("Core");
        })];

        public static AbstractBaseException? GetAbstractBaseException(this RequestFaultException ex)
        {
            if (ex.Fault == null)
                return null;

            KeyValuePair<ExceptionInfo, Type>? selectedData = null;

            foreach (ExceptionInfo e in ex.Fault.Exceptions)
            {
                foreach (Assembly assembly in AssembliesToCheck)
                {
                    Type? type = assembly.GetType(e.ExceptionType);
                    if (type == null || type.BaseType == null)
                        continue;
                    if (type.BaseType != typeof(AbstractBaseException))
                        continue;
                    if (e.Data == null) continue;
                    selectedData = new(e, type);
                    break;
                }
                if (selectedData != null)
                    break;
            }

            Console.WriteLine($"Is SelectedData Null? {selectedData == null}");
            if (selectedData == null)
                return null;

            ExceptionInfo selectedInfo = selectedData.Value.Key;
            Type exceptionType = selectedData.Value.Value;

            if (selectedInfo.Data == null)
                return null;

            string? code = selectedInfo.Data["Code"].ToString();
            string? message = selectedInfo.Data["Message"].ToString();
            string? statusCodeRaw = selectedInfo.Data["StatusCode"].ToString();
            if (code == null || message == null || statusCodeRaw == null)
                return null;

            int statusCode = int.Parse(statusCodeRaw);
            ConstructorInfo? fullConstructor = Array.Find(exceptionType.GetConstructors(), x => x.GetParameters().Length == 3);
            ConstructorInfo? emptyConstructor = Array.Find(exceptionType.GetConstructors(), x => x.GetParameters().Length == 0);
            ConstructorInfo? singleConstructor = Array.Find(exceptionType.GetConstructors(), x => x.GetParameters().Length == 1);

            object? throwAbleException = null;
            if (fullConstructor != null)
                throwAbleException = fullConstructor.Invoke([.. code, message, statusCode]);
            else if (emptyConstructor != null)
                throwAbleException = emptyConstructor.Invoke([]);
            else if (singleConstructor != null)
                throwAbleException = singleConstructor.Invoke([.. message]);

            if (throwAbleException == null)
                return null;

            AbstractBaseException exception = (AbstractBaseException)throwAbleException;
            foreach (KeyValuePair<string, object> data in selectedInfo.Data)
            {
                exception.Data[data.Key] = data.Value;
            }

            return exception;
        }
    }
}
