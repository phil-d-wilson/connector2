using connector.plugins;
using connector.supervisor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telerik.JustMock;
using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace connector.testing
{
    [TestClass]
    public class BasePluginTests
    {
        [TestMethod]
        public void PrivateMethod_NotAllEnvironmentVariablesSetOrDefaulted()
        {
            var testEnvironmentVariables = new Dictionary<string, string>
            {
                { "foo", "bar" },
            };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);            

            var testPlugin = CreateTestPlugin("Test");
            testPlugin.Initialise(testFixture.ServiceProvider);
            var instance = new PrivateAccessor(testPlugin);
            var result = instance.CallMethod("AllEnvironmentVariablesSet");

            Assert.AreEqual(false, result);
            Mock.Assert(() => testFixture.MockLogger.Warning(Arg.AnyString), Occurs.AtLeastOnce());
        }

        [TestMethod]
        public void PrivateMethod_AllEnvironmentVariablesSet()
        {
            var testEnvironmentVariables = new Dictionary<string, string>
            {
                { "foo", "bar" },
                { "foo2", "bar2" }
            };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);

            var testPlugin = CreateTestPlugin("Test");
            testPlugin.Initialise(testFixture.ServiceProvider);
            var instance = new PrivateAccessor(testPlugin);
            var result = instance.CallMethod("AllEnvironmentVariablesSet");

            Assert.AreEqual(true, result);
            Mock.Assert(() => testFixture.MockLogger.Warning(Arg.AnyString), Occurs.Never());
        }

        [TestMethod]
        public void PrivateMethod_DefaultEnvironmentVariableIsSet()
        {
            var testEnvironmentVariables = new Dictionary<string, string>{};
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);
            Mock.Arrange(() => testFixture.MockEnvironmentHandler.GetEnvironmentVariable(Arg.AnyString)).Returns((string)null);

            var testPlugin = CreateTestPlugin("Test");
            testPlugin.Initialise(testFixture.ServiceProvider);
            var instance = new PrivateAccessor(testPlugin);
           instance.CallMethod("LoadDefaultConfigurationIfNeeded");

            Mock.Assert(() => testFixture.MockLogger.Warning(Arg.AnyString), Occurs.Never());
            foreach (var configEnVar in testPlugin.ConfigurationEnvironmentVariables)
            {
                Mock.Assert(() => testFixture.MockEnvironmentHandler.SetEnvironmentVariable(configEnVar.Key, configEnVar.Value), Occurs.Once());
            }
        }

        [TestMethod]
        public async Task PluginLoadsSuccessfully()
        {
            var name = "Test";
            var filename = "Test.yaml";

            var testEnvironmentVariables = new Dictionary<string, string>
            {
                { "foo", "bar" },
                { "foo2", "bar2" }
            };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);

            var mockResolver = Mock.Create<IYamlResolver>();
            Mock.Arrange(() => testFixture.MockYamlResolver.ParseAndResolveYamlComponentFileAsync(name, filename)).Returns(Task.FromResult(true));

            var testPlugin = CreateTestPlugin(name);
            testPlugin.Initialise(testFixture.ServiceProvider);

            Assert.IsTrue(await testPlugin.TryLoadAsync());
            Mock.Assert(() => testFixture.MockYamlResolver.ParseAndResolveYamlComponentFileAsync(name,filename), Occurs.Once());
            Mock.Assert(() => testFixture.MockEnvironmentHandler.SetEnvironmentVariable(Arg.AnyString, Arg.AnyString), Occurs.Never());
        }

        [TestMethod]
        public async Task PluginNotLoaded_NotAllEnvironmentVariablesSet()
        {
            var name = "Test";
            var filename = "Test.yaml";

            var mockSupervisorHandler = Mock.Create<ISupervisorHandler>();
            var testEnvironmentVariables = new Dictionary<string, string>
                {
                    { "foo", "bar" },
                };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);

            var testPlugin = CreateTestPlugin(name);
            testPlugin.Initialise(testFixture.ServiceProvider);

            Assert.IsFalse(await testPlugin.TryLoadAsync());
            Mock.Assert(() => testFixture.MockYamlResolver.ParseAndResolveYamlComponentFileAsync(name, filename), Occurs.Never());
        }

        [TestMethod]
        public async Task PluginLoaded_OneEnvironmentVariablesDefaulted()
        {
            var name = "Test";
            var filename = "Test.yaml";

            var mockSupervisorHandler = Mock.Create<ISupervisorHandler>();
            var testEnvironmentVariables = new Dictionary<string, string> 
            {
                {"foo", "bar" },
                {"foo2", "bar2" }
            };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);
            Mock.Arrange(() => testFixture.MockEnvironmentHandler.GetEnvironmentVariable("foo2")).Returns((string)null);
            Mock.Arrange(() => testFixture.MockEnvironmentHandler.GetEnvironmentVariable("foo")).Returns("bar");

            var testPlugin = CreateTestPlugin(name);
            testPlugin.Initialise(testFixture.ServiceProvider);

            Assert.IsFalse(await testPlugin.TryLoadAsync());
            Mock.Assert(() => testFixture.MockEnvironmentHandler.SetEnvironmentVariable("foo2", "bar2"), Occurs.Once());
            Mock.Assert(() => testFixture.MockYamlResolver.ParseAndResolveYamlComponentFileAsync(name, filename), Occurs.Once());
        }

        [TestMethod]
        public async Task PluginLoaded_DefaultedEnVarWithTemplateValues()
        {
            var name = "Test";

            var mockSupervisorHandler = Mock.Create<ISupervisorHandler>();
            var testEnvironmentVariables = new Dictionary<string, string>
            {
                {"foo", "bar" },
                {"foo2", "bar2" }
            };
            var testFixture = new TestFixture();
            testFixture.SetMockEnvironmentVariables(testEnvironmentVariables);
            Mock.Arrange(() => testFixture.MockEnvironmentHandler.GetEnvironmentVariable("foo3")).Returns((string)null);

            Mock.Arrange(() => testFixture.MockSupervisorHandler.ServiceExistsInState("Test")).Returns(true);
            Mock.Arrange(()=> testFixture.MockSupervisorHandler.GetServiceDefinition("Test")).Returns(
                new ServiceDefinition()
                {
                    Name = name,
                    Address = "balena",
                    Port =  "8080"
                });

            var testPlugin = CreateTestPlugin(name);
            testPlugin.ConfigurationEnvironmentVariables.Add("foo3", "http://${service-address}:${service-port}");
            testPlugin.Initialise(testFixture.ServiceProvider);

            Assert.IsFalse(await testPlugin.TryLoadAsync());
            Mock.Assert(() => testFixture.MockEnvironmentHandler.SetEnvironmentVariable("foo3", @"http://balena:8080"), Occurs.Once());
        }

        private static Plugin CreateTestPlugin(string name)
        {
           var testPlugin = new Plugin
            {
                ConfigurationEnvironmentVariables = new Dictionary<string, string>() { },
                Name = name,
                ServiceName = "Test"

            };
            testPlugin.ConfigurationEnvironmentVariables.Add("foo", "bar");
            testPlugin.ConfigurationEnvironmentVariables.Add("foo2", "bar2");
            
            return testPlugin;
        }
    }

    public class TestFixture
    {
        public IServiceProvider ServiceProvider { get; set; }
        public IEnvironmentHandler MockEnvironmentHandler { get; set; }
        public ISupervisorHandler MockSupervisorHandler { get; set; }
        public IYamlResolver MockYamlResolver { get; set; }
        public ILogger MockLogger { get; set; }

        public TestFixture()
        {
            MockSupervisorHandler = Mock.Create<ISupervisorHandler>();
            MockEnvironmentHandler = Mock.Create<IEnvironmentHandler>();
            MockYamlResolver = Mock.Create<IYamlResolver>();
            MockLogger = Mock.Create<ILogger>();
            ServiceProvider = CreateMockServiceProvider(MockEnvironmentHandler, MockYamlResolver, MockSupervisorHandler, MockLogger);
        }

        private static IServiceProvider CreateMockServiceProvider(IEnvironmentHandler environmentHandler, IYamlResolver yamlResolver, ISupervisorHandler supervisorHandler, ILogger logger)
        {

            var services = new ServiceCollection();
            services.AddSingleton(supervisorHandler);
            services.AddSingleton(yamlResolver);
            services.AddSingleton(environmentHandler);
            services.AddSingleton(logger);
            var serviceProvider = services.BuildServiceProvider(true);
            var scope = serviceProvider.CreateScope();
            return scope.ServiceProvider;
        }

        public void SetMockEnvironmentVariables(Dictionary<string, string> environmentVariables)
        {
            Mock.Arrange(() => MockEnvironmentHandler.GetEnvironmentVariables()).Returns(environmentVariables);
        }
    }
}
