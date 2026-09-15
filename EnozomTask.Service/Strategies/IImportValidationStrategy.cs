using EnozomTask.Service.DTOs;

namespace EnozomTask.Service.Strategies;

public interface IImportValidationStrategy
{
    void Validate(ImportDatasetRequest request);
}
