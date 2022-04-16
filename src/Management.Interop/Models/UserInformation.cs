using System.Text.Json.Serialization;

namespace Xylab.Management.Models
{
    public class UserInformation
    {
        [JsonPropertyName("name")]
        public string UserName { get; set; }

        [JsonPropertyName("uid")]
        public uint UserId { get; set; }

        [JsonPropertyName("gid")]
        public uint GroupId { get; set; }

        [JsonPropertyName("gecos")]
        public string Comment { get; set; }

        [JsonPropertyName("dir")]
        public string HomeDirectory { get; set; }

        [JsonPropertyName("shell")]
        public string Shell { get; set; }

        internal static UserInformation From(in Interop.Libc.passwd_t passwd)
        {
            return new UserInformation
            {
                Shell = passwd.pw_shell,
                Comment = passwd.pw_gecos,
                HomeDirectory = passwd.pw_dir,
                GroupId = passwd.pw_gid,
                UserId = passwd.pw_uid,
                UserName = passwd.pw_name,
            };
        }
    }
}
