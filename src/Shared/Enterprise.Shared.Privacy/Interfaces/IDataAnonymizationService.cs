using Enterprise.Shared.Privacy.Models;

namespace Enterprise.Shared.Privacy.Interfaces;

public interface IDataAnonymizationService
{
    Task<string> AnonymizeAsync(string data, AnonymizationLevel level, CancellationToken cancellationToken = default);
    
    Task<string> HashDataAsync(string data, CancellationToken cancellationToken = default);
    
    Task<string> EncryptDataAsync(string data, CancellationToken cancellationToken = default);
    
    Task<string> DecryptDataAsync(string encryptedData, CancellationToken cancellationToken = default);
    
    Task<string> MaskDataAsync(string data, DataCategory category, CancellationToken cancellationToken = default);
    
    Task<string> PseudonymizeAsync(string data, string userId, CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord> AnonymizePersonalDataRecordAsync(PersonalDataRecord record, 
        AnonymizationLevel level, CancellationToken cancellationToken = default);
    
    Task<PersonalDataRecord[]> BulkAnonymizeAsync(PersonalDataRecord[] records, 
        AnonymizationLevel level, CancellationToken cancellationToken = default);
    
    bool IsDataAnonymized(string data);
    
    bool CanReverseAnonymization(AnonymizationLevel level);
    
    Task<Dictionary<string, object>> GetAnonymizationStatisticsAsync(CancellationToken cancellationToken = default);
}