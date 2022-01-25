/*
Copyright © 2021 by Biblica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace TptTest
{
    /// <summary>
    /// Test configuration class.
    /// 
    /// Used because MOQ can't mock extension methods, 
    /// which is mostly how IConfiguration is used.
    /// </summary>
    class TestConfiguration : IConfiguration
    {
        /// <summary>
        /// Map of allowed keys/values.
        /// </summary>
        private readonly IDictionary<string, string> _configMap;

        /// <summary>
        /// Set of checked (retrieved) keys.
        /// </summary>
        private readonly ISet<string> _checkedKeys;

        /// <summary>
        /// Read-only accessor for checked keys.
        /// </summary>
        public ISet<string> CheckedKeys => _checkedKeys;

        /// <summary>
        /// Assert if any of the keys aren't checked.
        /// </summary>
        public void AssertIfNotAllKeysChecked()
        {
            Assert.IsTrue(_checkedKeys.SetEquals(_configMap.Keys));
        }

        /// <summary>
        /// Basic ctor.
        /// </summary>
        /// <param name="configMap">Key/value pairs supported by this instance.</param>
        public TestConfiguration(IDictionary<string, string> configMap)
        {
            _configMap = configMap;
            _checkedKeys = new HashSet<string>();
        }

        /// <summary>
        /// Get a value from the configuration map.
        /// </summary>
        /// <param name="key">Configuration key (required).</param>
        /// <returns>Configuration value, if found</returns>
        public string this[string key]
        {
            get
            {
                var result = _configMap[key]; // with throw if key is absent (intentional).
                _checkedKeys.Add(key);

                return result;
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <returns>Doesn't return (throws NIE).</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <returns>Doesn't return (throws NIE).</returns>
        public IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="key">Configuration key (required).</param>
        /// <returns>Doesn't return (throws NIE).</returns>
        public IConfigurationSection GetSection(string key)
        {
            throw new NotImplementedException();
        }
    }

}
