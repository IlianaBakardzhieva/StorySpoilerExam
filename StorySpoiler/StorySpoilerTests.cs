using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;
using System.Net;
using System.Text.Json;

namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string? createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("IlianaD", "123123");
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }
        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }
        [Order(1)]
        [Test]
        public void CreateStorySpoiler_ShoudReturnCreated()
        {
            var storyRequest = new StoryDTO
            {
                Title = "Story Spoiler",
                Description = "This is a description of this story spoiler.",
                Url = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;
            Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty, "Story ID should not be null or empty.");
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }
        [Order(2)]
        [Test]
        public void EditStorySpoiler_ShouldReturnOK()
        {
            var editRequest = new StoryDTO
            {
                Title = "Edit the created story spoiler",
                Description = "This is an edited story spoiler of existing story.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddQueryParameter("storyId", createdStoryId);
            request.AddJsonBody(editRequest);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }
        [Order(3)]
        [Test]
        public void GetAllStorySpoilers_ShouldReturnListOfStories()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var stories = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);
            Assert.That(stories, Is.Not.Empty, "This list should be not empty.");
        }
        [Order(4)]
        [Test]
        public void DeleteStorySpoiler_ShouldReturnOK()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }
        [Order(5)]
        [Test]
        public void CreateStorySpoilerWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyFakeRequest = new StoryDTO
            {
                Title = "",
                Description = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyFakeRequest);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [Order(6)]
        [Test]
        public void EditStorySpoilerWithInvalidId_ShouldReturnNotFound()
        {
            string invalidSoryid = "47";
            var editRequest = new StoryDTO
            {
                Title = "Edited Non-Existing Story",
                Description = "This is an updated story description.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{invalidSoryid}", Method.Put);
            request.AddQueryParameter("storyId", invalidSoryid);
            request.AddJsonBody(editRequest);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }
        [Order(7)]
        [Test]
        public void DeleteStorySpoilerWithInvalidID_ShouldReturnBadRequest()
        {
            string invalidStoryId = "404";
            var request = new RestRequest($"/api/Story/Delete/{invalidStoryId}", Method.Delete);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}