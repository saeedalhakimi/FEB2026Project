using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Filters
{
    public sealed class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _age;

        public MinimumAgeAttribute(int age) => _age = age;

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is not DateTime dob) return ValidationResult.Success;

            var age = DateTime.UtcNow.Year - dob.Year;
            if (dob > DateTime.UtcNow.AddYears(-age)) age--;

            return age >= _age
                ? ValidationResult.Success
                : new ValidationResult($"User must be at least {_age} years old.");
        }
    }
}
