using System;

namespace WebApplication1.Models
{
    public class StoredFile
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
