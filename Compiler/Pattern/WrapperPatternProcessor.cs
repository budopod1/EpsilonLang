using System;
using System.Reflection;
using System.Collections.Generic;

public class WrapperPatternProcessor : IPatternProcessor<List<IToken>> {
    Type wrapper;
    IPatternProcessor<List<IToken>> subprocessor;
    
    public WrapperPatternProcessor(IPatternProcessor<List<IToken>> subprocessor,
                                   Type wrapper) {
        this.wrapper = wrapper;
        this.subprocessor = subprocessor;
    }
    
    public WrapperPatternProcessor(Type wrapper) {
        this.wrapper = wrapper;
        this.subprocessor = null;
    }

    public List<IToken> Process(List<IToken> tokens, int start, int end) {
        List<IToken> ntokens = tokens;
        if (subprocessor != null) {
            ntokens = subprocessor.Process(tokens, start, end);
        }
        IToken result = (IToken)Activator.CreateInstance(
            wrapper, new object[] {ntokens}
        );
        return new List<IToken> {result};
    }
}
