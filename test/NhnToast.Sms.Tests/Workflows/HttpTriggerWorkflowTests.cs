using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Aliencube.AzureFunctions.Extensions.Common;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;
using Toast.Common.Configurations;
using Toast.Common.Exceptions;
using Toast.Common.Models;
using Toast.Tests.Common.Fakes;
using Toast.Sms.Configurations;
using Toast.Sms.Triggers;
using Toast.Sms.Workflows;
using Toast.Sms.Models;
using System.Net.Http;
using FluentValidation;

using WorldDomination.Net.Http;
using Toast.Tests.Common.Configurations;
using Toast.Sms.Tests.Configurations;
using Toast.Common.Builders;

namespace Toast.Sms.Tests.Workflows
{
    [TestClass]
    public class HttpTriggerWorkflowTests

    {

        private Mock<IHttpClientFactory> _factory;
        private Mock<MediaTypeFormatter> _fomatter;
         [TestInitialize]
        public void Init()
        {
            this._factory = new Mock<IHttpClientFactory>();
            this._fomatter = new Mock<MediaTypeFormatter>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this._factory = null;
            this._fomatter = null;
        }
        [TestMethod]
        public void Given_Type_When_Initiated_Then_It_Should_Implement_Interface()
        {
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            var hasInterface = workflow.GetType().HasInterface<IHttpTriggerWorkflow>();

            hasInterface.Should().BeTrue();
        }

        [TestMethod]
        public async Task Given_NullHeader_When_Invoke_ValidateHeaderAsync_Then_It_Should_Throw_NullReferenceException()
        {
            var req = new Mock<HttpRequest>();
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            Func<Task> func = async () => await workflow.ValidateHeaderAsync(req.Object);

            await func.Should().ThrowAsync<NullReferenceException>();
        }

        [TestMethod]
        public async Task Given_NoHeader_When_Invoke_ValidateHeaderAsync_Then_It_Should_Throw_InvalidOperationException()
        {
            var headers = new HeaderDictionary();
            headers.Add("Authorization", "Basic");

            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.Headers).Returns(headers);

            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            Func<Task> func = async () => await workflow.ValidateHeaderAsync(req.Object);

            await func.Should().ThrowAsync<InvalidOperationException>();

        }

        [DataTestMethod]
        [DataRow("hello", " ")]
        [DataRow(" ", "world")]
        public async Task Given_InvalidHeader_When_Invoke_ValidateHeaderAsync_Then_It_Should_Throw_Exception(string username, string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary();
            headers.Add("Authorization", $"Basic {encoded}");

            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.Headers).Returns(headers);

            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            Func<Task> func = async () => await workflow.ValidateHeaderAsync(req.Object);

            await func.Should().ThrowAsync<RequestHeaderNotValidException>();
        }

        [DataTestMethod]
        [DataRow("hello", "world")]
        public async Task Given_ValidHeader_When_Invoke_ValidateHeaderAsync_Then_It_Should_Return_Result(string username, string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary();
            headers.Add("Authorization", $"Basic {encoded}");

            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.Headers).Returns(headers);

            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            var result = await workflow.ValidateHeaderAsync(req.Object);

            result.Should().BeOfType<HttpTriggerWorkflow>();
        }

        [DataTestMethod]
        [DataRow("hello", "world")]
        public async Task Given_ValidHeader_When_Invoke_ValidateHeaderAsync_Then_It_Should_Contain_Headers(string username, string password)
        {
            var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            var encoded = Convert.ToBase64String(bytes);

            var headers = new HeaderDictionary();
            headers.Add("Authorization", $"Basic {encoded}");

            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.Headers).Returns(headers);

            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            var result = await workflow.ValidateHeaderAsync(req.Object);
            var fi = workflow.GetType().GetField("_headers", BindingFlags.NonPublic | BindingFlags.Instance);
            var field = fi.GetValue(result) as RequestHeaderModel;

            field.Should().NotBeNull();
            field.AppKey.Should().Be(username);
            field.SecretKey.Should().Be(password);
        }
        
        /*
        //쿼리 예외 테스트
        [TestMethod]
        public void Given_ValidQueries_fails_When_Invoke_ValidateQueriesAsync_Then_It_Should_Throw_Exception()
        {
            var queries = new QueryString();
            queries.Add("name","value");
            
            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.QueryString).Returns(queries);
            
            var validator = new Mock<IValidator<FakeRequestQueries>>();
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);
            Func<Task> func = async () => await workflow.ValidateQueriesAsync<FakeRequestQueries>(req.Object, validator.Object);

            func.Should().ThrowAsync<RequestQueryNotValidException>();
        }

        //쿼리 테스트
        [DataTestMethod]
        [DataRow("Hello","world")]
        public async Task Given_ValidQueries_When_Invoke_ValidateQueriesAsync_Then_It_Should_Return_Result(string name, string value)
        {
            //var bytes = Encoding.UTF8.GetBytes($"{value}");
            //var encoded = Convert.ToBase64String(bytes);

            var queries = new QueryString();
            queries.Add("Name","Value");

            var req = new Mock<HttpRequest>();
            req.SetupGet(p => p.QueryString).Returns(queries);

            var validator = new Mock<IValidator<FakeRequestQueries>>();
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            await workflow.ValidateQueriesAsync<FakeRequestQueries>(req.Object, validator.Object);

            var fi = workflow.GetType().GetField("_queries", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (FakeRequestQueries)fi.GetValue(workflow);

            result.Should().BeOfType<FakeRequestQueries>();

            //result.PropertyA.Should().Be(expected);
        }             
        */
        
        [TestMethod]
        public void Given_NullSettings_When_Invoke_RequestUrlBuilder_Then_It_Should_Throw_Exception()
        { 
            var set = new Mock<ToastSettings<SmsEndpointSettings>>();

            Assert.ThrowsException<InvalidOperationException>(() => new RequestUrlBuilder().WithSettings(set.Object, "test"));
        }

        [TestMethod]
        public async Task Given_NoSettings_When_Invoke_BuildRequestUrlAync_Then_It_Should_Throw_Exception()
        { 
            var set = new Mock<ToastSettings<SmsEndpointSettings>>();
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            Func<Task> func = async () => await workflow.BuildRequestUrlAsync("Test", set.Object);

            await func.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public void Given_ValidSettings_When_Invoke_BuildRequestUrlAsync_Then_It_Return_requestUrl()
        {
            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);
            var settings = new FakeEndpointSettings()
            {
                BaseUrl = "http://localhost:7071/api/{version}/appKeys/{appKey}",
                Version = "v3.0"

            };

            var header = new RequestHeaderModel() { AppKey = "hello", SecretKey = "world" };
            var headers = typeof(HttpTriggerWorkflow).GetField("_headers", BindingFlags.Instance | BindingFlags.NonPublic);
            headers.SetValue(workflow, header);

            var query = new FakeRequestQueries() {};
            var queries = typeof(HttpTriggerWorkflow).GetField("_queries", BindingFlags.Instance | BindingFlags.NonPublic);
            queries.SetValue(workflow, query);

            workflow.BuildRequestUrlAsync("HttpTrigger", settings);
            var fi = workflow.GetType().GetField("_requestUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            var field = fi.GetValue(workflow);

            field.Should().Be("http://localhost:7071/api/v3.0/appKeys/hello/HttpTrigger");
            
        }

        //invoke
        // [TestMethod]
        // public void Given_GetMessageResponse_Invoke_Then_It_Should_Throw_Exception()
        // {
        //     // var headers = new HeaderDictionary();
        //     // headers.Add("Authorization", "Basic");

        //     // var req = new Mock<HttpRequest>();
        //     // req.SetupGet(p => p.Headers).Returns(headers);

        //     // var http = new HttpClient();
        //     // string requestUrl;
        //     var httpClient = new HttpClient();

        //     var factory = new Mock<IHttpClientFactory>();
        //     factory.Setup(p => p.CreateClient(It.IsAny<string>())).Returns(httpClient);
        //     var workflow = new HttpTriggerWorkflow(factory.Object);
        //     // var result = await workflow.Invoke<GetMessageResponse>();
            
        //     Func<Task> func = async () => await workflow.InvokeAsync<GetMessageResponse>();


        //     func.Should().BeOfType<HttpTriggerWorkflow>();
        //     // func.Should().ThrowAsync<RequestHeaderNotValidException>();
        //     // func.Should().ThrowAsync<RequestBodyNotValidException>();
        //     // func.Should().ThrowAsync<RequestHeaderNotValidException>();
        // }

        /*
        [DataTestMethod]
        [DataRow(HttpVerbs.POST, HttpStatusCode.OK, true, 200, "hello world", "lorem ipsum")]
        public async Task Given_Payload_When_Invoke_InvokeAsync_Then_It_Should_Return_Result(string method, HttpStatusCode statusCode, bool isSuccessful, int resultCode, string resultMessage, string body)
        {
            var model = new FakeResponseModel()
            {
                Header = new ResponseHeaderModel()
                {
                    IsSuccessful = isSuccessful,
                    ResultCode = resultCode,
                    ResultMessage = resultMessage
                },
                Body = body
            };
            var content = new ObjectContent<FakeResponseModel>(model, new JsonMediaTypeFormatter(), MediaTypeNames.Application.Json);
            var options = new HttpMessageOptions()
            {
                HttpResponseMessage = new HttpResponseMessage(statusCode) { Content = content }
            };

            var handler = new FakeHttpMessageHandler(options);

            var http = new HttpClient(handler);
            this._factory.Setup(p => p.CreateClient(It.IsAny<string>())).Returns(http);

            var workflow = new HttpTriggerWorkflow(this._factory.Object, this._fomatter.Object);

            var header = new RequestHeaderModel() { AppKey = "hello", SecretKey = "world" };
            var headers = typeof(HttpTriggerWorkflow).GetField("_headers", BindingFlags.Instance | BindingFlags.NonPublic);
            headers.SetValue(workflow, header);

            var url = "http://localhost:7071/api/HttpTrigger";
            var requestUrl = typeof(HttpTriggerWorkflow).GetField("_requestUrl", BindingFlags.Instance | BindingFlags.NonPublic);
            requestUrl.SetValue(workflow, url);

            var load = new FakeRequestModel()
            {
                FakeProperty1 = "lorem ipsum"
            };
            var payload = typeof(HttpTriggerWorkflow).GetField("_payload", BindingFlags.Instance | BindingFlags.NonPublic);
            payload.SetValue(workflow, load);

            var result = await workflow.InvokeAsync<FakeResponseModel>(new HttpMethod(method)).ConfigureAwait(false);

            result.Header.IsSuccessful.Should().Be(isSuccessful);
            result.Header.ResultCode.Should().Be(resultCode);
            result.Header.ResultMessage.Should().Be(resultMessage);
            result.Body.Should().Be(body);
        }
        */
    }
}