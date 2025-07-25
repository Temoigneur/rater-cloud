using System.ComponentModel.DataAnnotations;
namespace SharedModels.Common
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;
        private readonly string _comparisonValue;
        public RequiredIfAttribute(string comparisonProperty, string comparisonValue)
        {
            _comparisonProperty = comparisonProperty;
            _comparisonValue = comparisonValue;
        }
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (property == null)
                throw new ArgumentException($"Property with name '{_comparisonProperty}' not found.");
            var comparisonValue = property.GetValue(validationContext.ObjectInstance)?.ToString();
            if (comparisonValue == _comparisonValue && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}
