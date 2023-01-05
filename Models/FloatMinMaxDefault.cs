/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;

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
