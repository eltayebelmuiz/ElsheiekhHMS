using System.Data;
using System.Globalization;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class AppointmentCodeAllocator(
    ElsheiekhHmsDbContext context,
    TimeProvider timeProvider)
{
    public static string Format(int year, int sequenceNumber)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequenceNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
        }

        return $"AP-{year:D4}-{sequenceNumber.ToString("D5", CultureInfo.InvariantCulture)}";
    }

    public async Task<string> AllocateAsync(CancellationToken cancellationToken = default)
    {
        var year = timeProvider.GetUtcNow().ToUniversalTime().Year;

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var sequenceNumber = await AllocateSequenceAsync(year, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Format(year, sequenceNumber);
    }

    private async Task<int> AllocateSequenceAsync(
        int year,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            UPDATE dbo.AppointmentCodeAllocations
            SET NextSequenceNumber = NextSequenceNumber + 1
            OUTPUT INSERTED.NextSequenceNumber
            WHERE AllocationYear = @allocationYear;
            """;
        AddParameter(command, "@allocationYear", year, DbType.Int32);

        var updated = await command.ExecuteScalarAsync(cancellationToken);
        if (updated is not null)
        {
            return Convert.ToInt32(updated, CultureInfo.InvariantCulture) - 1;
        }

        command.Parameters.Clear();
        command.CommandText = """
            INSERT INTO dbo.AppointmentCodeAllocations (AllocationYear, NextSequenceNumber)
            VALUES (@allocationYear, 2);
            """;
        AddParameter(command, "@allocationYear", year, DbType.Int32);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return 1;
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value,
        DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        parameter.DbType = dbType;
        command.Parameters.Add(parameter);
    }
}
