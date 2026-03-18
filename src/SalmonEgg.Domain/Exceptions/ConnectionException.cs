using System;

namespace SalmonEgg.Domain.Exceptions
{
    /// <summary>
    /// Connection related exceptions.
    /// </summary>
    public class ConnectionException : Exception
    {
        /// <summary>
        /// Error type
        /// </summary>
        public ConnectionErrorType ErrorType { get; set; }

        /// <summary>
        /// Server URL (optional, not all connection errors have a URL)
        /// </summary>
        public string? ServerUrl { get; set; }

        /// <summary>
        /// Creates a connection exception instance.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Error type</param>
        public ConnectionException(string message, ConnectionErrorType errorType)
            : base(message)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// Creates a connection exception instance (with server URL).
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Error type</param>
        /// <param name="serverUrl">Server URL</param>
        public ConnectionException(string message, ConnectionErrorType errorType, string? serverUrl)
            : base(message)
        {
            ErrorType = errorType;
            ServerUrl = serverUrl;
        }

        /// <summary>
        /// Creates a connection exception instance (with inner exception).
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Error type</param>
        /// <param name="innerException">Inner exception</param>
        public ConnectionException(string message, ConnectionErrorType errorType, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
        }

        /// <summary>
        /// Creates a connection exception instance (with server URL and inner exception).
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Error type</param>
        /// <param name="serverUrl">Server URL</param>
        /// <param name="innerException">Inner exception</param>
        public ConnectionException(string message, ConnectionErrorType errorType, string? serverUrl, Exception innerException)
            : base(message, innerException)
        {
            ErrorType = errorType;
            ServerUrl = serverUrl;
        }
    }
}
