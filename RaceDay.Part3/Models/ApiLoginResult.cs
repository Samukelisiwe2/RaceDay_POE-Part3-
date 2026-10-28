
namespace RaceDay.Part3.Models
{
    public class ApiLoginResult
    {
        public LoginApiResponse User { get; set; } = new();

        public string AuthenticationCookie { get; set; } = "";
    }
}
