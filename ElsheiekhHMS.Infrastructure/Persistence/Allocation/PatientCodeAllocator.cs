using System.Data;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class PatientCodeAllocator(ElsheiekhHmsDbContext context)
{
    private const int MaximumSequence = 99_999;

    public static string Format(int year, int sequenceNumber)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (sequenceNumber is < 1 or > MaximumSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
        }

        return $"PT-{year:D4}-{sequenceNumber:D5}";
    }

    public async Task<string> AllocateAsync(int year, CancellationToken cancellationToken = default)
    {
        _ = Format(year, 1);

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
            UPDATE dbo.PatientCodeAllocations
            SET NextSequenceNumber = NextSequenceNumber + 1
            OUTPUT INSERTED.NextSequenceNumber
            WHERE CodeYear = @codeYear AND NextSequenceNumber <= @maximumSequence;
            """;
        AddParameter(command, "@codeYear", year);
        AddParameter(command, "@maximumSequence", MaximumSequence);

        var updated = await command.ExecuteScalarAsync(cancellationToken);
        if (updated is not null)
        {
            return Convert.ToInt32(updated) - 1;
        }

        command.Parameters.Clear();
        command.CommandText = """
            SELECT NextSequenceNumber
            FROM dbo.PatientCodeAllocations
            WHERE CodeYear = @codeYear;
            """;
        AddParameter(command, "@codeYear", year);
        var existing = await command.ExecuteScalarAsync(cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("PatientCode capacity is exhausted for the requested year.");
        }

        command.Parameters.Clear();
        command.CommandText = """
            INSERT INTO dbo.PatientCodeAllocations (CodeYear, NextSequenceNumber)
            VALUES (@codeYear, 2);
            """;
        AddParameter(command, "@codeYear", year);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return 1;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
