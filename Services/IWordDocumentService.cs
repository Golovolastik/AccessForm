using System.Threading.Tasks;
using System.Collections.Generic;

namespace AccessForm.Services
{
    public interface IWordDocumentService
    {
        Task<string> CreateDocumentCopyAsync(string templatePath);
        Task<string> UpdateDocumentContentAsync(string documentPath, Dictionary<string, string> replacements);
        Task<string> ConvertToPdfAndUpdatePathAsync(string wordDocumentPath, int accessRequestId);
    }
} 