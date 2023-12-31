using System;
using System.Collections.Generic;

public class RawForClause : TreeToken {
    string name;

    public RawForClause(string name) : base(new List<IToken>()) {
        this.name = name;
    }

    public RawForClause(string name, List<IToken> tokens) : base(tokens) {
        this.name = name;
    }

    protected override TreeToken _Copy(List<IToken> tokens) {
        return (TreeToken)new RawForClause(name, tokens);
    }

    public string GetName() {
        return name;
    }
}
