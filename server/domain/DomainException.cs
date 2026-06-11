namespace server.domain;

public sealed class DomainException(string message, Exception? innerException = null)
    : Exception(message, innerException);
