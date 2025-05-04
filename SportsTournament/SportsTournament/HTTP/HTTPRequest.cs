using System;
using System.Collections.Generic;
using System.Linq;

namespace HTTP
{
    public class HTTPRequest
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;

        public HTTPRequest Parse(string requestData)
        {
            var lines = requestData.Split("\r\n");
            if (lines.Length == 0) return this;

            // Erste Zeile: z.B. POST /register HTTP/1.1
            var firstLine = lines[0].Split(' ');
            Method = firstLine[0];
            Path = firstLine[1];

            // Headers einlesen
            int i = 1;
            while (i < lines.Length && !string.IsNullOrEmpty(lines[i]))
            {
                var headerParts = lines[i].Split(':', 2);
                if (headerParts.Length == 2)
                    Headers[headerParts[0].Trim()] = headerParts[1].Trim();
                i++;
            }

            // Body (alles nach der leeren Zeile)
            if (i < lines.Length - 1)
            {
                Body = string.Join("\r\n", lines.Skip(i + 1));
            }

            return this;
        }

        public int GetContentLength()
        {
            return Headers.TryGetValue("Content-Length", out var lengthStr)
                ? int.Parse(lengthStr)
                : 0;
        }
    }
}