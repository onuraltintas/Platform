using System.Data.Common;

namespace Enterprise.Shared.ErrorHandling.Exceptions;

public class DatabaseException : EnterpriseException
{
    public string? SqlState { get; }
    public int? SqlErrorNumber { get; }

    public DatabaseException(string message, string? sqlState = null, 
        int? sqlErrorNumber = null, Exception? innerException = null)
        : base(message, "DATABASE_ERROR", 500, innerException)
    {
        SqlState = sqlState;
        SqlErrorNumber = sqlErrorNumber;
        if (sqlState != null) ErrorData["sqlState"] = sqlState;
        if (sqlErrorNumber.HasValue) ErrorData["sqlErrorNumber"] = sqlErrorNumber.Value;
        Severity = Models.ErrorSeverity.Critical;
    }

    protected override string GetTitle() => "Database Error";

    public static DatabaseException FromDbException(DbException dbException, string? customMessage = null)
    {
        var message = customMessage ?? $"Database error occurred: {dbException.Message}";
        string? sqlState = null;
        int? sqlErrorNumber = null;

        try
        {
            var stateProp = dbException.GetType().GetProperty("SqlState");
            sqlState = stateProp?.GetValue(dbException)?.ToString();
        }
        catch { }

        try
        {
            var numberProp = dbException.GetType().GetProperty("Number");
            var numberVal = numberProp?.GetValue(dbException);
            if (numberVal != null)
            {
                sqlErrorNumber = Convert.ToInt32(numberVal);
            }
        }
        catch { }

        return new DatabaseException(message, sqlState, sqlErrorNumber, dbException);
    }
}