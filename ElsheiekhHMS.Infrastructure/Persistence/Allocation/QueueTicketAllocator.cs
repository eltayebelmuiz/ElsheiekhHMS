using System.Data;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class QueueTicketAllocator(ElsheiekhHmsDbContext context)
{
    private const int MaximumSequence = 999;

    public static string Format(int sequenceNumber)
    {
        if (sequenceNumber is < 1 or > MaximumSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
        }

        return $"A-{sequenceNumber:D3}";
    }

    public async Task<(DateOnly QueueDate, int SequenceNumber, string QueueNumber)> AllocateAsync(
        DateOnly queueDate,
        CancellationToken cancellationToken = default)
    {
        if (queueDate == DateOnly.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(queueDate));
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var sequenceNumber = await AllocateSequenceAsync(queueDate, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (queueDate, sequenceNumber, Format(sequenceNumber));
    }

    private async Task<int> AllocateSequenceAsync(
        DateOnly queueDate,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            UPDATE dbo.QueueTicketAllocations
            SET NextSequenceNumber = NextSequenceNumber + 1
            OUTPUT INSERTED.NextSequenceNumber
            WHERE QueueDate = @queueDate AND NextSequenceNumber <= @maximumSequence;
            """;
        AddParameter(command, "@queueDate", queueDate.ToDateTime(TimeOnly.MinValue), DbType.Date);
        AddParameter(command, "@maximumSequence", MaximumSequence);

        var updated = await command.ExecuteScalarAsync(cancellationToken);
        if (updated is not null)
        {
            return Convert.ToInt32(updated) - 1;
        }

        command.Parameters.Clear();
        command.CommandText = """
            SELECT NextSequenceNumber
            FROM dbo.QueueTicketAllocations
            WHERE QueueDate = @queueDate;
            """;
        AddParameter(command, "@queueDate", queueDate.ToDateTime(TimeOnly.MinValue), DbType.Date);
        var existing = await command.ExecuteScalarAsync(cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Queue ticket capacity is exhausted for the requested date.");
        }

        command.Parameters.Clear();
        command.CommandText = """
            INSERT INTO dbo.QueueTicketAllocations (QueueDate, NextSequenceNumber)
            VALUES (@queueDate, 2);
            """;
        AddParameter(command, "@queueDate", queueDate.ToDateTime(TimeOnly.MinValue), DbType.Date);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return 1;
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value,
        DbType? dbType = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        if (dbType is not null)
        {
            parameter.DbType = dbType.Value;
        }
        command.Parameters.Add(parameter);
    }
}
