namespace TaskFlow.Application.Options;

public class CacheOptions
{
    public int DefaultExpiryMinutes { get; set; } = 5;
    public int L1ExpirySeconds      { get; set; } = 30;
}
