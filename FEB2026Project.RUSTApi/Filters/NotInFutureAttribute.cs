using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Filters
{
    // Custom validation attribute to ensure the date is not in the future
    public class NotInFutureAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date > DateTime.UtcNow)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success!;
        }
    }
}
