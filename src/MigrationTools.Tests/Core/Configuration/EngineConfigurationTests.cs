﻿using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTools.Configuration;
using Newtonsoft.Json;

namespace MigrationTools.Tests
{
    [TestClass]
    public class EngineConfigurationTests
    {
        private IEngineConfigurationBuilder ecb = new EngineConfigurationBuilder(new NullLogger<EngineConfigurationBuilder>());

        [TestMethod]
        public void TestSeraliseToJson()
        {
            string json = JsonConvert.SerializeObject(ecb.BuildDefault(),
                    new FieldMapConfigJsonConverter(),
                    new ProcessorConfigJsonConverter(),
                    new MigrationClientConfigJsonConverter());
            StreamWriter sw = new StreamWriter("configuration.json");
            sw.WriteLine(json);
            sw.Close();
        }

        [TestMethod]
        public void TestDeseraliseFromJson()
        {
            TestSeraliseToJson();
            EngineConfiguration ec;
            StreamReader sr = new StreamReader("configuration.json");
            string configurationjson = sr.ReadToEnd();
            sr.Close();
            ec = JsonConvert.DeserializeObject<EngineConfiguration>(configurationjson,
                new FieldMapConfigJsonConverter(),
                new ProcessorConfigJsonConverter(),
                    new MigrationClientConfigJsonConverter());
            Assert.AreEqual(10, ec.FieldMaps.Count);
            Assert.AreEqual(12, ec.Processors.Count);
        }
    }
}