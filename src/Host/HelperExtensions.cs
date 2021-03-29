using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.IO.Abstractions;
using System.Text;

namespace JetHub
{
    public static class HelperExtensions
    {
        public static string Action(this IUrlHelper url, object values)
        {
            return url.Action(url.ActionContext.ActionDescriptor.RouteValues["action"], values);
        }

        public static EntityTagHeaderValue CreateEntityTag(this IFileSystemInfo sysInfo)
        {
            byte[] etag = BitConverter.GetBytes(sysInfo.LastWriteTimeUtc.Ticks);

            var result = new StringBuilder(2 + etag.Length * 2);
            result.Append("\"");
            foreach (byte b in etag) result.AppendFormat("{0:x2}", b);
            result.Append("\"");

            return new EntityTagHeaderValue(result.ToString());
        }
    }
}
