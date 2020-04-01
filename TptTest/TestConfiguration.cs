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
