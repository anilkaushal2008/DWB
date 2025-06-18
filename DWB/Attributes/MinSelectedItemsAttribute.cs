using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace DWB.Attributes
{
    public class MinSelectedItemsAttribute : ValidationAttribute
    {
        private readonly int _minItems;

        public MinSelectedItemsAttribute(int minItems)
        {
            _minItems = minItems;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var list = value as IList;

            if (list != null && list.Count >= _minItems)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? $"At least {_minItems} item(s) must be selected.");
        }
    }
}
