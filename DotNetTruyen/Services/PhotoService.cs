using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace DotNetTruyen.Services
{
    public class PhotoService : IPhoToService
    {
        private readonly Cloudinary _cloudinary;

        public PhotoService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<List<string>> AddListPhotoAsync(IList<IFormFile> files)
        {
            var uploadTasks = files
                .Where(file => file != null && file.Length > 0)
                .Select(file => AddPhotoAsync(file))
                .ToList();

            var imageUrls = await Task.WhenAll(uploadTasks);
            return imageUrls.Where(url => !string.IsNullOrEmpty(url)).ToList();
        }

        public async Task<string> AddPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream)
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult?.Url.ToString();
            }
        }

        public async Task DeletePhotoAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }


            var uri = new Uri(imageUrl);
            var segments = uri.Segments;
            var lastSegment = segments.Last(); 
            var publicId = Path.GetFileNameWithoutExtension(lastSegment); 
            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
