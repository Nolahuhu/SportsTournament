using System.Text;

namespace HTTP
{
    public class HTTPResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json";
        public string Body { get; set; } = "";

        private string GetStatusMessage()
        {
            return StatusCode switch
            {
                200 => "OK",
                201 => "Created",
                400 => "Bad Request",
                401 => "Unauthorized",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown"
            };
        }
        public static HTTPResponse Unauthorized(string message) => new()
        {
            StatusCode = 401,
            Body = $"{{ \"error\": \"{message}\" }}"
        };

        public byte[] GetBytes()
        {
            var response = new StringBuilder();
            response.AppendLine($"HTTP/1.1 {StatusCode} {GetStatusMessage()}");
            response.AppendLine($"Content-Type: {ContentType}");
            response.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(Body)}");
            response.AppendLine();
            response.AppendLine(Body);

            return Encoding.UTF8.GetBytes(response.ToString());
        }

        // Hilfsmethoden
        public static HTTPResponse Ok(string body = "") => new()
        {
            StatusCode = 200,
            Body = body
        };

        public static HTTPResponse Created(string body = "") => new()
        {
            StatusCode = 201,
            Body = body
        };

        public static HTTPResponse BadRequest(string message) => new()
        {
            StatusCode = 400,
            Body = $"{{ \"error\": \"{message}\" }}"
        };

        public static HTTPResponse NotFound(string message = "Not Found") => new()
        {
            StatusCode = 404,
            Body = $"{{ \"error\": \"{message}\" }}"
        };

        public static HTTPResponse Error(string message) => new()
        {
            StatusCode = 500,
            Body = $"{{ \"error\": \"{message}\" }}"
        };
    }
}
