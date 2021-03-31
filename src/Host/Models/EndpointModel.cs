namespace JetHub.Models
{
    public class EndpointModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public EndpointModel(string name, string url, string username, string password)
        {
            Name = name;
            Url = url;
            UserName = username;
            Password = password;
        }
    }
}
