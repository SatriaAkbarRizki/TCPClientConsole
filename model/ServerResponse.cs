class ServerResponse
{
    public string Status { get; set; }
    public string Message { get; set; }

    public Dictionary<string, string> Data { get; set; }
}