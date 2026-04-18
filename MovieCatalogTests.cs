using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using ExamMovieCatalog.Models;


namespace ExamMovieCatalog
{
    [TestFixture]
    public class MovieCatalogTests
    {
        private RestClient client;
        private static string? lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJlYzEyMjdiYy0wNTFhLTQxYTItOTQzMy1jMzNjOTUwZTU2YWEiLCJpYXQiOiIwNC8xOC8yMDI2IDA3OjM5OjAzIiwiVXNlcklkIjoiODU4NzZmNjEtNjE5MS00ODRkLTYxODUtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJkaW5rbzEyM0BhYnYuYmciLCJVc2VyTmFtZSI6ImRpbmtvMTIzIiwiZXhwIjoxNzc2NTE5NTQzLCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.JW6YDbiJXBuot00qBaRKfezBmJEwaxhxmzTfuSrZGcw";

        private const string LoginEmail = "dinko123@abv.bg";
        private const string LoginPassword = "Dinko123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            var movieRequest = new MovieDTO
            {
                Title = "Test Movie",
                Description = "This is a test movie description."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse!.Msg, Is.EqualTo("Movie created successfully!"));
            Assert.That(createResponse.Movie is not null);
            Assert.That(createResponse!.Movie!.Id, Is.Not.Null);
            lastCreatedMovieId = createResponse.Movie.Id;            
        }

        [Order(2)]
        [Test]
        public void EditExistingMovie_ShouldReturnSuccess()
        {
            var editRequest = new MovieDTO
            {
                Title = "Edited Movie",
                Description = "This is an updated test movie description."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse!.Msg!, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnListOfMovies()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);
            var responsItems = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responsItems, Is.Not.Null);
            Assert.That(responsItems, Is.Not.Empty);            
        }        

        [Order(4)]
        [Test]
        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]

        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var movieRequest = new MovieDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]

        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            var nonExistingMovieId = "Non123";
            var editRequest = new MovieDTO
            {
                Title = "Edited Non-Existing Movie",
                Description = "This is an updated test movie description for a non-existing movie."
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            var nonExistingMovieId = "Non123";
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
}