
namespace RaceDay.Part3.Models
{
    public class LoginApiResponse
    {
        public string Message { get; set; } = "";

        public string Id { get; set; } = "";

        public string Email { get; set; } = "";

        public List<string> Roles { get; set; } = new();
    }
}
