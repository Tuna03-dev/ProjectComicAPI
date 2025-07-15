using System.ComponentModel.DataAnnotations;

namespace DotNetTruyen.Services
{
    public class FileValidationAttribute : ValidationAttribute
    {
        private readonly string[] _allowedExtensions = { ".jpg", ".png", ".jpeg" };
        private readonly long _maxFileSize = 5 * 1024 * 1024;

        public FileValidationAttribute() { }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var file = value as IFormFile;

            if (file == null)
            {
                return new ValidationResult("Vui lòng chọn một file.");
            }

            // Kiểm tra đuôi file
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(extension))
            {
                return new ValidationResult("File phải có định dạng .jpg, .png, hoặc .jpeg.");
            }

            // Kiểm tra kích thước file
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult("File phải nhỏ hơn 5MB.");
            }

            return ValidationResult.Success;
        }
    }
}
