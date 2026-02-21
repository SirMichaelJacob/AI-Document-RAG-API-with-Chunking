using System;
using System.Collections.Generic;
using System.Text;

namespace RAG_Doc.Application.Utilities
{
    public class CustomResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public HttpResponseMessage HttpResponse { get; set; }

        public object Data { get; set; } = new object();
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();

        public CustomResult()
        {

        }
    }
}
