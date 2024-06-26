using System;
using System.Linq;
using System.Collections.Generic;

public class ExternalFunction : FunctionDeclaration {
    PatternExtractor<List<IToken>> pattern;
    List<FunctionArgument> arguments;
    string id;
    Func<List<IValueToken>, Type_> returnType_;
    FunctionSource source;

    public ExternalFunction(PatternExtractor<List<IToken>> pattern, List<FunctionArgument> arguments, string id, Func<List<Type_>, Type_> returnType_, FunctionSource source) {
        this.pattern = pattern;
        this.arguments = arguments;
        this.id = id;
        this.returnType_ = (tokens) => {
            List<Type_> types_ = tokens.Select(token=>token.GetType_()).ToList();
            try {
                return returnType_(types_);
            } catch (FunctionCallTypes_Exception e) {
                throw new SyntaxErrorException(
                    e.Message, tokens[e.ArgumentIndex]
                );
            }
        };
        this.source = source;
    }

    public ExternalFunction(PatternExtractor<List<IToken>> pattern, List<FunctionArgument> arguments, string id, Func<List<IValueToken>, Type_> returnType_, FunctionSource source) {
        this.pattern = pattern;
        this.arguments = arguments;
        this.id = id;
        this.returnType_ = returnType_;
        this.source = source;
    }
    
    public ExternalFunction(PatternExtractor<List<IToken>> pattern, List<FunctionArgument> arguments, string id, Type_ returnType_, FunctionSource source) {
        this.pattern = pattern;
        this.arguments = arguments;
        this.id = id;
        this.returnType_ = (tokens) => returnType_;
        this.source = source;
    }
    
    public override PatternExtractor<List<IToken>> GetPattern() {
        return pattern;
    }
    
    public override List<FunctionArgument> GetArguments() {
        return arguments;
    }

    public override Type_ GetReturnType_(List<IValueToken> tokens) {
        return returnType_(tokens);
    }
    
    public override string GetID() {
        return id;
    }

    public override FunctionSource GetSource() {
        return source;
    }

    public override string ToString() {
        return Utils.WrapName(GetType().Name, id.ToString());
    }
}
