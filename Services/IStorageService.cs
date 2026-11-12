namespace GroupProject.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder = "requests", string? customFileName = null);
        Task DeleteFileAsync(string folder, string fileName);
    }
}