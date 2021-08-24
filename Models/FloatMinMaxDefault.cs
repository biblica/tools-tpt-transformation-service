using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TptMain.Models
{
    /// <summary>
    /// Model for tracking the expected minimum, maximum, and default values of a parameter.
    /// 
    /// <para>The values are validated such that min <= default <= max.</para>
    /// </summary>
    public class FloatMinMaxDefault
    {
        /// <summary>
        /// The minimum allowed value.
        /// </summary>
        public float Min { get; private set; }
        /// <summary>
        /// The maximum allowed value.
        /// </summary>
        public float Max { get; private set; }
        /// <summary>
        /// The default value to use if no other value is provided.
        /// </summary>
        public float Default { get; private set; }

        /// <summary>
        /// Basic constructor.
        /// </summary>
        /// <param name="defaultVal">Default value when a parameter is null. (required)</param>
        /// <param name="min">Minimum value allowed. (required)</param>
        /// <param name="max">Maximum value allowed. (required)</param>
        public FloatMinMaxDefault(float defaultVal, float min, float max)
        {
            if (min > max)
            {
                throw new ArgumentException($"min ({min}) cannot be greater than max ({max})");
            }
            if (defaultVal < min || defaultVal > max)
            {
                throw new ArgumentException($"default {defaultVal} must be within min ({min}) and max ({max})");
            }

            Min = min;
            Max = max;
            Default = defaultVal;
        }

        /// <summary>
        /// Validate a value or set it to a default if unset.
        /// </summary>
        /// <param name="inputVal">Input to validate.</param>
        /// <returns>The original value, if set; Otherwise the default.</returns>
        public float ValidateValue(float? inputVal)
        {
            // Return default value if nothing is set.
            if (inputVal == null)
            {
                return Default;
            }

            // ensure the value is within the expected range
            if (inputVal < Min || inputVal > Max)
            {
                throw new ArgumentException($"input ({inputVal}) must be within min ({Min}) and max ({Max})");
            }

            // the value is good, return it.
            return (float)inputVal;
        }
    }
}
