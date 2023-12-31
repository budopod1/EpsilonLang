using System;
using System.Collections.Generic;

public class StructHolder : Holder {
    public StructHolder(List<IToken> tokens) : base(tokens) {}
    
    protected override TreeToken _Copy(List<IToken> tokens) {
        return (TreeToken)new StructHolder(tokens);
    }
}
