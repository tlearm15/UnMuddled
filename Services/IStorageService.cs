using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GroupProject.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file);
    }
}