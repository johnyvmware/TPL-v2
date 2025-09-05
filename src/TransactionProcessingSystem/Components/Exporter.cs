using TransactionProcessingSystem.Services;

namespace TransactionProcessingSystem.Components;

public sealed class Exporter(IDatabaseService databaseService)
{
    private readonly IDatabaseService _databaseService = databaseService;
}
