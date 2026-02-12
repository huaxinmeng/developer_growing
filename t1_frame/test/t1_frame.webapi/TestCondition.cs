using System.ComponentModel.DataAnnotations;

namespace t1_frame.webapi
{
    [CustomValidation(typeof(SeachApiAddressPartsValidator), "IsValid")]
    public class TestCondition : IValidatableObject
    {
        /// <summary>
        /// 是否定标（是：Y，否：N）
        /// </summary>
        [Required]
        public string IsTarget { get; set; }

        /// <summary>
        /// 目标值： 必填
        /// </summary>
        public string DataValue { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(DataValue))
            {
                yield return new ValidationResult("价格参数对应的值不能为空", new[] { nameof(DataValue) });
            }
        }

        [CustomValidation(typeof(TestCondition), "ValidateDataValue")]
        public string data_value { get; set; }

        public static ValidationResult ValidateDataValue(string value, ValidationContext context)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new ValidationResult("价格参数对应的值不能为空");
            }
            return ValidationResult.Success;
        }

        [AllowedValues("aa", "bb")]
        public string Numbers { get; set; }
    }

    public class SeachApiAddressPartsValidator
    {
        public static ValidationResult IsValid(object newObj, ValidationContext context)
        {
            if(context.ObjectInstance is TestCondition obj)
            {
                if (string.IsNullOrEmpty(obj.data_value))
                {
                    return new ValidationResult("价格参数对应的值不能为空", new[] { nameof(obj.data_value) });
                }
            }
            return ValidationResult.Success;
        }
    }
}