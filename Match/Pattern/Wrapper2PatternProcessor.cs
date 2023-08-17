using System;
using System.Reflection;
using System.Collections.Generic;

public class Wrapper2PatternProcessor : IPatternProcessor<List<IToken>> {
    Type wrapper;
    IPatternProcessor<List<IToken>> subprocessor;
    
    public Wrapper2PatternProcessor(IPatternProcessor<List<IToken>> subprocessor,
                                   Type wrapper) {
        this.wrapper = wrapper;
        this.subprocessor = subprocessor;
    }

    public List<IToken> Process(List<IToken> tokens, int start, int end) {
        IToken result = (IToken)Activator.CreateInstance(
            wrapper, subprocessor.Process(tokens, start, end).ToArray()
        );
        return new List<IToken> {result};
    }
}
