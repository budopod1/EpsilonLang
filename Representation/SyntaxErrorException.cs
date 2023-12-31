using System;

public class SyntaxErrorException : Exception {
    public CodeSpan span;
    
    public SyntaxErrorException(string message, IToken token) : base(message) {
        if (token.span == null) {
            throw new NullReferenceException("IToken.span should never be null");
        }
        span = token.span;
    }

    public SyntaxErrorException(string message, CodeSpan span) : base(message) {
        this.span = span;
    }
}
